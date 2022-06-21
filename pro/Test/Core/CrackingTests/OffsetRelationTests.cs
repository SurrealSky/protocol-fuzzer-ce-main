﻿

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class OffsetRelationTests
	{
		[Test]
		public void TooBigOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 20, "Hello World");

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("String 'TheDataModel.Data' failed to crack. Offset is 160 bits but buffer only has 96 bits.", ex.Message);
		}

		[Test]
		public void TooSmallOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 0, "Hello World");

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("String 'TheDataModel.Data' failed to crack. Offset is 0 bits but already read 8 bits.", ex.Message);
		}

		[Test]
		public void BasicOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2}", offsetdata.Length + 1, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(offsetdata.Length + 1, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void Basic2Offset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"5\"/>" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "12345";
			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			var data = Bits.Fmt("{0}{1:L8}{2}{3}", otherdata, offsetdata.Length + 1 + otherdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(offsetdata.Length + 1 + otherdata.Length, (int)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][2].DefaultValue.BitsToString());
		}

		[Test]
		public void Basic3Offset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<String name=\"Middle\"/>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			string target = "Hello World";
			var data = Bits.Fmt("{0:L8}{1}{2}", offsetdata.Length + 1, offsetdata, target);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(offsetdata.Length + 1, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(offsetdata, (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual(target, (string)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		public void Basic4Offset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<String name=\"Middle\"/>" +
				"		<String name=\"Sized\" length=\"5\"/>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			string sizeddata = "12345";
			string target = "Hello World";
			var data = Bits.Fmt("{0:L8}{1}{2}{3}", sizeddata.Length + offsetdata.Length + 1, offsetdata, sizeddata, target);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(sizeddata.Length + offsetdata.Length + 1, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(offsetdata, (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual(sizeddata, (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(target, (string)dom.dataModels[0][3].DefaultValue);
		}

		[Test]
		public void BadOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"		</Number>" +
				"		<String name=\"Middle\"/>" +
				"		<String name=\"Sized\" length=\"5\"/>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			string sizeddata = "12345";
			string target = "Hello World";
			var data = Bits.Fmt("{0:L8}{1}{2}{3}", 3, offsetdata, sizeddata, target);

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("String 'TheDataModel.Data' failed to crack. Offset is 16 bits but must be at least 40 bits.", ex.Message);
		}


		[Test]
		public void OffsetInSizedBlock()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>

		<Number name='offset' size='8'>
			<Relation type='offset' of='Data'/>
		</Number>

		<Block name='block'>
			<Number name='size' size='8'>
				<Relation type='size' of='block'/>
			</Number>

			<String name='Data'/>
		</Block>

	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAA";
			string payload = "Hello World";
			var data = Bits.Fmt("{0:L8}{1:L8}{2}{3}", 1 + 1 + offsetdata.Length, 1 + offsetdata.Length + payload.Length, offsetdata, payload);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0].find("block.Data").DefaultValue);
		}

		[Test]
		public void BadOffsetInSizedBlock()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>

		<Number name='offset' size='8'>
			<Relation type='offset' of='Data'/>
		</Number>

		<Block name='block'>
			<Number name='size' size='8'>
				<Relation type='size' of='block'/>
			</Number>

			<String name='Data'/>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAA";
			string payload = "Hello World";
			var data = Bits.Fmt("{0:L8}{1:L8}{2}{3}", 30, 1 + offsetdata.Length + payload.Length, offsetdata, payload);

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("String 'TheDataModel.block.Data' failed to crack. Offset is 224 bits but buffer only has 168 bits.", ex.Message);
		}

		[Test]
		public void RelativeOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"5\"/>"+
				"		<Number size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" relative=\"true\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "12345";
			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			var data = Bits.Fmt("{0}{1:L8}{2}{3}", otherdata, offsetdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(offsetdata.Length, (int)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][2].DefaultValue.BitsToString());
		}

		[Test]
		public void RelativeToOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String length=\"5\"/>" +
				"		<Number name=\"Size\" size=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" relative=\"true\" relativeTo=\"RelData\" />" +
				"		</Number>" +
				"		<String length=\"5\"/>" +
				"		<String name=\"RelData\" length=\"5\"/>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "12345";
			string offsetdata = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			var data = Bits.Fmt("{0}{1:L8}{2}{3}{4}{5}", otherdata, offsetdata.Length + otherdata.Length, otherdata, otherdata, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(offsetdata.Length + otherdata.Length, (int)dom.dataModels[0]["Size"].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0]["Data"].DefaultValue);
		}

		[Test]
		public void RelativeOffsetBlock()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block length='5'>
			<Number size='8'>
				<Relation type='offset' of='Data' relative='true' />
			</Number>
		</Block>
		<String name='Data'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2}", offsetdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0]["Data"].DefaultValue);
		}

		[Test]
		public void RelativeOffsetSpace()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Number size='8'>
			<Relation type='offset' of='Data' relative='true' />
		</Number>
		<String length='5'/>
		<String name='Data'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string offsetdata = "AAAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2}", offsetdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0]["Data"].DefaultValue);
		}

		[Test]
		public void NonChildOffset()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='blk'>
			<Number size='8'>
				<Relation type='size' of='blk' />
			</Number>
			<Block>
				<Blob/>
				<Number size='8'>
					<Relation type='offset' of='Data'/>
				</Number>
			</Block>
		</Block>
		<Blob name='Data'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "abcde";
			string offsetdata = "AAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2:L8}{3}{4}", 7, otherdata, 16, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", dom.dataModels[0]["Data"].DefaultValue.BitsToString());
		}

		[Test]
		public void NonChildRelativeOffset()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='blk'>
			<Number size='8'>
				<Relation type='size' of='blk' />
			</Number>
			<Block>
				<Blob/>
				<Number size='8'>
					<Relation type='offset' relative='true' of='Data'/>
				</Number>
			</Block>
		</Block>
		<String name='Data'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "abcde";
			string offsetdata = "AAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2:L8}{3}{4}", 1 + otherdata.Length + 1, otherdata, offsetdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0]["Data"].DefaultValue);
		}

		[Test]
		public void NonChildRelativeToOffset()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='blk'>
			<Number size='8'>
				<Relation type='size' of='blk' />
			</Number>
			<Block name='inner'>
				<String/>
				<String name='off' length='1'/>
				<Number size='8'>
					<Relation type='offset' of='Data' relativeTo='off'/>
				</Number>
			</Block>
		</Block>
		<String name='Data'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			string otherdata = "abcde";
			string offsetdata = "AAAAAAAAA";
			var data = Bits.Fmt("{0:L8}{1}{2:L8}{3}{4}", 1 + otherdata.Length + 1, otherdata, 1 + 1 + offsetdata.Length, offsetdata, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0]["Data"].DefaultValue);
		}

		[Test]
		public void Sample()
		{
			string xml = @"
<Peach>
    <DataModel name='TheDataModel'>
        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset0' />
        </String>
        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset1' />
        </String>
        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset2' />
        </String>
        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset3' />
        </String>
        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset4' />
        </String>

        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset5' />
        </String>

        <String length='4' padCharacter=' '>
                <Relation type='offset' of='Offset6' />
        </String>

        <Block>
                <Block name='Offset0'>
                        <Block>
                                <String name='Offset1' value='CRAZY STRING!' />
                                <String value='aslkjalskdjas' />
                                <String value='aslkdjalskdjasdkjasdlkjasd' />
                        </Block>
                        <String name='Offset2' value='ALSKJDALKSJD' />
                        <Block>
                                <String name='Offset3' value='1' />
                                <String name='Offset4' value='' />
                                <String name='Offset5' value='1293812093' />
                        </Block>
                </Block>
        </Block>

        <String name='Offset6' value='aslkdjalskdjas' />

    </DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var final = dom.dataModels[0].Value;
			string str = Encoding.ASCII.GetString(final.ToArray());
			Assert.NotNull(str);
		}

		[Test]
		public void CrackStringOffset()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String length=\"8\">" +
				"			<Relation type=\"offset\" of=\"Data\" />" +
				"			<Hint name='NumericalString' value='true' />" +
				"		</String>" +
				"		<String name=\"Data\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "00000010  Payload");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(10, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Payload", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void RelativeToDataModel()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String length='4'/>
		<Choice>
		<Block>
			<String length='4'>
				<Relation type='offset' of='Data' relative='true' relativeTo='TheDataModel'/>
			</String>
			<String name='Data' length='4' />
		</Block>
		</Choice>
		<String/>
	</DataModel>

	<DataModel name='DM2' ref='TheDataModel'/>

</Peach>";

			var data = Bits.Fmt("{0}", "abcd0010  dataend");

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual("abcd", (string)dom.dataModels[1][0].DefaultValue);
			Assert.AreEqual("end", (string)dom.dataModels[1][2].DefaultValue);
		}

		[Test]
		public void RelationInChoice()
		{
			string xml = @"
<Peach>
	<DataModel name='Table'>
		<Choice maxOccurs='10'>
			<Block name='BASE'>
				<Blob name='tag' length='4' value='BASE' token='true'/>
				<Number name='Offset' size='32' signed='false' endian='big'>
					<Relation type='offset' of='baseTableData' relative='true'/>
				</Number>
				<Number name='length' size='32' signed='false' endian='big'>
					<Relation type='size' of='baseTableData'/>
				</Number>
				<Block name='baseTableData'>
					<Placement after='OffsetTable'/>
				</Block>
			</Block>
		</Choice>
	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.NotNull(dom);
		}
	}
}

// end
