using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core.Publishers;

namespace Peach.Pro.Test.Core.Publishers
{
	class SimpleTcpClient
	{
		private readonly EndPoint localEP;
		private Socket Socket;
		private bool Graceful;
		public string Result = null;


		public SimpleTcpClient(ushort port, bool graceful)
		{
			localEP = new IPEndPoint(IPAddress.Loopback, port);
			Graceful = graceful;
		}

		public void Start()
		{
			if (Socket != null)
				Socket.Close();
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Socket.BeginConnect(localEP, OnConnect, null);
		}

		public void OnConnect(IAsyncResult ar)
		{
			try
			{
				Socket.EndConnect(ar);

				Socket.Send(Encoding.ASCII.GetBytes("Test buffer"));
				var recv = new byte[1024];
				var len = Socket.Receive(recv);
				Result = Encoding.ASCII.GetString(recv, 0, len);
				if (Graceful)
				{
					Socket.Shutdown(SocketShutdown.Both);
				}
				else
				{
					do
					{
						len = Socket.Receive(recv);
					}
					while (len > 0);
				}
				Socket.Close();
				Socket = null;
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					System.Threading.Thread.Sleep(250);
					Start();
				}
				else
				{
					Assert.Null(ex.Message);
				}
			}
			catch (Exception ex)
			{
				Assert.Null(ex.Message);
			}
		}
	}

	class SimpleTcpEcho : IDisposable
	{
		private readonly EndPoint localEP;
		private Socket Socket;
		private Socket DataSocket;

		public SimpleTcpEcho()
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			localEP = new IPEndPoint(IPAddress.Loopback, 0);
			Socket.Bind(localEP);
			localEP = Socket.LocalEndPoint;
			Socket.Listen(8);
			Socket.BeginAccept(OnAccept, null);
		}

		public int LocalPort
		{
			get
			{
				return ((IPEndPoint)localEP).Port;
			}
		}

		public void OnAccept(IAsyncResult ar)
		{
			try
			{
				DataSocket = Socket.EndAccept(ar);

				while (true)
				{
					var recv = new byte[1024];
					var len = DataSocket.Receive(recv);

					System.Threading.Thread.Sleep(500);

					DataSocket.Send(recv, len, SocketFlags.None);
				}
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (Socket != null)
			{
				Socket.Close();
				Socket = null;
			}

			if (DataSocket != null)
			{
				DataSocket.Close();
				DataSocket = null;
			}
		}
	}

	class SimpleTcpServer
	{
		private EndPoint localEP;
		private Socket Socket;
		private bool Graceful;
		public string Result = null;
		private bool Send;


		public SimpleTcpServer(ushort port, bool graceful, bool send = true)
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			localEP = new IPEndPoint(IPAddress.Loopback, port);
			Graceful = graceful;
			Send = send;
			Socket.Bind(localEP);
			Socket.Listen(8);
		}

		public void Start()
		{
			Socket.BeginAccept(OnAccept, null);
		}

		public void Stop()
		{
			Socket.Close();
			Socket.Dispose();
			Socket = null;
		}

		public void OnAccept(IAsyncResult ar)
		{
			try
			{
				var cli = Socket.EndAccept(ar);

				if (Send)
					cli.Send(Encoding.ASCII.GetBytes("Test buffer"));
				var recv = new byte[1024];
				var len = cli.Receive(recv);
				Result = Encoding.ASCII.GetString(recv, 0, len);
				if (Graceful)
				{
					cli.Shutdown(SocketShutdown.Both);
				}
				else
				{
					do
					{
						len = cli.Receive(recv);
					}
					while (len > 0);
				}
				cli.Close();
				Socket.Close();
				Socket = null;
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					System.Threading.Thread.Sleep(250);
					Start();
				}
				else
				{
					throw;
				}
			}
		}
	}


	[TestFixture]
	[Quick]
	[Peach]
	class TcpPublisherTests : DataModelCollector
	{
		public string template = @"
<Peach>

	<DataModel name=""TheDataModel"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<DataModel name=""TheDataModel2"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<StateModel name=""ListenState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Accept"" type=""accept""/>

			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""{0}""/>
		<Publisher class=""{1}"">
			<Param name=""{2}"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{3}""/>
			<Param name=""RetryMode"" value=""{4}""/>
		</Publisher>
	</Test>

</Peach>
";
		public string templateListener = @"
<Peach>

	<DataModel name=""TheDataModel"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<DataModel name=""TheDataModel2"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<StateModel name=""ListenState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Accept"" type=""accept""/>

			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""{0}""/>
		<Publisher class=""{1}"">
			<Param name=""{2}"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{3}""/>
		</Publisher>
	</Test>

</Peach>
";
		public void TcpServer(bool clientShutdown)
		{
			var port = TestBase.MakePort(55000, 56000);
			var cli = new SimpleTcpClient(port, clientShutdown);
			cli.Start();

			var xml = string.Format(templateListener, "ListenState", "TcpListener", "Interface", port);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration { singleIteration = true };

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(2, actions.Count);

			var de1 = actions[0].dataModel.find("TheDataModel.str");
			Assert.NotNull(de1);
			var de2 = actions[1].dataModel.find("TheDataModel2.str");
			Assert.NotNull(de2);

			var send = (string)de2.DefaultValue;
			var recv = (string)de1.DefaultValue;

			Assert.AreEqual("Hello World", send);
			Assert.AreEqual("Test buffer", recv);

			Assert.NotNull(cli.Result);
			Assert.AreEqual("Hello World", cli.Result);
		}

		[Test]
		public void TestListenShutdownClient()
		{
			// Test TcpListener deals with client initiating shutdown
			TcpServer(true);
		}

		[Test]
		public void TestListenShutdownServer()
		{
			// Test TcpListener deals with it initiating shutdown
			TcpServer(false);
		}

		public void TcpClient(bool serverShutdown)
		{
			var port = TestBase.MakePort(56000, 57000);
			var cli = new SimpleTcpServer(port, serverShutdown);
			cli.Start();

			var xml = string.Format(template, "ClientState", "TcpClient", "Host", port, "Always");

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration { singleIteration = true };

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(2, actions.Count);

			var de1 = actions[0].dataModel.find("TheDataModel.str");
			Assert.NotNull(de1);
			var de2 = actions[1].dataModel.find("TheDataModel2.str");
			Assert.NotNull(de2);

			var send = (string)de2.DefaultValue;
			var recv = (string)de1.DefaultValue;

			Assert.AreEqual("Hello World", send);
			Assert.AreEqual("Test buffer", recv);

			Assert.NotNull(cli.Result);
			Assert.AreEqual("Hello World", cli.Result);
		}

		[Test]
		public void TestClientShutdownClient()
		{
			// Test TcpClient deals with itself initiating shutdown
			TcpClient(false);
		}

		[Test]
		public void TestClientShutdownServer()
		{
			// Test TcpListener deals with client initiating shutdown
			TcpClient(true);
		}

		[Test]
		public void TestConnectRetry()
		{
			// Should throw PeachException if unable to connect during a control iteration
			var xml = string.Format(template, "ClientState", "TcpClient", "Host", 20, "Always");
			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration { singleIteration = true };

			var e = new Engine(this);

			var sw = new Stopwatch();
			sw.Start();

			Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));

			sw.Stop();

			var delta = sw.ElapsedMilliseconds;

			Assert.Less(delta, 10500);
			Assert.Greater(delta, 10000);
		}

		[Test]
		public void TestConnectRetry2()
		{
			// Should throw PeachException if unable to connect during a control iteration
			var xml = string.Format(template, "ClientState", "TcpClient", "Host", 20, "FirstAndAfterFault");
			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration { singleIteration = true };

			var e = new Engine(this);

			var sw = new Stopwatch();
			sw.Start();

			Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));

			sw.Stop();

			var delta = sw.ElapsedMilliseconds;

			Assert.Less(delta, 10500);
			Assert.Greater(delta, 10000);
		}

		[Test]
		public void TestConnectRetryNever()
		{
			// Should throw PeachException if unable to connect during a control iteration
			var xml = string.Format(template, "ClientState", "TcpClient", "Host", 20, "Never");
			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration { singleIteration = true };

			var e = new Engine(this);

			var sw = new Stopwatch();
			sw.Start();

			Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));

			sw.Stop();

			var delta = sw.ElapsedMilliseconds;

			Assert.Less(delta, 8500);
		}

		[Test]
		public void TestConnectFaultOnConnectFailure()
		{
			var port = TestBase.MakePort(45000, 46000);
			SimpleTcpServer cli;

			// Should throw PeachException if unable to connect during a control iteration
			var xml = @"
<Peach>

	<DataModel name=""TheDataModel"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<DataModel name=""TheDataModel2"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<StateModel name=""ListenState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Accept"" type=""accept""/>

			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"" targetLifetime=""iteration"">
		<StateModel ref=""{0}""/>
		<Publisher class=""{1}"">
			<Param name=""{2}"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{3}""/>
			<Param name=""RetryMode"" value=""{4}""/>
		</Publisher>
	</Test>

</Peach>
".Fmt("ClientState", "TcpClient", "Host", port, "Never");
			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var fault = false;

			var config = new RunConfiguration { range = true, rangeStart = 1, rangeStop = 1 };

			var e = new Engine(this);
			e.IterationStarting += (context, iteration, iterations) =>
			{
				if (!context.controlIteration) return;
				cli = new SimpleTcpServer(port, false, false);
				cli.Start();
			};

			e.Fault += (context, iteration, model, data) =>
			{
				fault = true;
			};

			e.startFuzzing(dom, config);
			Assert.IsTrue(fault);
		}

		[Test]
		public void TestRecvTimeout()
		{
			var port = TestBase.MakePort(45000, 46000);
			var cli = new SimpleTcpServer(port, false, false);
			cli.Start();

			var xml = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<Choice>
			<String value=""00"" token=""true""/>
			<String value=""01"" token=""true""/>
			<String value=""02"" token=""true""/>
			<String value=""03"" token=""true""/>
			<String value=""04"" token=""true""/>
			<String value=""05"" token=""true""/>
			<String value=""06"" token=""true""/>
			<String value=""07"" token=""true""/>
			<String value=""08"" token=""true""/>
			<String value=""09"" token=""true""/>
			<String name=""empty"" length=""0""/>
		</Choice>
	</DataModel>

	<StateModel name=""SM"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""SM""/>
		<Publisher class=""TcpClient"">
			<Param name=""Host"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{0}""/>
			<Param name=""Timeout"" value=""1000""/>
		</Publisher>
	</Test>
</Peach>".Fmt(port);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(this);

			var sw = new Stopwatch();
			sw.Start();
			e.startFuzzing(dom, config);
			sw.Stop();

			var delta = sw.ElapsedMilliseconds;

			Assert.Less(delta, 2000);
			Assert.Greater(delta, 1000);
		}

		[Test]
		public void TestResetRecvTimeout()
		{
			/*
			 * Use a lazy echo server that waits 1/2 sec to reply.
			 * Have a 1sec input timeout on the tcp publisher
			 * Should sucessfully receive data in every input action
			 */
			using (var cli = new SimpleTcpEcho())
			{
				var xml = @"
<Peach>
	<DataModel name=""input"">
		<Choice>
			<String length=""100""/>
			<String />
		</Choice>
	</DataModel>

	<DataModel name=""output"">
		<String name=""str""/>
	</DataModel>

	<StateModel name=""SM"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action type=""output"">
				<DataModel ref=""output""/>
				<Data>
					<Field name=""str"" value=""Hello""/>
				</Data>
			</Action>
			<Action type=""input"">
				<DataModel ref=""input""/>
			</Action>
			<Action type=""output"">
				<DataModel ref=""output""/>
				<Data>
					<Field name=""str"" value=""World""/>
				</Data>
			</Action>
			<Action type=""input"">
				<DataModel ref=""input""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""SM""/>
		<Publisher class=""TcpClient"">
			<Param name=""Host"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{0}""/>
			<Param name=""Timeout"" value=""1000""/>
		</Publisher>
	</Test>
</Peach>".Fmt(cli.LocalPort);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
				var config = new RunConfiguration { singleIteration = true };
				var e = new Engine(this);

				e.startFuzzing(dom, config);

				Assert.AreEqual("Hello", dom.tests[0].stateModel.states["InitialState"].actions[0].dataModel.InternalValue.BitsToString());
				Assert.AreEqual("Hello", dom.tests[0].stateModel.states["InitialState"].actions[1].dataModel.InternalValue.BitsToString());
				Assert.AreEqual("World", dom.tests[0].stateModel.states["InitialState"].actions[2].dataModel.InternalValue.BitsToString());
				Assert.AreEqual("World", dom.tests[0].stateModel.states["InitialState"].actions[3].dataModel.InternalValue.BitsToString());
			}
		}

		[Test]
		public void TestSendTimeout()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();

			var xml = @"
