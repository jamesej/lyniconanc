using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Lynicon.Attributes;
// TMP BASE
//using Lynicon.Base.Attributes;
//using Lynicon.Base.Models;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace Lynicon.Test.Models
{
    [Serializable]
    public class SubTest
    {
        public string A {get; set;}

        //[Index(IndexAttribute.Mode.TextualAndAgglomerated)]
        public string B {get; set;}
    }

    [Serializable]
    public class HeaderSummary : Summary
    {
        public Image Image { get; set; }
        public int SubTestsCount { get; set; }

        public HeaderSummary()
        {
            Image = new Image();
        }
    }

    [Serializable, RedirectPropertySource("Common"), SummaryType(typeof(HeaderSummary))]
    public class HeaderContent : PageContent
    {
        [Summary]
        public string Title { get; set; }

        [Summary]
        public Image Image { get; set; }

        [UIHint("MinHtml")]
        public string HeaderBody { get; set; }

        public string Common { get; set; }

        [Summary]
        public int SubTestsCount
        {
            get { return SubTests == null ? 0 : SubTests.Count; }
            set { }
        }
        public List<SubTest> SubTests { get; set; }

        private List<GeneralSummary> childItems = null;
        [JsonIgnore, ScaffoldColumn(false)]
        public List<GeneralSummary> ChildItems
        {
            get
            {
                if (childItems == null)
                    childItems = GetPathChildren<GeneralSummary>().ToList();
                return childItems;
            }
        }

        public HeaderContent()
        {
            // TMP BASE this.AlternateUrls = new AlternateUrlList();
            this.SubTests = new List<SubTest>();
            this.Image = new Image();
        }
    }
}