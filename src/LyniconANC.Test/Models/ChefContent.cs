using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace LyniconANC.Test.Models
{
     
    public class ChefSummary : Summary
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Image MainImage { get; set; }
        [UIHint("Multiline")]
        public string Intro { get; set; }

        public ChefSummary()
        {
            BaseContent.InitialiseProperties(this);
        }
    }

     
    public class TwitterDetails
    {
        public string TwitterTitle { get; set; }
        public string TwitterHandle { get; set; }
        public string TwitterWeblink { get; set; }
        public List<string> ListString { get; set; }

        public TwitterDetails()
        {
            ListString = new List<string>();
        }
    }

    [SummaryType(typeof(ChefSummary))]
    public class ChefContent : PageContent
    {
        public ChefSummary Summary { get; set; }
        [UIHint("Html")]
        public string Biography { get; set; }
        [UIHint("Html")]
        public string Awards { get; set; }
        [UIHint("MinHtml")]
        public string Interviews { get; set; }

        public List<string> ListString { get; set; }
        public List<TwitterDetails> ListDetails { get; set; }

        public TwitterDetails Twitter { get; set; }

        public ChefContent()
        {
            BaseContent.InitialiseProperties(this);
        }
    }
}