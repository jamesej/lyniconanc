using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lynicon.Models;
using Lynicon.Attributes;

namespace LyniconANC.Test.Models
{
    [RedirectPropertySource("Common"),
     RedirectPropertySource("ExternalVal > Value1", ContentType=typeof(TestData), SourceDescriptor="{0}")]
    public class PropertyRedirectContent : BaseContent
    {
        [Summary]

        public string Title { get; set; }

        public string Common { get; set; }

        public MinHtml Stuff { get; set; }

        public string ExternalVal { get; set; }
    }
}