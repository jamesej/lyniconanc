using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lynicon.Models;

namespace Lynicon.Test.Models
{
    [Serializable]
    public class GeneralSummary : Summary
    {
        public Image Image { get; set; }

        public GeneralSummary()
        {
            Image = new Image();
        }
    }
}