<Peach>
	<DataModel name='output'>
		<String length='1000000'/>
	</DataModel>

	<StateModel name=""SM"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action type=""output"">
				<DataModel ref=""output""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""SM""/>
		<Publisher class=""TcpClient"">
			<Param name=""Host"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{0}""/>
			<Param name=""SendTimeout"" value=""0""/>
		</Publisher>
	</Test>
</Peach>".Fmt(((IPEndPoint)listener.LocalEndpoint).Port);

			var ex = Assert.Throws<PeachException>(() =>
			{
				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
				var config = new RunConfiguration { singleIteration = true };
				var e = new Engine(this);

				e.startFuzzing(dom, config);
			});
			listener.Stop();
			StringAssert.Contains("The operation has timed", ex.Message);
		}


		[Test]
		public void TestSessionStop()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='InitialState'>
		<State name='InitialState'>
			<Action type='open' name='open' />
			<Action type='output' name='output'>
				<DataModel name='DM'>
					<Blob value='Hello' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>

		<Publisher class='Tcp'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='Port' value='0'/>
			<Param name='Lifetime' value='session'/>
		</Publisher>
	</Test>
</Peach>";

			var dom = ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);
			var localEp = new IPEndPoint(IPAddress.Loopback, 0);
			var l = new TcpListener(localEp);

			byte[] rx = null;
			Socket cli = null;
			var len = 0;

			l.Start();
			localEp.Port = ((IPEndPoint)l.LocalEndpoint).Port;

			((TcpClientPublisher)dom.tests[0].publishers[0]).Port = (ushort)localEp.Port;

			e.TestStarting += ctx =>
			{
				ctx.ActionFinished += (c, a) =>
				{
					if (a.Name == "open")
					{
						cli = l.AcceptSocket();
					}
					else if (a.Name == "output")
					{
						Assert.NotNull(cli, "Client should be non-null");

						rx = new byte[5];
						len = cli.Receive(rx);

						cli.Shutdown(SocketShutdown.Send);
					}
				};
			};

			e.startFuzzing(dom, cfg);

			Assert.NotNull(cli, "Should have gotten connection");
			Assert.NotNull(rx, "Should have gotten msg");

			Assert.AreEqual(rx.Length, len);

			len = cli.Receive(rx);

			Assert.AreEqual(0, len, "Remote side should have closed");

			cli.Close();
			l.Stop();
		}


		[Test]
		public void TestSoftExceptionControlIteration()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='InitialState'>
		<State name='InitialState'>
			<Action type='open' name='open' />
			<Action type='output' name='output'>
				<DataModel name='DM'>
					<Blob value='Hello' />
				</DataModel>
			</Action>
			<Action type='input' name='input'>
				<DataModel name='DM'>
					<Blob length='5' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='1'>
		<StateModel ref='SM'/>

		<Publisher class='Tcp'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='Port' value='0'/>
			<Param name='Lifetime' value='session'/>
		</Publisher>
	</Test>
