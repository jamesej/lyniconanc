using Lynicon.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensions
{
    public class MetadataAwareMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context?.PropertyAttributes == null || context.PropertyAttributes.Count == 0)
                return;

            foreach (var ima in context.PropertyAttributes.OfType<IMetadataAware>())
            {
                ima.OnMetadataCreated(context);
            }
        }
    }
}
