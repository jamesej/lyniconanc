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
    public class CmdTests
    {
        [Test]
        public void TestInitializeLyniconProject()
        {
            var fm = new FileModel(TestStartupFiles.Basic);

            var cmd = new InitializeLyniconProjectCommand();
            cmd.Send = msg => { };
            bool succeed = cmd.UpdateStartup(fm);
            Assert.IsTrue(succeed, "var1 not updated successfully");

            var fm2 = new FileModel(fm.GetLines());
            succeed = cmd.UpdateStartup(fm);
            Assert.IsTrue(succeed, "repeat not updated successfully");
            Assert.IsTrue(fm2.GetLines().SequenceEqual(fm.GetLines()), "repeat changes updated versn");

            var fm3 = new FileModel(TestStartupFiles.Extended1);

            succeed = cmd.UpdateStartup(fm3);
            Assert.IsTrue(succeed, "ext not updated successfully");
        }

        [Test]
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
            Assert.IsTrue(succeed, "Insert Text failed");
            Assert.AreEqual("\t(isZ ? 0 : 1)).qqq();", doc[3]);
        }
    }
}
