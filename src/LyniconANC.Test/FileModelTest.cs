using Lynicon.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LyniconANC.Test
{
    public class FileModelTests
    {
        [Fact]
        public void TestStripLineComments()
        {
            var doc = new List<string>
            {
                "// abc",
                "def",
                "// ghi"
            };
            var fms = new FileModelStripper(doc);
            Assert.Equal("", fms.StrippedLines[0]);
            Assert.Equal("def", fms.StrippedLines[1]);
            Assert.Equal("", fms.StrippedLines[2]);
        }

        [Fact]
        public void TestStripFlowComments()
        {
            var doc = new List<string>
            {
                "/* abc",
                "def */ qqq",
                "ghi"
            };
            var fms = new FileModelStripper(doc);
            Assert.Equal("", fms.StrippedLines[0]);
            Assert.Equal(" qqq", fms.StrippedLines[1]);
            Assert.Equal("ghi", fms.StrippedLines[2]);
        }

        [Fact]
        public void TestStripFlowString()
        {
            var doc = new List<string>
            {
                "var xyz = \"qqq\"",
                "var ddd = @\" qqq",
                "ghi\" xxx"
            };
            var fms = new FileModelStripper(doc);
            Assert.Equal("var xyz = ", fms.StrippedLines[0]);
            Assert.Equal("var ddd = @", fms.StrippedLines[1]);
            Assert.Equal(" xxx", fms.StrippedLines[2]);
        }

        [Fact]
        public void TestStripMixed()
        {
            var doc = new List<string>
            {
                "// abc",
                "// def",
                "var xyz = \"qq\\\"q\"",
                "var ddd = @\" q\"\"qq",
                "ghi\" xxx",
                "/*",
                "* dkdkdk",
                "*/ x",
                "hello"
            };
            var fms = new FileModelStripper(doc);
            Assert.Equal("", fms.StrippedLines[0]);
            Assert.Equal("", fms.StrippedLines[1]);
            Assert.Equal("var xyz = ", fms.StrippedLines[2]);
            Assert.Equal("var ddd = @", fms.StrippedLines[3]);
            Assert.Equal(" xxx", fms.StrippedLines[4]);
            Assert.Equal("", fms.StrippedLines[5]);
            Assert.Equal("", fms.StrippedLines[6]);
            Assert.Equal(" x", fms.StrippedLines[7]);
            Assert.Equal("hello", fms.StrippedLines[8]);
        }

        [Fact]
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
            Assert.Equal(7, fm.LineNum);

            fm.Jump(1);
            string wordFound = fm.FindLineContainsAny(new string[] { "public", "private", "protected" });
            Assert.Equal(18, fm.LineNum);
            Assert.Equal("public", wordFound);

            fm.FindPrevLineIs("}");
            Assert.Equal(16, fm.LineNum);

            fm.ToTop();

            fm.FindLineContains("Startup(");
            fm.FindEndOfMethod();
            Assert.Equal(15, fm.LineNum);

            fm.FindLineContains("Thingy(");
            fm.FindEndOfMethod();
            Assert.Equal(19, fm.LineNum);
        }

        [Fact]
        public void TestModelBracketInsert()
        {
            var doc = new List<string>
            {
                "// hello",
                "{",
                "var x = y.Func1(x =>",
                "\t(isZ ? 0 : 1));",
                "}"
            };

            var fm = new FileModel(doc);

            fm.FindLineContains("Func1(");
            bool succeed = fm.InsertTextAfterMatchingBracket("Func1(", ".qqq()");
            Assert.True(succeed, "Insert Text failed");
            Assert.Equal("\t(isZ ? 0 : 1)).qqq();", doc[3]);

            fm.ToTop();
            fm.FindLineIs("{");
            succeed = fm.InsertTextAfterMatchingBracket("{", "bbb", '{', '}');
            Assert.True(succeed, "Insert text after } failed");
            Assert.Equal("}bbb", doc[4]);
        }
    }
}
