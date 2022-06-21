﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Peach.Core;
using Peach.Core.Dom;

using NLog;
using Peach.Core.IO;

namespace MyExtensions
{
	/// <summary>
	/// Standard file system logger.
	/// </summary>
	[Logger("FileExample", true)]
	[Parameter("Path", typeof(string), "Log folder")]
	public class FileLogger : Peach.Core.Logger
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		Fault reproFault = null;
		TextWriter log = null;
		List<Fault.State> states = null;

		public FileLogger(Dictionary<string, Variant> args)
		{
			Path = (string)args["Path"];
		}

		/// <summary>
		/// The user configured base path for all the logs
		/// </summary>
		public string Path
		{
			get;
			private set;
		}

		/// <summary>
		/// The specific path used to log faults for a given test.
		/// </summary>
		protected string RootDir
		{
			get;
			private set;
		}

		protected enum Category { Faults, Reproducing, NonReproducable }

		protected void SaveFault(Category category, Fault fault)
		{
			log.WriteLine("! Fault detected at iteration {0} : {1}", fault.iteration, DateTime.Now.ToString());

			// root/category/bucket/iteration
			var subDir = System.IO.Path.Combine(RootDir, category.ToString(), fault.folderName, fault.iteration.ToString());

			var files = new List<string>();

			foreach (var kv in fault.collectedData)
			{
				var fileName = System.IO.Path.Combine(subDir, kv.Key);
				SaveFile(category, fileName, kv.Value);
				files.Add(fileName);
			}

			OnFaultSaved(category, fault, files.ToArray());

			log.Flush();
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faults)
		{
			System.Diagnostics.Debug.Assert(reproFault == null);

			reproFault = combineFaults(context, currentIteration, stateModel, faults);
			SaveFault(Category.Reproducing, reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			System.Diagnostics.Debug.Assert(reproFault != null);

			SaveFault(Category.NonReproducable, reproFault);
			reproFault = null;
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			var fault = combineFaults(context, currentIteration, stateModel, faults);

			if (reproFault != null)
			{
				// Save reproFault collectedData in fault
				foreach (var kv in reproFault.collectedData)
				{
					var key = System.IO.Path.Combine("Initial", reproFault.iteration.ToString(), kv.Key);
					fault.collectedData.Add(new Fault.Data(key, kv.Value));
				}

				reproFault = null;
			}

			SaveFault(Category.Faults, fault);
		}

		// TODO: Figure out how to not do this!
		private static byte[] ToByteArray(BitwiseStream data)
		{
			var length = (data.LengthBits + 7) / 8;
			var buffer = new byte[length];
			var offset = 0;
			var count = buffer.Length;

			data.Seek(0, System.IO.SeekOrigin.Begin);

			int nread;
			while ((nread = data.Read(buffer, offset, count)) != 0)
			{
				offset += nread;
				count -= nread;
			}

			if (count != 0)
			{
				System.Diagnostics.Debug.Assert(count == 1);

				ulong bits;
				nread = data.ReadBits(out bits, 64);

				System.Diagnostics.Debug.Assert(nread > 0);
				System.Diagnostics.Debug.Assert(nread < 8);

				buffer[offset] = (byte)(bits << (8 - nread));
			}

			return buffer;
		}

		private static IEnumerable<T> SafeEnum<T>(IEnumerable<T> obj)
		{
			return obj == null ?  new T[0] : obj;
		}

		private Fault combineFaults(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			Fault ret = new Fault();

			Fault coreFault = null;
			List<Fault> dataFaults = new List<Fault>();

			// First find the core fault.
			foreach (Fault fault in faults)
			{
				if (fault.type == FaultType.Fault)
				{
					coreFault = fault;
					logger.Debug("Found core fault [" + coreFault.title + "]");
				}
				else
					dataFaults.Add(fault);
			}

			if (coreFault == null)
				throw new PeachException("Error, we should always have a fault with type = Fault!");

			// Gather up data from the state model
			foreach (var item in stateModel.dataActions)
			{
				logger.Debug("Saving action: " + item.Key);
				ret.collectedData.Add(new Fault.Data(item.Key, ToByteArray(item.Value)));
			}

			// Write out all collected data information
			foreach (Fault fault in faults)
			{
				logger.Debug("Saving fault: " + fault.title);

				foreach (var kv in fault.collectedData)
				{
					string fileName = string.Join(".", new[] { fault.agentName, fault.monitorName, fault.detectionSource, kv.Key }.Where(a => !string.IsNullOrEmpty(a)));
					ret.collectedData.Add(new Fault.Data(fileName, kv.Value));
				}

				if (!string.IsNullOrEmpty(fault.description))
				{
					string fileName = string.Join(".", new[] { fault.agentName, fault.monitorName, fault.detectionSource, "description.txt" }.Where(a => !string.IsNullOrEmpty(a)));
					ret.collectedData.Add(new Fault.Data(fileName, System.Text.Encoding.UTF8.GetBytes(fault.description)));
				}
			}

			// Copy over information from the core fault
			if (coreFault.folderName != null)
				ret.folderName = coreFault.folderName;
			else if (coreFault.majorHash == null && coreFault.minorHash == null && coreFault.exploitability == null)
				ret.folderName = "Unknown";
			else
				ret.folderName = string.Format("{0}_{1}_{2}", coreFault.exploitability, coreFault.majorHash, coreFault.minorHash);

			// Collect the data sets used by peach
			var sb = new StringBuilder();
			var visited = new HashSet<string>();
			foreach (var state in SafeEnum(states))
			{
				// Don't worry about re-entered states, they will always use the same data set selection
				if (!visited.Add(state.name))
					continue;

				foreach (var action in SafeEnum(state.actions))
				{
					foreach (var model in SafeEnum(action.models))
					{
						if (!string.IsNullOrEmpty(model.dataSet))
						{
							sb.Append(state.name);
							sb.Append(".");
							sb.Append(action.name);
							sb.Append(".");

							if (!string.IsNullOrEmpty(model.parameter))
							{
								sb.Append(model.parameter);
								sb.Append(".");
							}

							sb.Append(model.name);
							sb.Append(": ");
							sb.AppendLine(model.dataSet);
						}
					}
				}
			}

			var dataSets = sb.ToString();
			if (!string.IsNullOrEmpty(dataSets))
				ret.collectedData.Add(new Fault.Data("dataSets.txt", System.Text.Encoding.UTF8.GetBytes(dataSets)));

			ret.controlIteration = coreFault.controlIteration;
			ret.controlRecordingIteration = coreFault.controlRecordingIteration;
			ret.description = coreFault.description;
			ret.detectionSource = coreFault.detectionSource;
			ret.monitorName = coreFault.monitorName;
			ret.agentName = coreFault.agentName;
			ret.exploitability = coreFault.exploitability;
			ret.iteration = currentIteration;
			ret.majorHash = coreFault.majorHash;
			ret.minorHash = coreFault.minorHash;
			ret.title = coreFault.title;
			ret.type = coreFault.type;
			ret.states = states;

			return ret;
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			states = new List<Fault.State>();

			if (currentIteration != 1 && currentIteration % 100 != 0)
				return;

			if (totalIterations != null)
			{
				log.WriteLine(". Iteration {0} of {1} : {2}", currentIteration, (uint)totalIterations, DateTime.Now.ToString());
				log.Flush();
			}
			else
			{
				log.WriteLine(". Iteration {0} : {1}", currentIteration, DateTime.Now.ToString());
				log.Flush();
			}
		}

		protected override void StateStarting(RunContext context, State state)
		{
			states.Add(
				new Fault.State()
				{
					name = state.Name,
					actions = new List<Fault.Action>()
				});
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			var rec = new Fault.Action()
			{
				name = action.Name,
				type = action.type,
				models = new List<Fault.Model>()
			};

			foreach (var data in action.allData)
			{
				rec.models.Add(new Fault.Model()
				{
					name = data.dataModel.Name,
					parameter = data.Name ?? "",
					dataSet = data.selectedData != null ? data.selectedData.Name : "",
					mutations = new List<Fault.Mutation>(),
				});
			}

			if (rec.models.Count == 0)
				rec.models = null;

			states.Last().actions.Add(rec);
		}

		protected override void ActionFinished(RunContext context, Peach.Core.Dom.Action action)
		{
			var rec = states.Last().actions.Last();
			if (rec.models == null)
				return;

			foreach (var model in rec.models)
			{
				if (model.mutations.Count == 0)
					model.mutations = null;
			}
		}

		protected override void DataMutating(RunContext context, ActionData data, DataElement element, Mutator mutator)
		{
			var rec = states.Last().actions.Last();

			var tgtName = data.dataModel.Name;
			var tgtParam = data.Name ?? "";
			var tgtDataSet = data.selectedData != null ? data.selectedData.Name : "";
			var model = rec.models.Where(m => m.name == tgtName && m.parameter == tgtParam && m.dataSet == tgtDataSet).FirstOrDefault();
			System.Diagnostics.Debug.Assert(model != null);

			model.mutations.Add(new Fault.Mutation() { element = element.fullName, mutator = mutator.Name });
		}

		protected override void Engine_TestError(RunContext context, Exception e)
		{
			log.WriteLine("! Test error: " + e.ToString());
			log.Flush();
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			if (log != null)
			{
				log.WriteLine(". Test finished: " + context.test.Name);
				log.Flush();
				log.Close();
				log.Dispose();
				log = null;
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			if (log != null)
			{
				log.Flush();
				log.Close();
				log.Dispose();
				log = null;
			}

			RootDir = GetBasePath(context);

			log = OpenStatusLog();

			log.WriteLine("Peach Fuzzing Run");
			log.WriteLine("=================");
			log.WriteLine("");
			log.WriteLine("Date of run: " + context.config.runDateTime.ToString());
			log.WriteLine("Peach Version: " + context.config.version);

			log.WriteLine("Seed: " + context.config.randomSeed);

			log.WriteLine("Command line: " + context.config.commandLine);
			log.WriteLine("Pit File: " + context.config.pitFile);
			log.WriteLine(". Test starting: " + context.test.Name);
			log.WriteLine("");

			log.Flush();
		}

		protected virtual TextWriter OpenStatusLog()
		{
			try
			{
				Directory.CreateDirectory(RootDir);
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}

			return File.CreateText(System.IO.Path.Combine(RootDir, "status.txt"));
		}

		protected virtual string GetBasePath(RunContext context)
		{
			return GetLogPath(context, Path);
		}

		protected virtual void OnFaultSaved(Category category, Fault fault, string[] dataFiles)
		{
			if (category != Category.Reproducing)
			{
				// Ensure any past saving of this fault as Reproducing has been cleaned up
				string reproDir = System.IO.Path.Combine(RootDir, Category.Reproducing.ToString());

				if (Directory.Exists(reproDir))
				{
					try
					{
						Directory.Delete(reproDir, true);
					}
					catch (IOException)
					{
						// Can happen if a process has a file/subdirectory open...
					}
				}
			}
		}

		protected virtual void SaveFile(Category category, string fullPath, byte[] contents)
		{
			try
			{
				string dir = System.IO.Path.GetDirectoryName(fullPath);
				Directory.CreateDirectory(dir);
				File.WriteAllBytes(fullPath, contents);
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}
		}
	}
}

// end