</Peach>";

			var dom = ParsePit(xml);
			var cfg = new RunConfiguration { range = true, rangeStart = 1, rangeStop = 2 };
			var e = new Engine(null);
			var localEp = new IPEndPoint(IPAddress.Loopback, 0);
			var l = new TcpListener(localEp);

			byte[] rx = null;
			Socket cli = null;
			var len = 0;

			l.Start();
			localEp.Port = ((IPEndPoint)l.LocalEndpoint).Port;

			((TcpClientPublisher)dom.tests[0].publishers[0]).Port = (ushort)localEp.Port;

			e.TestStarting += ctx =>
			{
				ctx.ActionFinished += (c, a) =>
				{
					if (a.Name == "open")
					{
						cli = l.AcceptSocket();
					}
					else if (a.Name == "output")
					{
						Assert.NotNull(cli, "Client should be non-null");

						rx = new byte[5];
						len = cli.Receive(rx);

						if (c.controlRecordingIteration)
						{
							cli.Send(rx, len, SocketFlags.None);
							cli.Shutdown(SocketShutdown.Send);
						}
						else if (c.reproducingFault && c.controlIteration && c.reproducingIterationJumpCount == 0)
						{
							cli.Send(rx, len, SocketFlags.None);
							cli.Shutdown(SocketShutdown.Send);
						}

						cli.Close();
					}
				};
			};

			var f = new List<Fault[]>();
			e.Fault += (c, it, sm, faults) =>
			{
				f.Add(faults);
			};

			e.startFuzzing(dom, cfg);

			Assert.NotNull(cli, "Should have gotten connection");
			Assert.NotNull(rx, "Should have gotten msg");

			l.Stop();

			Assert.AreEqual(1, f.Count);
			Assert.AreEqual(1, f[0].Length);

			var msg = f[0][0].description;

			StringAssert.Contains("Timed out waiting for input from the publisher.", msg);
		}
	}
}
