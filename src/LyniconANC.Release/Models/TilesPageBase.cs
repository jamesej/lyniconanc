using Lynicon.Attributes;
using Lynicon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    // Gets/Saves the property Shared from the single instance of the content item of type CommonContent
    [RedirectPropertySource("Shared", ContentType = typeof(CommonContent))]
    public class TilesPageBase : PageContent
    {
        // Automatically fetched from the single common content item of type CommonContent
        // If you use the content editor to update this property in any content item whose type inherits from this, it will update
        // the single common content item.
        public SharedContent Shared { get; set; }

        public TilesPageBase()
        {
            InitialiseProperties();
        }
    }
}
