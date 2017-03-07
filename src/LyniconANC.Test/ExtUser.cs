using Lynicon.Membership;
using LyniconANC.Test.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Test
{
    public interface IExtTestData
    {
        string ExtData { get; set; }
    }

    public class ExtTestData : TestData, IExtTestData
    {
        public string ExtData { get; set; }
    }
}
