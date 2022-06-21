﻿

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Peach.Core.Test;
using Logger = NLog.Logger;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class ArrayTests
	{

		[Test]
		public void CrackUrl()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String />" +
				"		<String value=\"://\" token=\"true\" />" +
				"		<String />" +
				"		<String value=\"/\" token=\"true\" />" +
				"		<String />" +
				"		<String value=\"?\" token=\"true\" />" +

				"		<Block name=\"TheArray\" minOccurs=\"0\" maxOccurs=\"100\">" +
				"		  <String name=\"key1\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value1\" />" +
				"			<String value=\"&amp;\" token=\"true\" />" +
				"		</Block>" +

				"		<Block name=\"EndBlock\">" +
				"		  <String name=\"key2\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value2\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			// Positive test

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "http://www.foo.com/crazy/path.cgi?k1=v1&k2=v2&k3=v3");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, ((Peach.Core.Dom.Array)dom.dataModels[0]["TheArray"]).Count);
			Assert.AreEqual("k3", (string)((Peach.Core.Dom.Block)dom.dataModels[0]["EndBlock"])["key2"].InternalValue);
			Assert.AreEqual("v3", (string)((Peach.Core.Dom.Block)dom.dataModels[0]["EndBlock"])["value2"].InternalValue);
		}

		[Test]
		public void CrackUrl2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"?\" token=\"true\" />" +

				"		<Block name=\"TheArray\" minOccurs=\"0\" maxOccurs=\"100\">" +
				"		  <String name=\"key1\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value1\" />" +
				"			<String value=\"&amp;\" token=\"true\" />" +
				"		</Block>" +
				"		<String name=\"key2\" />" +
				"		<String value=\"=\" token=\"true\" />" +
				"		<String name=\"value2\" />" +
				"	</DataModel>" +
				"</Peach>";

			// Positive test

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "?k1=v1&k2=v2&k3=v3");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, ((Peach.Core.Dom.Array)dom.dataModels[0]["TheArray"]).Count);
			Assert.AreEqual("k3", (string)dom.dataModels[0]["key2"].InternalValue);
			Assert.AreEqual("v3", (string)dom.dataModels[0]["value2"].InternalValue);
		}

		[Test]
		public void CrackArrayBlob1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"1\" minOccurs=\"1\" maxOccurs=\"100\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];

			Assert.AreEqual(6, array.Count);
			Assert.AreEqual(new byte[] { 1 }, array[0].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 6 }, array[5].InternalValue.BitsToArray());
		}

		/// <summary>
		/// We should stop cracking at maxOccurs.  Question, should we throw an exception or just
		/// stop?
		/// </summary>
		[Test]
		public void CrackArrayStopAtMax()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"1\" minOccurs=\"1\" maxOccurs=\"3\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];

			Assert.AreEqual(3, array.Count);
			Assert.AreEqual(new byte[] { 1 }, array[0].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 2 }, array[1].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 3 }, array[2].InternalValue.BitsToArray());
		}

		[Test]
		public void CrackArrayVerifyMin()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"1\" minOccurs=\"4\" maxOccurs=\"6\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3 });

			try
			{
				DataCracker cracker = new DataCracker();
				cracker.CrackData(dom.dataModels[0], data);
				Assert.True(false);
			}
			catch (CrackingFailure)
			{
				Assert.True(true);
			}
		}

		[Test]
		public void CrackArrayBlobZeroMore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"1\" minOccurs=\"0\" maxOccurs=\"100\" />" +
				"		<Blob name=\"Rest\" length=\"6\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];

			Assert.AreEqual(0, array.Count);

			Blob rest = (Blob)dom.dataModels[0].find("Rest");

			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, rest.InternalValue.BitsToArray());
		}

		[Test]
		public void CrackZeroArray()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"Item\" minOccurs=\"0\" token=\"true\" />" +
				"		<Blob name=\"Rest\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];

			Assert.AreEqual(0, array.Count);

			Blob rest = (Blob)dom.dataModels[0].find("Rest");

			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, rest.InternalValue.BitsToArray());
		}

		[Test]
		public void CrackArrayRelation()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"count\" of=\"TheArray\"/>" +
				"		</Number>" +
				"		<Blob name=\"TheArray\" length=\"1\" minOccurs=\"0\" maxOccurs=\"100\" />" +
				"		<Blob name=\"Rest\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 3, 1, 2, 3, 4, 5, 6 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][1];
			Blob blob = (Blob)dom.dataModels[0][2];

			Assert.AreEqual(3, array.Count);
			Assert.AreEqual(new byte[] { 1 }, array[0].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 2 }, array[1].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 3 }, array[2].InternalValue.BitsToArray());
			Assert.AreEqual(new byte[] { 4, 5, 6 }, blob.InternalValue.BitsToArray());
		}

		[Test]
		public void CrackArrayOfOne()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\" nullTerminated=\"true\" minOccurs=\"1\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello\x00");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];
			Assert.AreEqual(1, array.Count);
			string str = (string)array[0].InternalValue;

			Assert.AreEqual("Hello", str);
		}

		[Test]
		public void CrackArrayOfZeroOrOne()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\" nullTerminated=\"true\" minOccurs=\"0\" maxOccurs=\"1\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello\x00");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];
			Assert.AreEqual(1, array.Count);
			string str = (string)array[0].InternalValue;

			Assert.AreEqual("Hello", str);
		}

		[Test]
		public void CrackArrayWithinArray()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block occurs=\"3\">" +
				"			<Number name=\"num1\" size=\"8\" minOccurs=\"0\" constraint=\"str(element.DefaultValue) != '0'\" />" +
				"			<Number name=\"num2\" size=\"8\" valueType=\"hex\" value=\"00\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x02, 0x03, 0x00, 0x04, 0x00, 0x00 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array blockArray = dom.dataModels[0][0] as Peach.Core.Dom.Array;
			Assert.NotNull(blockArray);
			Assert.AreEqual(3, blockArray.Count);
			Block firstBlock = blockArray[0] as Block;
			Assert.NotNull(firstBlock);
			Peach.Core.Dom.Array numArray = firstBlock[0] as Peach.Core.Dom.Array;
			Assert.NotNull(numArray);
			Assert.AreEqual(3, numArray.Count);
		}

		[Test]
		public void CrackArrayWithTokenSibling()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block minOccurs=\"0\">" +
				"			<Number name=\"num1\" size=\"8\" constraint=\"str(element.DefaultValue) != '0'\" />" +
				"		</Block>" +
				"		<Number name=\"zero\" size=\"8\" valueType=\"hex\" value=\"00\" token=\"true\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x02, 0x03, 0x00 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			Peach.Core.Dom.Array blockArray = dom.dataModels[0][0] as Peach.Core.Dom.Array;
			Assert.NotNull(blockArray);
			Assert.AreEqual(3, blockArray.Count);

		}

		[Test]
		public void CrackArrayParentName()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\" nullTerminated=\"true\" minOccurs=\"1\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello\x00World\x00");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];
			Assert.AreEqual(2, array.Count);
			Assert.AreEqual("TheDataModel.str1", array.fullName);
			Assert.AreEqual("TheDataModel.str1.str1", array.OriginalElement.fullName);
			Assert.AreEqual("TheDataModel.str1.str1_0", array[0].fullName);
			Assert.AreEqual("TheDataModel.str1.str1_1", array[1].fullName);
		}

		[Test]
		public void CrackArrayEmptyElement()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block minOccurs=\"10\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (byte)0);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];
			Assert.AreEqual(10, array.Count);
		}

		[Test]
		public void CrackArrayEmptyElementMin()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block minOccurs=\"0\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (byte)0);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][0];
			Assert.AreEqual(0, array.Count);
		}

		[Test]
		public void TokenAfterArray()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""A"">
			<Block name=""block1"" minOccurs=""0"">
				<Number size=""8"" value=""11"" token=""true""/> 
				<Number size=""8"" constraint=""str(element.DefaultValue) != '0'""/> 
			</Block>
			<Block name=""block2"">
				<Number size=""8"" value=""11"" token=""true""/> 
			</Block>
			<Number name=""end"" size=""8"" value=""0""/>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 11, 1, 11, 2, 11, 3, 11, 0 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var block = dom.dataModels[0][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);
			Assert.AreEqual(3, block.Count);
			var array = block[0] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);
		}

		[Test]
		public void TokenAfterArrayInArray()
		{
			string xml = @"
<Peach>
	<Import import='re'/>

	<DataModel name='TupleLine'>
		<String name='LineHeader' />
		<Block name='ValueTuples' minOccurs='0' maxOccurs='5'>
			<String name='CommaSeparator' value=',' token='true' />
			<String name='TupleValue' constraint='re.search(""^[A-Za-z0-9]+$"", value) != None' />
		</Block>
		<String name='LineTerminator' value='\n' token='true' />
	</DataModel>

	<DataModel name='TheDataModel'>
		<Block name='FirstLine' ref='TupleLine'>
			<String name='LineHeader' value='CIRCLE' token='true' />
		</Block>

		<Block name='SecondLine' ref='TupleLine'>
			<String name='LineHeader' value='RECT' token='true' />
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "CIRCLE,0,50\nRECT,0\n");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(2, dom.dataModels[1].Count);

			var b1 = dom.dataModels[1][0] as Block;
			Assert.NotNull(b1);
			Assert.AreEqual(3, b1.Count);
			var b1_array = b1[1] as Peach.Core.Dom.Array;
			Assert.AreEqual(2, b1_array.Count);

			var b2 = dom.dataModels[1][1] as Block;
			Assert.NotNull(b2);
			Assert.AreEqual(3, b2.Count);
			var b2_array = b2[1] as Peach.Core.Dom.Array;
			Assert.AreEqual(1, b2_array.Count);
		}

		[Test]
		public void InvalidFieldTest()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Root'>
			<Blob name='data'/>
		</Block>
		<Blob/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Root.data.str2' value='foo' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));

			Assert.AreEqual("Error, action \"Initial.Action\" unable to resolve field \"str2\" of \"Root.data.str2\" against \"DM\" (DataModel).", ex.Message);
		}

		class DeferredPublisher : StreamPublisher
		{
			static readonly Logger ClassLogger = NLog.LogManager.GetCurrentClassLogger();

			public DeferredPublisher(string payload)
				: base(new Dictionary<string, Variant>())
			{
				var bytes = Encoding.ASCII.GetBytes(payload);

				stream = new MemoryStream();
				stream.Write(bytes, 0, bytes.Length);
				stream.Position = 0;
			}

			protected override Logger Logger
			{
				get { return ClassLogger; }
			}

			public override void WantBytes(long count)
			{
				if (stream.Position + count > stream.Length)
					Assert.Fail("Publisher shouldn't be asking for more bytes!");
			}
		}

		[Test]
		public void TokenAfterArrayMoreData()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='blk' minOccurs='0'>
			<Choice name='c'>
				<Block name='b'>
					<String name='key' />
					<String name='delim' value=': '  token='true' />
					<String name='value' />
					<String name='eol' value='\r\n' token='true' />
				</Block>
			</Choice>
		</Block>
		<Block>
			<String name='end' value='\r\n' token='true' />
		</Block>
		<String name='body' />
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var pub = new DeferredPublisher("Foo: Bar\r\nBaz: Qux\r\n\r\n");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], new BitStream(pub));

			Assert.AreEqual("", (string)dom.dataModels[0][2].InternalValue);
		}

		[Test]
		public void TokenAfterArrayMoreData2()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='blk' minOccurs='0'>
			<Choice name='c'>
				<Block name='b'>
					<String name='key' />
					<String name='delim' value=': '  token='true' />
					<String name='value' />
					<String name='eol' value='\r\n' token='true' />
				</Block>
			</Choice>
		</Block>
		<Block>
			<String />
			<String name='end' value='\r\n' token='true' />
		</Block>
		<String name='body' />
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var pub = new DeferredPublisher("Foo: Bar\r\nBaz: Qux\r\n\r\n");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], new BitStream(pub));

			Assert.AreEqual("", (string)dom.dataModels[0][2].InternalValue);
		}


		[Test]
		public void TokenAfterArrayWithTokenMoreData()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String />
		<Block minOccurs='0'>
			<String value=','  token='true' />
			<String />
		</Block>
		<String value='\n' token='true' />
		<String />
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var pub = new DeferredPublisher("a,b,c\ndone");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], new BitStream(pub));

			Assert.AreEqual("done", (string)dom.dataModels[0][3].InternalValue);
		}

	}
}

// end
