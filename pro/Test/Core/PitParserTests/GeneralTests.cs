﻿

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	public static class Extensions
	{
		public static bool mutable(this DataModel dm, string element)
		{
			var de = dm.find(element);
			Assert.NotNull(de);
			return de.isMutable;
		}
	}

	[TestFixture]
	[Quick]
	[Peach]
	class GeneralTests
	{
		//[Test]
		//public void NumberDefaults()
		//{
		//    string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
		//        "	<Defaults>" +
		//        "		<Number size=\"8\" endian=\"big\" signed=\"true\"/>" +
		//        "	</Defaults>" +
		//        "	<DataModel name=\"TheDataModel\">" +
		//        "		<Number name=\"TheNumber\" size=\"8\"/>" +
		//        "	</DataModel>" +
		//        "</Peach>";

		//    PitParser parser = new PitParser();
		//    Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
		//    Number num = dom.dataModels[0][0] as Number;

		//    Assert.IsTrue(num.Signed);
		//    Assert.IsFalse(num.LittleEndian);
		//}

		[Test]
		public void DeepOverride()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"       <Block name=\"TheBlock\">" +
				"		      <String name=\"TheString\" value=\"Hello\"/>" +
				"       </Block>" +
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel\" ref=\"TheDataModel1\">" +
				"      <String name=\"TheBlock.TheString\" value=\"World\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels["TheDataModel"].Count);
			Assert.AreEqual(1, ((DataElementContainer)dom.dataModels["TheDataModel"][0]).Count);

			Assert.AreEqual("TheString", ((DataElementContainer)dom.dataModels["TheDataModel"][0])[0].Name);
			Assert.AreEqual("World", (string)((DataElementContainer)dom.dataModels["TheDataModel"][0])[0].DefaultValue);
		}

		[Test]
		public void PeriodInName()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"       <Block name=\"TheBlock\">" +
				"		      <String name=\"The.String\" value=\"Hello\"/>" +
				"       </Block>" +
				"	</DataModel>" +
				"</Peach>";

			Assert.Throws<PeachException>(delegate()
			{
				new Peach.Core.Dom.String("Foo.Bar");
			});

			PitParser parser = new PitParser();

			Assert.Throws<PeachException>(delegate()
			{
				parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			});
		}

		[Test]
		public void IncludeMutators()
		{
			string xml =
@"<Peach>
   <DataModel name='TheDataModel'>
       <String name='str' value='Hello World!'/>
   </DataModel>

   <StateModel name='TheState' initialState='Initial'>
       <State name='Initial'>
           <Action type='output'>
               <DataModel ref='TheDataModel'/>
           </Action>
       </State>
   </StateModel>

   <Test name='Default'>
       <StateModel ref='TheState'/>
       <Publisher class='Null'/>
       <Strategy class='Sequential'/>
       <Mutators mode='include'>
           <Mutator class='StringCaseMutator'/>
           <Mutator class='BlobMutator'/>
       </Mutators>
   </Test>
</Peach>";
			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(0, dom.tests[0].excludedMutators.Count);
			Assert.AreEqual(2, dom.tests[0].includedMutators.Count);
			Assert.AreEqual("StringCaseMutator", dom.tests[0].includedMutators[0]);
			Assert.AreEqual("BlobMutator", dom.tests[0].includedMutators[1]);
		}

		[Test]
		public void ExcludeMutators()
		{
			string xml =
@"<Peach>
   <DataModel name='TheDataModel'>
       <String name='str' value='Hello World!'/>
   </DataModel>

   <StateModel name='TheState' initialState='Initial'>
       <State name='Initial'>
           <Action type='output'>
               <DataModel ref='TheDataModel'/>
           </Action>
       </State>
   </StateModel>

   <Test name='Default'>
       <StateModel ref='TheState'/>
       <Publisher class='Null'/>
       <Strategy class='Sequential'/>
       <Mutators mode='exclude'>
           <Mutator class='StringCaseMutator'/>
           <Mutator class='BlobMutator'/>
       </Mutators>
   </Test>
</Peach>";
			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(0, dom.tests[0].includedMutators.Count);
			Assert.AreEqual(2, dom.tests[0].excludedMutators.Count);
			Assert.AreEqual("StringCaseMutator", dom.tests[0].excludedMutators[0]);
			Assert.AreEqual("BlobMutator", dom.tests[0].excludedMutators[1]);
		}

		[Test]
		public void IncludeExcludeMutable()
		{
			string xml =
@"<Peach>
	<DataModel name='TheDataModel'>
		<String name='str' value='Hello World!'/>
		<String name='str2' value='Hello World!'/>
		<Block name='block'>
			<Block name='subblock'>
				<Blob name='blob'/>
				<Number name='subnum' size='8'/>
			</Block>
			<Number name='num' size='8'/>
		</Block>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Exclude ref='str'/>
		<Exclude ref='block'/>
		<Include ref='subblock'/>
	</Test>
</Peach>";
			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			DataElement de;

			// Shouldn't update the top level data model
			de = dom.dataModels[0].find("TheDataModel.str");
			Assert.NotNull(de);
			Assert.True(de.isMutable);

			var dm = dom.tests[0].stateModel.states["Initial"].actions[0].dataModel;
			Assert.NotNull(dm);

			// Should update the action's data model

			Assert.False(dm.mutable("TheDataModel.str"));
			Assert.True( dm.mutable("TheDataModel.str2"));
			Assert.False(dm.mutable("TheDataModel.block"));
			Assert.True( dm.mutable("TheDataModel.block.subblock"));
			Assert.True( dm.mutable("TheDataModel.block.subblock.blob"));
			Assert.True( dm.mutable("TheDataModel.block.subblock.subnum"));
			Assert.False(dm.mutable("TheDataModel.block.num"));
		}

		[Test]
		public void IncludeExcludeMutable2()
		{
			string xml =
@"<Peach>
	<DataModel name='TheDataModel'>
		<String name='str' value='Hello World!'/>
		<String name='str2' value='Hello World!'/>
		<Choice name='ExcludeMe'>
			<Block name='block'>
				<Block name='subblock'>
					<Blob name='blob'/>
					<Number name='subnum' size='8'/>
				</Block>
				<Number name='num' size='8'/>
			</Block>
			<Block name='block2'>
				<Number name='num' size='8'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Exclude ref='ExcludeMe'/>
	</Test>
</Peach>";
			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var dm = dom.tests[0].stateModel.states["Initial"].actions[0].dataModel;
			Assert.NotNull(dm);

			// Should update the action's data model

			Assert.False(dm.mutable("TheDataModel.ExcludeMe"));
			Assert.False(dm.mutable("TheDataModel.ExcludeMe.block"));
			Assert.False(dm.mutable("TheDataModel.ExcludeMe.block.subblock"));
			Assert.False(dm.mutable("TheDataModel.ExcludeMe.block.subblock.blob"));
			Assert.False(dm.mutable("TheDataModel.ExcludeMe.block.subblock.subnum"));
			Assert.False(dm.mutable("TheDataModel.ExcludeMe.block.num"));

			var choice = dm["ExcludeMe"] as Peach.Core.Dom.Choice;
			Assert.NotNull(choice);

			// All in-scope children should be non-mutable
			foreach (var elem in choice.PreOrderTraverse())
				Assert.False(elem.isMutable, "{0} should not be mutable".Fmt(elem.debugName));

			choice.SelectElement(choice.choiceElements[1]);

			// Because block2 was not selected, its mutability should be not effected
			Assert.True(dm.mutable("TheDataModel.ExcludeMe.block2"));
			Assert.True(dm.mutable("TheDataModel.ExcludeMe.block2.num"));

		}

		[Test]
		public void TopDataElement()
		{
			string temp1 = Path.GetTempFileName();
			File.WriteAllBytes(temp1, Encoding.ASCII.GetBytes("Hello World"));

			string xml = 
@"<Peach>
	<Data name='data' fileName='{0}'/>

	<DataModel name='TheDataModel'>
		<String name='str' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data ref='data'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			xml = string.Format(xml, temp1);

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var ds = dom.stateModels[0].states["Initial"].actions[0].allData.First().dataSets;
			Assert.NotNull(ds);
			Assert.AreEqual(1, ds.Count);
			Assert.AreEqual(1, ds[0].Count());
			Assert.AreEqual(temp1, ((DataFile)ds[0].First()).FileName);
		}

		[Test]
		public void TopDataElement2()
		{
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();
			File.WriteAllBytes(temp1, Encoding.ASCII.GetBytes("Hello World"));
			File.WriteAllBytes(temp2, Encoding.ASCII.GetBytes("Hello World"));

			string xml =
@"<Peach>
	<Data name='data' fileName='{0}'/>

	<DataModel name='TheDataModel'>
		<String name='str' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data ref='data' fileName='{1}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			xml = string.Format(xml, temp1, temp2);

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var ds = dom.stateModels[0].states["Initial"].actions[0].allData.First().dataSets;
			Assert.NotNull(ds);
			Assert.AreEqual(1, ds.Count);
			Assert.AreEqual(1, ds[0].Count);
			Assert.True(ds[0][0] is DataFile);
			Assert.AreEqual(temp2, ((DataFile)ds[0][0]).FileName);
			Assert.AreEqual(temp1, ((DataFile)dom.datas[0][0]).FileName);
		}

		[Test]
		public void TopDataElementGlob()
		{
			string tempDir = Path.GetTempFileName() + "_d";

			Directory.CreateDirectory(tempDir);
			File.WriteAllText(Path.Combine(tempDir, "1.txt"), "");
			File.WriteAllText(Path.Combine(tempDir, "2.txt"), "");
			File.WriteAllText(Path.Combine(tempDir, "2a.txt"), "");
			File.WriteAllText(Path.Combine(tempDir, "1.png"), "");
			File.WriteAllText(Path.Combine(tempDir, "2.png"), "");
			File.WriteAllText(Path.Combine(tempDir, "2a.png"), "");

			var cwd = Environment.CurrentDirectory;

			try
			{
				Environment.CurrentDirectory = tempDir;

				string xml = "<Peach><Data name='data' fileName='*'/></Peach>";
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"].Select(d => d.Name).ToList();

				CollectionAssert.AreEqual(new[]
				{
					"data/1.png",
					"data/1.txt",
					"data/2.png",
					"data/2.txt",
					"data/2a.png",
					"data/2a.txt",
				}, ds);
			}
			finally
			{
				Environment.CurrentDirectory = cwd;
			}

			Assert.Throws<PeachException>(delegate()
			{
				string xml = string.Format("<Peach><Data name='data' fileName=''/></Peach>", tempDir);
				PitParser parser = new PitParser();
				parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			});

			Assert.Throws<PeachException>(delegate()
			{
				string xml = string.Format("<Peach><Data name='data' fileName='foo'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			});

			Assert.Throws<PeachException>(delegate()
			{
				string xml = string.Format("<Peach><Data name='data' fileName='*/foo'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			});

			{
				string xml = string.Format("<Peach><Data name='data' fileName='{0}/*'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"];
				Assert.AreEqual(6, ds.Count);
			}

			{
				string xml = string.Format("<Peach><Data name='data' fileName='{0}/*.txt'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"];
				Assert.AreEqual(3, ds.Count);
			}

			{
				string xml = string.Format("<Peach><Data name='data' fileName='{0}/1.*'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"];
				Assert.AreEqual(2, ds.Count);
			}

			{
				string xml = string.Format("<Peach><Data name='data' fileName='{0}/2*.txt'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"];
				Assert.AreEqual(2, ds.Count);
			}

			{
				string xml = string.Format("<Peach><Data name='data' fileName='{0}/*a.*'/></Peach>", tempDir);
				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				Assert.AreEqual(1, dom.datas.Count);
				Assert.True(dom.datas.ContainsKey("data"));
				var ds = dom.datas["data"];
				Assert.AreEqual(2, ds.Count);
			}
		}

		[Test]
		public void AgentPlatform()
		{
			string xml =
@"<Peach>
	<DataModel name='TheDataModel'>
		<String name='str' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='TheAgent'>
		<Monitor class='Null' />
	</Agent>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Agent ref='TheAgent' platform='{0}'/>
	</Test>
</Peach>";
			xml = string.Format(xml, Platform.GetOS() == Platform.OS.Windows ? "linux" : "windows");

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration() { singleIteration = true };
			var e = new Engine(null);

			e.TestStarting += ctx =>
			{
				ctx.AgentConnect += (c, a) => Assert.Fail("AgentConnect should never be called!");
			};

			e.startFuzzing(dom, config);
		}

		[Test]
		public void RefNamespace()
		{
			string tmp1 = Path.GetTempFileName();
			string tmp2 = Path.GetTempFileName();

			string xml1 = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' endian='big'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
		<Block name='Value'/>
	</DataModel>

	<Agent name='ThirdAgent'/>

	<DataModel name='Random'>
		<String value='Hello World'/>
	</DataModel>
</Peach>";

			string xml2 = @"
<Peach>
	<Include ns='bar' src='{0}'/>

	<DataModel name='DM'>
		<Block ref='bar:TLV' name='Type1'>
			<Number name='Type' size='8' endian='big' value='201'/>
			<Block name='Value'>
				<Blob length='10' value='0000000000'/>
			</Block>
		</Block>
	</DataModel>

	<Agent name='SomeAgent'/>

	<StateModel name='SM' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='output'>
				<DataModel ref='bar:Random'/>
			</Action>
		</State>
	</StateModel>

</Peach>".Fmt(tmp1);

			string xml3 = @"
<Peach>
	<Include ns='foo' src='{0}'/>

	<DataModel name='DM' ref='foo:DM'>
		<Blob/>
	</DataModel>

	<DataModel name='DM2'>
		<Block ref='foo:bar:Random'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='output'>
				<DataModel ref='foo:bar:Random'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>

	<Test name='Other'>
		<StateModel ref='foo:SM'/>
		<Agent ref='foo:SomeAgent' platform='none' />
		<Publisher class='Null'/>
	</Test>

	<Test name='Third'>
		<StateModel ref='TheStateModel'/>
		<Agent ref='foo:SomeAgent' />
		<Agent ref='foo:bar:ThirdAgent' />
		<Publisher class='Null'/>
	</Test>

</Peach>".Fmt(tmp2);

			File.WriteAllText(tmp1, xml1);
			File.WriteAllText(tmp2, xml2);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml3)));

			var final = dom.dataModels[1].Value.ToArray();
			var expected = Encoding.ASCII.GetBytes("Hello World");

			Assert.AreEqual(expected, final);

			Assert.AreEqual(3, dom.tests.Count);

			Assert.AreEqual(0, dom.tests[0].agents.Count);
			Assert.AreEqual(1, dom.tests[1].agents.Count);
			Assert.AreEqual(2, dom.tests[2].agents.Count);

			Assert.AreEqual("foo:SomeAgent", dom.tests[1].agents[0].Name);
			Assert.AreEqual(Platform.OS.None, dom.tests[1].agents[0].platform);

			Assert.AreEqual("foo:SomeAgent", dom.tests[2].agents[0].Name);
			Assert.AreEqual(Platform.OS.All, dom.tests[2].agents[0].platform);

			Assert.AreEqual("foo:bar:ThirdAgent", dom.tests[2].agents[1].Name);
			Assert.AreEqual(Platform.OS.All, dom.tests[2].agents[1].platform);

		}

		[Test]
		public void TestDupeModelNames()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob/>
	</DataModel>

	<DataModel name='DM'>
		<Block/>
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, a <DataModel> element named 'DM' already exists.", ex.Message);
		}

		[Test]
		public void TestBadHexValueType1()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='blob' valueType='hex' value='00 a'/>
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, the hex value of Blob 'DM.blob' must contain an even number of characters: 00a", ex.Message);
		}

		[Test]
		public void TestBadHexValueType2()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='blob' valueType='hex' value='00 aq'/>
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, the value of Blob 'DM.blob' contains invalid hex characters: 00aq", ex.Message);
		}

		[Test]
		public void TestDataModelMutable()
		{
			string xml = @"
<Peach>
	<DataModel name='DM' mutable='false'>
		<Blob name='blob' valueType='hex' value='00 ab'/>
	</DataModel>

	<DataModel name='DM2' ref='DM'>
		<Blob name='blob2' valueType='hex' value='00 ab'/>
	</DataModel>

	<DataModel name='DM3'>
		<Block ref='DM'>
			<Blob name='blob2' valueType='hex' value='00 ab'/>
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels.Count);
			Assert.AreEqual(false, dom.dataModels[0].isMutable);
			Assert.AreEqual(true, dom.dataModels[0][0].isMutable);

			Assert.AreEqual(false, dom.dataModels[1].isMutable);
			Assert.AreEqual(true, dom.dataModels[1][0].isMutable);
			Assert.AreEqual(true, dom.dataModels[1][1].isMutable);

			Assert.AreEqual(true, dom.dataModels[2].isMutable);
			Assert.AreEqual(false, dom.dataModels[2][0].isMutable);
			var c = dom.dataModels[2][0] as DataElementContainer;
			Assert.NotNull(c);
			Assert.AreEqual(true, c[0].isMutable);
			Assert.AreEqual(true, c[1].isMutable);

			Assert.AreEqual(false, dom.stateModels[0].states[0].actions[0].dataModel.isMutable);

		}

		[Test]
		public void LineNumbers()
		{
			const string xml = @"
<Peach>
	<DataModel bad_attr=''/>

	<StateModel/>

	<Test name='Default'/>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			var lines = ex.Message.Split('\n');
			Assert.AreEqual(5, lines.Length, "Expected 5 lines, got:\n{0}", ex.Message);

			StringAssert.Contains("file failed to validate", lines[0]);
			StringAssert.Contains("Line: 3, Position: 13", lines[1]);
			StringAssert.Contains("Line: 5, Position: 3", lines[2]);
			StringAssert.Contains("Line: 5, Position: 3", lines[3]);

			StringAssert.Contains("The 'bad_attr' attribute is not declared.", lines[1]);
			StringAssert.Contains("The required attribute 'initialState' is missing.", lines[2]);
			StringAssert.Contains("The required attribute 'name' is missing.", lines[3]);
		}


		[Test]
		public void TestValidName()
		{
			// Ensure all fieldId values are valid element names so they are xpath selectable
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='1bad' />
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			StringAssert.StartsWith("Error, Pit file failed to validate", ex.Message);
		}

		[Test]
		public void TestGlobalization()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' />
	</StateModel>

	<Test name='Default' waitTime='0.2' faultWaitTime='5.1'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";
			var culture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

			Peach.Core.Dom.Dom pit;

			try
			{
				pit = DataModelCollector.ParsePit(xml);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = culture;
			}

			Assert.AreEqual(0.2, pit.tests[0].waitTime);
			Assert.AreEqual(5.1, pit.tests[0].faultWaitTime);
		}

	}
}
