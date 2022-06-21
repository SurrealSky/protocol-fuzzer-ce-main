﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.IO;

#if DISABLED
namespace Peach.Core.Test.Mutators
{
    [TestFixture] 
	[Quick]
	[Peach]
    class SizedVarianceMutatorTests : DataModelCollector
    {
        [Test]
        public void Test1()
        {
            // standard test ... change the length of sizes to count +/- N (default is 50)
            // - Initial string: "AAAAA"
            // - will produce 1 A through 55 A's (the initial value just wraps when expanding and we negate <= 0 sized results)
            // - NOTE: this mutator will update the length of the size relation

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <String name=\"sizeRelation1\">" +
                "           <Relation type=\"size\" of=\"string1\"/>" +
                "       </String>" +
                "       <String name=\"string1\" value=\"AAAAA\"/>" +
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
                "       <Strategy class=\"Sequential\"/>" +
                "   </Test>" +
                "</Peach>";

            PitParser parser = new PitParser();

            Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
            dom.tests[0].includedMutators = new List<string>();
            dom.tests[0].includedMutators.Add("SizedVaranceMutator");

            RunConfiguration config = new RunConfiguration();

            Engine e = new Engine(this);
            e.startFuzzing(dom, config);

            // verify values
            Assert.AreEqual(56, dataModels.Count);
            Assert.AreEqual(Variant.VariantType.Long, dataModels[0][0].InternalValue.GetVariantType());
            Assert.AreEqual(5, (long)dataModels[0][0].InternalValue);
            Assert.AreEqual(Encoding.ASCII.GetBytes("AAAAA"), dataModels[0][1].Value.ToArray());

            for (int i = 1; i < 56; ++i)
            {
                Assert.AreEqual(Variant.VariantType.Long, dataModels[i][0].InternalValue.GetVariantType());
                Assert.AreEqual(i, (long)dataModels[i][0].InternalValue);
                Assert.AreEqual(i, dataModels[i][1].Value.Length);
            }
        }

        [Test]
        public void Test2()
        {
            // standard test ... change the length of sizes to count +/- N (N = 5)
            // - Initial string: "AAAAA"
            // - will produce 1 A through 10 A's (the initial value just wraps when expanding and we negate <= 0 sized results)
            // - NOTE: this mutator will update the length of the size relation

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <String name=\"sizeRelation1\">" +
                "           <Relation type=\"size\" of=\"string1\"/>" +
                "           <Hint name=\"SizedVaranceMutator-N\" value=\"5\"/>" +
                "       </String>" +
                "       <String name=\"string1\" value=\"AAAAA\"/>" +
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
                "       <Strategy class=\"Sequential\"/>" +
                "   </Test>" +
                "</Peach>";

            PitParser parser = new PitParser();

            Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
            dom.tests[0].includedMutators = new List<string>();
            dom.tests[0].includedMutators.Add("SizedVaranceMutator");

            RunConfiguration config = new RunConfiguration();

            Engine e = new Engine(this);
            e.startFuzzing(dom, config);

            // verify values
            Assert.AreEqual(11, dataModels.Count);
            Assert.AreEqual(Variant.VariantType.Long, dataModels[0][0].InternalValue.GetVariantType());
            Assert.AreEqual(5, (long)dataModels[0][0].InternalValue);
            Assert.AreEqual(Encoding.ASCII.GetBytes("AAAAA"), dataModels[0][1].Value.ToArray());

            for (int i = 1; i < 11; ++i)
            {
                Assert.AreEqual(Variant.VariantType.Long, dataModels[i][0].InternalValue.GetVariantType());
                Assert.AreEqual(i, (long)dataModels[i][0].InternalValue);
                Assert.AreEqual(i, dataModels[i][1].Value.Length);
            }
        }

		[Test]
		public void Test3()
		{
			// standard test ... change the length of sizes to count +/- N (N = 5)
			// - Initial string: "AAAAAA"
			// - will produce 1 A through 11 A's (the initial value just wraps when expanding and we negate <= 0 sized results)
			// - NOTE: this mutator will update the length of the size relation

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <String name=\"sizeRelation1\">" +
				"           <Relation type=\"size\" of=\"string1\"/>" +
				"           <Hint name=\"SizedVaranceMutator-N\" value=\"5\"/>" +
				"       </String>" +
				"       <String name=\"string1\" value=\"AAAAAA\"/>" +
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
				"       <Strategy class=\"Sequential\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("SizedVaranceMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			Assert.AreEqual(12, dataModels.Count);
			Assert.AreEqual(Variant.VariantType.Long, dataModels[0][0].InternalValue.GetVariantType());
			Assert.AreEqual(6, (long)dataModels[0][0].InternalValue);
			Assert.AreEqual(Encoding.ASCII.GetBytes("AAAAAA"), dataModels[0][1].Value.ToArray());

			for (int i = 1; i < 12; ++i)
			{
				Assert.AreEqual(Variant.VariantType.Long, dataModels[i][0].InternalValue.GetVariantType());
				Assert.AreEqual(i, (long)dataModels[i][0].InternalValue);
				Assert.AreEqual(i, dataModels[i][1].Value.Length);
			}
		}

		[Test]
		public void TestEmptyValue()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <String name=\"sizeRelation1\">" +
				"           <Relation type=\"size\" of=\"string1\" expressionSet=\"size + 10\"/>" +
				"       </String>" +
				"       <String name=\"string1\" value=\"\"/>" +
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
				"       <Strategy class=\"Sequential\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("SizedVaranceMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			Assert.Greater(dataModels.Count, 1);
			foreach (var item in dataModels)
			{
				Assert.AreEqual(Variant.VariantType.Long, item[0].InternalValue.GetVariantType());
				long len = (long)item[0].InternalValue;
				Assert.GreaterOrEqual(len, 10);
				Assert.AreEqual(len - 10, item[1].Value.Length);
			}
		}

		[Test]
		public void TestBitWiseEmpty()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='Number' size='8' value='0'>
			<Relation type='size' of='Blob' lengthType='bits'/>
		</Number>
		<Blob name='Blob'/>
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
	</Test>
</Peach>
";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("SizedVaranceMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			Assert.AreEqual(51, dataModels.Count);

			for (int i = 0; i < 51; ++i)
			{
				Assert.AreEqual(i, (int)dataModels[i][0].InternalValue);
				Assert.AreEqual(8 + i, dataModels[i].Value.LengthBits);
			}
		}

		[Test]
		public void TestBitWiseNotEmpty()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='Number' size='8' value='0'>
			<Relation type='size' of='Blob' lengthType='bits'/>
		</Number>
		<Blob name='Blob' lengthType='bits' length='7'/>
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
	</Test>
</Peach>
";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("SizedVaranceMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			Assert.AreEqual(1 + 50 + 7, dataModels.Count);

			Assert.AreEqual(7, (int)dataModels[0][0].InternalValue);
			Assert.AreEqual(8 + 7, dataModels[0].Value.LengthBits);

			for (int i = 1; i < 58; ++i)
			{
				Assert.AreEqual(i, (int)dataModels[i][0].InternalValue);
				Assert.AreEqual(8 + i, dataModels[i].Value.LengthBits);
			}
		}

    }
}

// end
#endif
