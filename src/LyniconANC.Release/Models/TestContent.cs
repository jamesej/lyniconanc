using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
// TMP BASE using Lynicon.Base.Models;
using Lynicon.Models;
using Newtonsoft.Json;

namespace Lynicon.Test.Models
{
    [Serializable]
    public class TestSub
    {
        public List<string> Colln { get; set; }
        public TestSub()
        {
            Colln = new List<string>();
        }
    }

    [Serializable]
    public class TestContent : PageContent // TMP BASE PageWithUrlsContent
    {
        public string Title { get; set; }
        public TestSub Sub { get; set; }
        public BbText TestText { get; set; }
        public List<string> Strings { get; set; }
        public Image Img { get; set; }
        [UIHint("MaxHtml")]
        public string Body { get; set; }

        public string[] Array { get; set; }

        [JsonIgnore, ScaffoldColumn(false)]
        public List<HeaderSummary> Hdrs { get; set; }

        public TestContent()
        {
            Sub = new TestSub();
            TestText = new BbText();
            Strings = new List<string>();
            Img = new Image();
            Array = new string[0];
            // TMP BASE this.AlternateUrls = new AlternateUrlList();
        }
    }
}