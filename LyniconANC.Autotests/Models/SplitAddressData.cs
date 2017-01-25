using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;

namespace LyniconANC.Autotests.Models
{
    internal class SplitAddressData
    {
        [AddressComponent("_0")]
        public string A { get; set; }

        [AddressComponent("_1", ConversionFormat = "000")]
        public int B { get; set; }

        public string C { get; set; }
    }
}
