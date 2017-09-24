using Lynicon.Attributes;
using Lynicon.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    // All summaries inherit directly or indirectly from Summary
    public class ProductSummary : Summary
    {
        public Image Thumbnail { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string PriceUnit { get; set; }
        public string Caption { get; set; }
    }

    // Link the content type to the summary type with this attribute
    [SummaryType(typeof(ProductSummary))]
    public class ProductContent : TilesPageBase
    {
        [Summary]
        public string Title { get; set; }

        [Summary]
        public Image Thumbnail { get; set; }
        
        public Image Image { get; set; }

        // DisplaBlock attribute is used to group related properties in the editor in a collapsible block
        [Summary, DisplayBlock("Pricing")]
        public decimal Price { get; set; }

        [Summary, DisplayBlock("Pricing"), Display(Name = "Price Unit")]
        public string PriceUnit { get; set; }

        [Summary, DisplayBlock("Pricing")]
        public decimal Discount { get; set; }

        [Summary]
        public string Caption { get; set; }

        public MinHtml Description { get; set; }

        public ProductContent()
        {
            InitialiseProperties();
        }
    }
}
