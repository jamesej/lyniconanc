using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Models;

namespace LyniconANC.Test.Models
{
     
    public class GeneralSummary : Summary
    {
        public Image Image { get; set; }

        public GeneralSummary()
        {
            Image = new Image();
        }
    }
}