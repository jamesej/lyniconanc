using Lynicon.Collation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Indicates that the content item has a method to generate a default address to be
    /// used by the Data API
    /// </summary>
    public interface IHasDefaultAddress
    {
        Address GetDefaultAddress();
    }
}
