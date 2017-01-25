using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lynicon.Attributes;
using Lynicon.Models;

namespace Lynicon.Test.Models
{
    [Serializable]
    public class BaseTContent : BaseContent
    {
        [Summary]
        public string Title { get; set; }

        public MinHtml SomeStuff { get; set; }
        public List<Link> Links { get; set; }

        public BaseTContent()
        {
            BaseContent.InitialiseProperties(this);
        }
    }

    [Serializable]
    public class Sub1TContent : BaseTContent
    {
        public int Number { get; set; }
    }

    [Serializable]
    public class Sub2TSummary : Summary
    {
        public Image AnImage { get; set; }
    }

    [Serializable, SummaryType(typeof(Sub2TSummary))]
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