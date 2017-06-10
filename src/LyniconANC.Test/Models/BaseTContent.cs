using System;
using System.Collections.Generic;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Models;

namespace LyniconANC.Test.Models
{
     
    public class BaseTContent : BaseContent
    {
        [Summary]
        public string Title { get; set; }

        public MinHtml SomeStuff { get; set; }
        public List<Link> Links { get; set; }

        public BaseTContent()
        {
            InitialiseProperties();
        }
    }

     
    public class Sub1TContent : BaseTContent
    {
        public int Number { get; set; }
    }

     
    public class Sub2TSummary : Summary
    {
        public Image AnImage { get; set; }
    }

    [SummaryType(typeof(Sub2TSummary))]
    public class Sub2TContent : BaseTContent
    {
        [Summary]
        public Image AnImage { get; set; }

        public Sub2TContent()
        {
            BaseContent.InitialiseProperties(this);
        }
    }
}