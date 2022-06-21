﻿

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using ASCIIEncoding = Peach.Core.ASCIIEncoding;
using Encoding = Peach.Core.Encoding;
using NLog;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class RelationSizeTest : DataModelCollector
	{
		[Test]
		public void BasicTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"12345\"/>" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Number num = dom.dataModels[0][1] as Number;

			Variant val = num.InternalValue;
			Assert.AreEqual(5, (int)val);
		}

		[Test]
		public void BasicTestParent()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"12345\"/>" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"size\" of=\"TheDataModel\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Number num = dom.dataModels[0][1] as Number;

			Variant val = num.InternalValue;
			Assert.AreEqual(6, (int)val);
		}

		[Test]
		public void ExpressionGet()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" expressionGet=\"size + 5\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 6, "Hello WorldAAAAAAAAAAA");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].InternalValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].InternalValue.BitsToString());
		}

		[Test]
		public void ExpressionSet()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"12345\"/>" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"size\" of=\"TheDataModel\" expressionSet=\"size + 10\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Number num = dom.dataModels[0][1] as Number;

			Variant val = num.InternalValue;
			Assert.AreEqual(6 + 10, (int)val);
		}

		[Test]
		public void RelationInRelation()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"TheBlock\">"+
				"			<Blob name=\"Data\" value=\"12345\"/>" +
				"			<Number size=\"8\">" +
				"				<Relation type=\"size\" of=\"Data\" />" +
				"			</Number>" +
				"		</Block>"+
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"TheBlock\" />" +
				"		</Number>" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"TheDataModel\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Number num1 = ((Block)dom.dataModels[0][0])[1] as Number;
			Number num2 = dom.dataModels[0][1] as Number;
			Number num3 = dom.dataModels[0][2] as Number;

			Assert.AreEqual(5, (int)num1.InternalValue);
			Assert.AreEqual(6, (int)num2.InternalValue);
			Assert.AreEqual(8, (int)num3.InternalValue);
		}

		[Test]
		public void MultipleFromRelations()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"num\" size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data1\" />" +
				"			<Relation type=\"size\" of=\"Data2\" />" +
				"		</Number>" +
				"		<Blob name=\"Data1\" />" +
				"		<Blob name=\"Data2\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 5, "HelloWorldMore");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);

			Number num = dom.dataModels[0][0] as Number;
			Variant val = num.InternalValue;
			Assert.AreEqual(5, (int)val);

			Blob blob1 = dom.dataModels[0][1] as Blob;
			Variant val2 = blob1.InternalValue;
			Assert.AreEqual("Hello", val2.BitsToString());

			Blob blob2 = dom.dataModels[0][2] as Blob;
			Variant val3 = blob2.InternalValue;
			Assert.AreEqual("World", val3.BitsToString());
		}

		[Test]
		public void MultipleOfRelations()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"num1\" size=\"16\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" value=\"00 01 02 03 04 05 06 07\" valueType=\"hex\" >" +
				"			<Hint name=\"BlobMutator-How\" value=\"ExpandAllRandom\" />" + 
				"		</Blob>" +
				"		<Number name=\"num2\" size=\"16\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"	</DataModel>" +

				"	<StateModel name=\"TheState\" initialState=\"Initial\">" +
				"		<State name=\"Initial\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +

				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"			<Publisher class=\"Null\"/>" +
				"			<Strategy class=\"Random\"/>" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("BlobMutator");

			RunConfiguration config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 0;
			config.rangeStop = 9;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(10, dataModels.Count);

			for (int i = 0; i < 10; ++i)
			{
				var model = dataModels[i];

				Assert.AreEqual(3, model.Count);

				Number num1 = model[0] as Number;
				Variant val1 = num1.InternalValue;
				Assert.GreaterOrEqual((long)val1, 8);

				Blob blob = model[1] as Blob;
				Variant val2 = blob.InternalValue;
				int len = val2.BitsToArray().Length;
				Assert.GreaterOrEqual(len, 8);

				Number num2 = model[2] as Number;
				Variant val3 = num2.InternalValue;
				Assert.GreaterOrEqual((long)val3, 8);

				Assert.AreEqual(len, (long)val1);
				Assert.AreEqual(len, (long)val3);
			}
		}

		[Test]
		public void RelationOfDataModel()
		{
			string xml = @"
<Peach>
	<DataModel name=""TheModel"">
		<Number name=""marker"" value=""1"" size=""8"" token=""true""/>
		<Number name=""id"" value=""1"" size=""8""/> 
		<Number name=""length"" size=""16"">
			<Relation type=""size"" of=""TheModel""/>
		</Number>
		<String name=""value"" value=""Hello World!"" />
	</DataModel>

	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel name=""foo"" ref=""TheModel""/>
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Sequential""/>
	</Test>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Assert.IsTrue(dom.dataModels[0].Count == 4);

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, dataModels.Count);
			var actual = dom.dataModels[0].Value.ToArray();
			var expected = Encoding.ASCII.GetBytes("\x01\x01\x10\x00Hello World!");
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void RelationGetSet()
		{
			string xml = @"
<Peach>
	<DataModel name=""TheModel"">
		<Number size=""8"" mutable=""false""/>
		<Number name=""len"" size=""64"" mutable=""false"">
			<Relation type=""size"" of=""data"" expressionGet=""size+2"" expressionSet=""size-2""/>
		</Number>
		<Block name=""data"">
			<Blob length=""1""/>
			<Blob length=""2""/>
		</Block>
	</DataModel>

	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel name=""foo"" ref=""TheModel""/>
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Sequential""/>
	</Test>
</Peach>
";
			cloneActions = true;

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("DataElementRemove");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(4, actions.Count);

			byte[] act1 = Encoding.ASCII.GetBytes("\x00\x01\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			Assert.False(actions[0].error);
			Assert.AreEqual(act1, actions[0].dataModel.Value.ToArray());

			byte[] act2 = Encoding.ASCII.GetBytes("\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			Assert.False(actions[1].error);
			Assert.AreEqual(act2, actions[1].dataModel.Value.ToArray());

			byte[] act3 = Encoding.ASCII.GetBytes("\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			Assert.False(actions[2].error);
			Assert.AreEqual(act3, actions[2].dataModel.Value.ToArray());

			byte[] act4 = Encoding.ASCII.GetBytes("\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			Assert.False(actions[3].error);
			Assert.AreEqual(act4, actions[3].dataModel.Value.ToArray());
		}

		Dictionary<string, object> SpeedTest(uint repeat)
		{
			// When the data element duplicator clones blockData multiple times,
			// it becomes painfully slow to evaulate the final value of DM3

			string outter_xml = @"
<Peach>

<DataModel name=""DM3"">
	<Number name=""tag"" size=""32"" signed=""false"" endian=""big"">
		<Fixup class=""Crc32Fixup"">
			<Param name=""ref"" value=""blockData""/>
		</Fixup>
	</Number>
{0}
</DataModel>

</Peach>
";

			string inner_xml = @"
	<Block name=""blockData{0}"">
		<Number name=""CommandSize"" size=""32"" signed=""false"" endian=""big"">
			<Relation type=""size"" of=""DM3"" />
		</Number>
		<Number name=""CommandCode"" size=""32"" signed=""false"" endian=""big"">
			<Transformer class=""Md5""/>
		</Number>
	</Block>
";

			StringBuilder sb = new StringBuilder();
			sb.Append(string.Format(inner_xml, ""));
			for (int i = 0; i < repeat; ++i)
			{
				sb.Append(string.Format(inner_xml, "_" + i));
			}

			string xml = string.Format(outter_xml, sb.ToString());

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Assert.AreEqual(1, dom.dataModels.Count);

			int start = Environment.TickCount;

			var value = dom.dataModels[0].Value;

			int end = Environment.TickCount;

			Assert.NotNull(value);

			TimeSpan delta = TimeSpan.FromMilliseconds(end - start);

			Dictionary<string, object> ret = new Dictionary<string, object>();
			ret["delta"] = delta;
			ret["count"] = dom.dataModels[0].GenerateCount;
			return ret;
		}

		[Test]
		public void MeasureSpeed()
		{
			for (uint i = 0; i <= 25; i += 5)
			{
				var ret = SpeedTest(i);
				TimeSpan delta = (TimeSpan)ret["delta"];
				uint count = (uint)ret["count"];
				Assert.LessOrEqual(delta, TimeSpan.FromSeconds(30));
				Assert.AreEqual(count, i + 2);
			}
		}

		[Test]
		public void RelationInArray()
		{
			string xml = @"
<Peach>
	<DataModel name=""ElemModel"">
		<Number name=""length"" size=""16"" endian=""big"">
			<Relation type=""size"" of=""data""/>
		</Number>
		<Blob name=""data""/>
	</DataModel>

	<DataModel name=""DM"">
		<Number name=""tag"" size=""16"" endian=""big"" />
		<Number name=""length"" size=""16"" endian=""big"">
			<Relation type=""size"" of=""Elements""/>
		</Number>
		<Block name=""Elements"">
			<Block name=""Elem"" minOccurs=""0"" maxOccurs=""999"">
				<Block name=""Elem0"" ref=""ElemModel""/>
			</Block>
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Assert.AreEqual(2, dom.dataModels.Count);

			var data = Bits.Fmt("{0}", new byte[] { 0x00, 0x10, 0x00, 0x06, 0x00, 0x04, 0xde, 0xad, 0xbe, 0xef });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(3, dom.dataModels[1].Count);

			var tag = dom.dataModels[1][0] as Number;
			var len = dom.dataModels[1][1] as Number;
			var blk = dom.dataModels[1][2] as Block;

			Assert.NotNull(tag);
			Assert.NotNull(len);
			Assert.NotNull(blk);
			Assert.AreEqual(1, blk.Count);

			var arr = blk[0] as Peach.Core.Dom.Array;
			Assert.NotNull(arr);
			Assert.AreEqual(1, arr.Count);

			var elm = arr[0] as Block;
			Assert.NotNull(elm);
			Assert.AreEqual(1, elm.Count);

			var el0 = elm[0] as DataModel;
			Assert.NotNull(el0);
			Assert.AreEqual(2, el0.Count);

			var length = el0[0] as Number;
			var blob = el0[1] as Blob;

			Assert.NotNull(length);
			Assert.NotNull(blob);

			Assert.AreEqual(16, (int)tag.DefaultValue);
			Assert.AreEqual(6, (int)len.DefaultValue);
			Assert.AreEqual(4, (int)length.DefaultValue);

			var final = (BitStream)blob.DefaultValue;
			Assert.NotNull(final);

			var actual = final.ToArray();
			var expected = new byte[] { 0xde, 0xad, 0xbe, 0xef };

			Assert.AreEqual(actual, expected);
		}

		[Test]
		public void IncludeRelation()
		{
			string tmp = Path.GetTempFileName();

			string xml1 = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' endian='big'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
		<Block name='Value'/>
	</DataModel>
</Peach>";

			string xml2 = @"
<Peach>
	<Include ns='ns' src='{0}'/>

	<DataModel name='DM'>
		<Block ref='ns:TLV' name='Type1'>
			<Number name='Type' size='8' endian='big' value='201'/>
			<Block name='Value'>
				<Blob length='10' value='0000000000'/>
			</Block>
		</Block>
	</DataModel>
</Peach>".Fmt(tmp);

			File.WriteAllText(tmp, xml1);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml2)));

			var final = dom.dataModels[0].Value.ToArray();
			var expected = new byte[] { 201, 10, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48 };

			Assert.AreEqual(expected, final);
		}

		[Test]
		public void IncludeChildRelation()
		{
			string tmp = Path.GetTempFileName();

			string xml1 = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' endian='big'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
		<Block name='Out'>
			<Block name='Value'/>
		</Block>
	</DataModel>
</Peach>";

			string xml2 = @"
<Peach>
	<Include ns='ns' src='{0}'/>

	<DataModel name='DM'>
		<Block ref='ns:TLV' name='Type1'>
			<Number name='Type' size='8' endian='big' value='201'/>
			<Block name='Out'>
				<Block name='Value'>
					<Blob length='10' value='0000000000'/>
				</Block>
			</Block>
		</Block>
	</DataModel>
</Peach>".Fmt(tmp);

			File.WriteAllText(tmp, xml1);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml2)));

			var final = dom.dataModels[0].Value.ToArray();
			var expected = new byte[] { 201, 10, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48 };

			Assert.AreEqual(expected, final);
		}

		[Test]
		public void IncludeBadChildRelation()
		{
			string tmp = Path.GetTempFileName();

			string xml1 = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' endian='big'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
		<Block name='Out'>
			<Block name='Value'/>
		</Block>
	</DataModel>
</Peach>";

			string xml2 = @"
<Peach>
	<Include ns='ns' src='{0}'/>

	<DataModel name='DM'>
		<Block ref='ns:TLV' name='Type1'>
			<Number name='Type' size='8' endian='big' value='201'/>
			<Block name='Out'/>
		</Block>
	</DataModel>
</Peach>".Fmt(tmp);

			File.WriteAllText(tmp, xml1);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml2)));

			var final = dom.dataModels[0].Value.ToArray();
			var expected = new byte[] { 201, 0 };

			Assert.AreEqual(expected, final);
		}

		[Test]
		public void IncludeStateModelRelation()
		{
			string tmp = Path.GetTempFileName();

			string xml1 = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' endian='big' value='201'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
			<Block name='Value'>
				<Blob length='10' value='0000000000'/>
			</Block>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TLV'/>
			</Action>
		</State>
	</StateModel>
</Peach>";

			string xml2 = @"
<Peach>
	<Include ns='ns' src='{0}'/>

	<Test name='Default'>
		<StateModel ref='ns:TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>".Fmt(tmp);

			File.WriteAllText(tmp, xml1);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml2)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);
		}

		[Test]
		public void RelationOfDataModelWithinDataModel()
		{
			string xml = @"
<Peach>
	<DataModel name=""RefData"">
		<Number name=""Length"" size=""8"" endian=""big"">
			<Relation type=""size"" of=""RefData""/>
		</Number>
		<Blob name=""data"" value=""a""/>
	</DataModel>
	
	<DataModel name=""TheModel"">
		<Block ref=""RefData"">
			<Block name=""data"" ref=""RefData"">
					<Blob name=""data"" value=""0""/>
			</Block>
		</Block>
	</DataModel>

	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel name=""foo"" ref=""TheModel""/>
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Sequential""/>
	</Test>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Assert.IsTrue(dom.dataModels[0].Count == 2);

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, dataModels.Count);
			var actual = dom.dataModels[1].Value.ToArray();
			var expected = Encoding.ASCII.GetBytes("\x03\x02\x30");
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void RelationRefOVerride()
		{
			string xml = @"
<Peach>
	<DataModel name='SubBlock'>
		<Number name='BlockSize' size='8' signed='false'>
			<Relation type='size' of='Data' />
		</Number>
		<Block name='Data'/>
	</DataModel>

	<DataModel name='DerivedSubBlock' ref='SubBlock'>
		<String name='Data' value='Hello'/>
	</DataModel>

	<DataModel name='BasicBlock'>
		<Block name='SubBlock' ref='SubBlock'/>
	</DataModel>

	<DataModel name='DerivedBlock' ref='BasicBlock'>
		<Block name='SubBlock' ref='DerivedSubBlock'/>
	</DataModel>

</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[3].Value.ToArray();
			Assert.AreEqual(Encoding.ASCII.GetBytes("\x5Hello"), val);
		}

		[Test]
		public void RelationParentOverride()
		{
			string xml = @"
<Peach>

	<DataModel name=""DataModel1"">
		<Number name=""RelOrStatic"" size=""8"">
			<Relation type=""size"" of=""Data""/>
		</Number>
		<Blob name=""Data"" value=""AB""/>
	</DataModel>

	<DataModel name=""DataModel2"" ref=""DataModel1"">
		<Number name=""RelOrStatic"" size=""8"" value=""0x40""/>
	</DataModel>

</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[1].Value.ToArray();
			Assert.AreEqual(Encoding.ASCII.GetBytes("\x40\x41\x42"), val);

			var copy = dom.dataModels[1].Clone();
			val = copy.Value.ToArray();
			Assert.AreEqual(Encoding.ASCII.GetBytes("\x40\x41\x42"), val);
		}

		[Test]
		public void RelatioDeepOverride()
		{
			string xml = @"
<Peach>

	<DataModel name=""DataModel1"">
		<Number name=""RelOrStatic"" size=""8"">
			<Relation type=""size"" of=""Data""/>
		</Number>
		<Blob name=""Data"" value=""AB""/>
	</DataModel>

	<DataModel name=""DataModel2"">
		<Choice name=""opts"" minOccurs=""0"">
			<Block name=""blk1"" ref=""DataModel1"">
				<Number name=""RelOrStatic"" size=""8"" value=""0x40""/>
				<Blob name=""Data"" value=""One""/>
			</Block>
			<Block name=""blk2"" ref=""DataModel1"">
				<Blob name=""Data"" value=""Two""/>
			</Block>
		</Choice>
	</DataModel>

	<DataModel name=""DataModel3"">
		<Block ref=""DataModel2""/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var copy = dom.dataModels[2].Clone();

			var array = copy.find("opts") as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);

			array.Add(array.OriginalElement.Clone("one"));
			array.Add(array.OriginalElement.Clone("two"));

			var c1 = array[0] as Choice;
			c1.SelectElement(c1.choiceElements[0]);

			var c2 = array[1] as Choice;
			c2.SelectElement(c2.choiceElements[1]);

			var final = copy.Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("\x40One\x03Two");
			Assert.AreEqual(exp, final);
		}

		[Test]
		public void NoCacheGetValue()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='Size' size='8' signed='false'>
			<Relation type='size' of='DM' />
		</Number>
		<String name='Value'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// Invalidate 'Value', thus invalidating 'DM'
			dom.dataModels[0][1].DefaultValue = new Variant("Hello");

			// Get InternalValue from 'Size' when 'DM' is invalidated
			uint size = (uint)dom.dataModels[0][0].InternalValue;

			Assert.AreEqual(6, size);

			// Ensure 'DM' has proper value
			byte[] actual = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(Encoding.ASCII.GetBytes("\x6Hello"), actual);
		}

		[Test]
		public void LengthInBits()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='Size' size='8' signed='false'>
			<Relation type='size' of='DM' lengthType='bits' />
		</Number>
		<String value='Hello'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			byte[] actual = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(Encoding.ASCII.GetBytes("\x30Hello"), actual);
		}

		[Test]
		public void OfModelRefInChoice()
		{
			const string xml = @"
<Peach>
	<DataModel name='RequestModel'>
		<Number name='Size' size='16' endian='big' signed='false'>
			<Relation type='size' of='RequestModel' />
		</Number>
		<Blob name='Value' value='Hello'/>
	</DataModel>

	<DataModel name='TheRequest'>
		<Number name='MessageSize' endian='big' size='16' signed='false'>
			<Relation type='size' of='Message' />
		</Number>

		<Block name='Message'>
			<Blob name='Header' value='**' />
			<Choice name='Command'>
				<Block name='Request' ref='RequestModel' />
			</Choice>
		</Block>
	</DataModel>
</Peach>
";

			var dom = ParsePit(xml);

			var final = dom.dataModels[1].Value.ToArray();
			var expected = Encoding.ASCII.GetBytes("\x00\x09**\x00\x07Hello");
			Assert.AreEqual(expected, final);

			var req = dom.dataModels[1].find("TheRequest.Message.Command.Request.Size");

			Assert.NotNull(req);
			Assert.AreEqual(1, req.relations.Count);
			Assert.AreEqual("TheRequest.Message.Command.Request", req.relations[0].Of.fullName);
		}



		class BytePublisher : Peach.Core.Publishers.StreamPublisher
		{
			private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
			protected override NLog.Logger Logger { get { return logger; } }

			readonly MemoryStream _data;

			public BytePublisher(byte[] data)
				: base(new Dictionary<string, Variant>())
			{
				Name = "Pub";
				_data = new MemoryStream(data);
			}

			protected override void OnOpen()
			{
				_data.Seek(0, SeekOrigin.Begin);
				this.stream = new MemoryStream();
			}

			public override void WantBytes(long count)
			{
				var buf = new byte[count];
				var len = _data.Read(buf, 0, (int)count);
				var pos = stream.Position;
				stream.Seek(0, SeekOrigin.End);
				stream.Write(buf, 0, len);
				stream.Seek(pos, SeekOrigin.Begin);
			}
		}

		[Test]
		public void RelationMaintainedShallowClones()
		{
			const string xml = @"
<Peach>
	<DataModel name='TLV'>
		<Number name='Type' size='8' value='0'/>
		<Number name='Length' size='8'>
			<Relation type='size' of='Value'/>
		</Number>
		<Blob name='Value'/>
	</DataModel>

	<DataModel name='Response'>
		<Choice name='Items' minOccurs='2'>
			<Block name='A' ref='TLV'>
				<Number name='Type' size='8' token='true' value='1'/>
			</Block>
			<Block name='B' ref='TLV'>
				<Number name='Type' size='8' token='true' value='2'/>
			</Block>
		</Choice>
	</DataModel>
</Peach>
";

			var dom = ParsePit(xml);
			var dm = dom.dataModels[1];

			// When DataModels are used for cracking in the state model
			// a clone of an evaluated model is used. So make sure we call
			// .Value prior to cracking.  The array will be expanded to
			// two items and bot will select 'A'
			var val = dm.Value;
			Assert.AreEqual(new byte[] { 0x01, 0x00, 0x01, 0x00 }, val.ToArray());

			var chocies = dm.Walk().OfType<Choice>().ToList();
			Assert.AreEqual(2, chocies.Count);

			foreach (var item in chocies)
			{
				Assert.NotNull(item.SelectedElement);

				// The selected element should be a clone of one of the choice elements
				CollectionAssert.DoesNotContain(item.choiceElements, item.SelectedElement);
			}

			// Change second item to a 'B'
			var payload = new byte[]
			{
				0x01, 0x03, 0x61, 0x62, 0x63,   // first item (an A)
				0x02, 0x03, 0x61, 0x62, 0x63,   // second item (an B)
			};

			var cracker = new DataCracker();
			cracker.CrackData(dm, new BitStream(payload));

			Assert.AreEqual(payload, dm.Value.ToArray());
		}

		[Test]
		public void ExpressionGetSetUlong()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Number size='64' signed='false' endian='big'>
			<Relation type='size' of='Item' expressionGet='size &amp; 0x00ffffffffffffff' expressionSet='size | 0xff00000000000000' />
		</Number>
		<Blob name='Item' valueType='hex' value='00 00 00' />
	</DataModel>

	<DataModel name='DM2' ref='DM1' />
</Peach>
";

			var dom = ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0 }, val);

			var data = new BitStream(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0 });

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			var blob = (Blob)dom.dataModels[1][1];

			Assert.AreEqual(new byte[] { 0, 0, 0, 0}, ((BitwiseStream)blob.DefaultValue).ToArray());
		}
	}
}

// end
