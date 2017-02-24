using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Lynicon rebuild for ASP.Net Core of old IMetadataAware interface
    /// </summary>
    public interface IMetadataAware
    {
        void OnMetadataCreated(DisplayMetadataProviderContext metadata);
    }
}
