﻿using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
    class Crc32DualFixupTests : DataModelCollector
    {
        [Test]
        public void Test1()
        {
            // standard test

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <Number name=\"CRC\" size=\"32\" signed=\"false\" endian=\"little\">" +
                "           <Fixup class=\"Crc32DualFixup\">" +
                "               <Param name=\"ref1\" value=\"Data1\"/>" +
                "               <Param name=\"ref2\" value=\"Data2\"/>" +
                "           </Fixup>" +
                "       </Number>" +
                "       <Blob name=\"Data1\" value=\"12345\"/>" +
                "       <Blob name=\"Data2\" value=\"6789\"/>" +
                "   </DataModel>" +

                "   <StateModel name=\"TheState\" initialState=\"Initial\">" +
                "       <State name=\"Initial\">" +
                "           <Action type=\"output\">" +
                "               <DataModel ref=\"TheDataModel\"/>" +
                "           </Action>" +
                "       </State>" +
                "   </StateModel>" +

                "   <Test name=\"Default\">" +
                "       <StateModel ref=\"TheState\"/>" +
                "       <Publisher class=\"Null\"/>" +
                "   </Test>" +
                "</Peach>";

            PitParser parser = new PitParser();

            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            RunConfiguration config = new RunConfiguration();
            config.singleIteration = true;

            Engine e = new Engine(this);
            e.startFuzzing(dom, config);

            // verify values
            // -- this is the pre-calculated checksum from Peach2.3 on the blobs: { 1, 2, 3, 4, 5 } and { 6, 7, 8, 9 }
            byte[] precalcChecksum = new byte[] { 0x26, 0x39, 0xF4, 0xCB };
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(precalcChecksum, values[0].ToArray());
        }

		[Test]
		public void TestCrack()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='8' signed='false' name='Length'>
			<Relation type='size' of='DataBlock'/>
		</Number>
		<Block name='DataBlock'>
			<Blob name='Data'/>
			<Number size='32' name='CRC' endian='big' signed='false'>
				<Fixup class='Crc32DualFixup'>
					<Param name='ref1' value='Length'/>
					<Param name='ref2' value='Data'/>
				</Fixup>
			</Number >
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var actual = dom.dataModels[0].Value.ToArray();
			byte[] expected = new byte[] { 0x05, 0x11, 0x56, 0x1e, 0xc6, 0x48 };
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRoundTrip()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Header'>
			<Number size='32' signed='false' endian='big'>
				<Fixup class='Crc32DualFixup'>
					<Param name='ref1' value='Header' />
					<Param name='ref2' value='Payload' />
				</Fixup>
			</Number>
		</Block>
		<Blob name='Payload' value='Hello' />
	</DataModel>
</Peach>
";

			VerifyRoundTrip(xml);
		}
	}
}

// end
