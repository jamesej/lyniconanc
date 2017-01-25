using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Relations;
using Lynicon.Utility;
using Newtonsoft.Json;

namespace LyniconANC.Test.Models
{
    [Serializable]
    public class Block : Switchable
    {
        public Image Image { get; set; }

        [UIHint("MinHtml")]
        public string Text { get; set; }

        public Block()
        {
            Image = new Image();
        }
    }

    [Serializable]
    public class RestaurantSummary : Summary
    {
        public Image MainImage { get; set; }
        [UIHint("Multiline")]
        public string Intro { get; set; }
        public string Description { get; set; }

        public RestaurantSummary()
        {
            BaseContent.InitialiseProperties(this);
        }
    }

    [Serializable, SummaryType(typeof(RestaurantSummary))]
    public class RestaurantContent : PageContent
    {
        [Summary]
        public Image MainImage { get; set; }
        [Summary]
        public string Intro { get; set; }
        [Summary]
        public string Title { get; set; }

        [Summary, UIHint("MaxHtml")]
        public string Description { get; set; }
        [UIHint("ReferenceSelect")]
        public Reference<ChefContent> Chef { get; set; }

        [UIHint("ReferenceServer")]
        public Reference<HeaderContent> TestHeader { get; set; }

        public MinHtml SomeInfo { get; set; }

        public List<Block> Body { get; set; }

        public List<Reference> Tags { get; set; }

        private ChefContent chefFull = null;

        [JsonIgnore, ScaffoldColumn(false)]
        public string ChefBiography
        {
            get
            {
                if (chefFull == null && Chef.ItemId != null)
                {
                    chefFull = Collator.Instance.Get<ChefContent>(Chef.ItemId);
                    return chefFull.Biography;
                }
                return "";
            }
        }

        public RestaurantContent()
        {
            BaseContent.InitialiseProperties(this);
        }
    }
}