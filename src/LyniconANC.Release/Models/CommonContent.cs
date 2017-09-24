using Lynicon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    // subtype used within CommonContent
    public class SharedContent
    {
        public Image Logo { get; set; }
    }

    // Definition of a content class for information shared between all pages of the site
    public class CommonContent : BaseContent
    {
        public SharedContent Shared { get; set; }

        public CommonContent()
        {
            InitialiseProperties();
        }
    }
}
