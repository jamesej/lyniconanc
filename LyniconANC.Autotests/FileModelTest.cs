using LyniconANC.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Autotests
{
    [TestFixture]
    public class FileModelTests
    {
        [Test]
        public void TestStripLineComments()
        {
            var doc = new List<string>
            {
                "// abc",
                "def",
                "// ghi"
            };
            var fms = new FileModelStripper(doc);
            Assert.AreEqual("", fms.StrippedLines[0]);
            Assert.AreEqual("def", fms.StrippedLines[1]);
            Assert.AreEqual("", fms.StrippedLines[2]);
        }

        [Test]
        public void TestStripFlowComments()
        {
            var doc = new List<string>
            {
                "/* abc",
                "def */ qqq",
                "ghi"
            };
            var fms = new FileModelStripper(doc);
            Assert.AreEqual("", fms.StrippedLines[0]);
            Assert.AreEqual(" qqq", fms.StrippedLines[1]);
            Assert.AreEqual("ghi", fms.StrippedLines[2]);
        }

        [Test]
        public void TestStripFlowString()
        {
            var doc = new List<string>
            {
                "var xyz = \"qqq\"",
                "var ddd = @\" qqq",
                "ghi\" xxx"
            };
            var fms = new FileModelStripper(doc);
            Assert.AreEqual("var xyz = ", fms.StrippedLines[0]);
            Assert.AreEqual("var ddd = @", fms.StrippedLines[1]);
            Assert.AreEqual(" xxx", fms.StrippedLines[2]);
        }

        [Test]
        public void TestStripMixed()
        {
            var doc = new List<string>
            {
                "// abc",
                "// def",
                "var xyz = \"qqq\"",
                "var ddd = @\" qqq",
                "ghi\" xxx",
                "/*",
                "* dkdkdk",
                "*/ x",
                "hello"
            };
            var fms = new FileModelStripper(doc);
            Assert.AreEqual("", fms.StrippedLines[0]);
            Assert.AreEqual("", fms.StrippedLines[1]);
            Assert.AreEqual("var xyz = ", fms.StrippedLines[2]);
            Assert.AreEqual("var ddd = @", fms.StrippedLines[3]);
            Assert.AreEqual(" xxx", fms.StrippedLines[4]);
            Assert.AreEqual("", fms.StrippedLines[5]);
            Assert.AreEqual("", fms.StrippedLines[6]);
            Assert.AreEqual(" x", fms.StrippedLines[7]);
            Assert.AreEqual("hello", fms.StrippedLines[8]);
        }

        [Test]
        public void TestModelOps()
        {
            var doc = new List<string>
            {
                "using abc.def;",
                "",
                "namespace abc.def",
                "{",
                "// hello",
                "\tpublic class Startup", // POS 5
                "\t{",
                "\t\tpublic Startup(Thing x)", // POS 7
                "\t\t{",
                "\t\t\tblah blah",
                "\t\t\tvar x = \"321\";",
                "\t\t\t",
                "\t\t\tif (check)",
                "\t\t\t{",
                "\t\t\t\blah",
                "\t\t\t}",
                "\t\t}",
                "",
                "\t\ttpublic void Thingy()", // POS 18
                "\t\t{",
                "\t\t}",
                "\t}",
                "}"
            };

            var fm = new FileModel(doc);

            bool found = fm.FindLineContains("Startup(");
            Assert.AreEqual(7, fm.LineNum);

            fm.Jump(1);
            string wordFound = fm.FindLineContainsAny(new string[] { "public", "private", "protected" });
            Assert.AreEqual(18, fm.LineNum);
            Assert.AreEqual("public", wordFound);

            fm.FindPrevLineIs("}");
            Assert.AreEqual(16, fm.LineNum);

            fm.ToTop();

            fm.FindLineContains("Startup(");
            fm.FindEndOfMethod();
            Assert.AreEqual(15, fm.LineNum);

            fm.FindLineContains("Thingy(");
            fm.FindEndOfMethod();
            Assert.AreEqual(19, fm.LineNum);
        }
    }
}
