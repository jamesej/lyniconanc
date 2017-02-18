using Lynicon.Linq;
using Lynicon.Repositories;
using LyniconANC.Test.Models;
using LyniconANC.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyniconANC.Autotests
{
    //public class ChClass
    //{
    //    public string Change { get; set; }
    //}

    //[TestFixture]
    //public class DbTest
    //{
    //    [Test]
    //    public void TestEF()
    //    {
    //        var db = new PreloadDb("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=LynTest;Integrated Security=True");

    //        var exp1 = db.DbChanges.Select(x => new ChClass { Change = x.Change }).Where(c => c.Change == "x").Where(x => true).Expression;
    //        var exp2 = db.DbChanges.Select(x => new ChClass { Change = x.Change }).Where(c => c.Change == "x").Where(x => true).AsFacade<ChClass>().Expression;

    //        var res = db.DbChanges.Select(x => new ChClass { Change = x.Change }).Where(c => c.Change == "x").Where(x => true).AsFacade<ChClass>().Count();

    //        //var res2 = Setup.LyniconSystemWithDb.Collator.Get<HeaderContent>().ToList();
    //    }
    //}
}
