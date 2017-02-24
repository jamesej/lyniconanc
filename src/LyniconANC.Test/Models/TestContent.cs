using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Lynicon.Models;

namespace LyniconANC.Test.Models
{
     
    public class TestSub
    {
        public List<string> Colln { get; set; }
        public TestSub()
        {
            Colln = new List<string>();
        }
    }

     
    public class TestContent : PageContent
    {
        public string Title { get; set; }
        public TestSub Sub { get; set; }
        public BbText TestText { get; set; }
        public List<string> Strings { get; set; }
        public Image Img { get; set; }
        [UIHint("Html")]
        public string Body { get; set; }

        public string[] Array { get; set; }

        public TestContent()
        {
            Sub = new TestSub();
            TestText = new BbText();
            Strings = new List<string>();
            Img = new Image();
            Array = new string[0];
        }
    }
}