using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Attributes;

namespace LyniconANC.Test.Models
{
    public class PathAddressData
    {
        [AddressComponent(UsePath = true)]
        public string P { get; set; }

        public string X { get; set; }
    }
}
