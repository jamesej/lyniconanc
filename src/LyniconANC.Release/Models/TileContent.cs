using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Relations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    // Notice you can have the summary type inherit from the summary type of the parent of the content type
    public class TileSummary : ProductSummary
    {
        public Reference<TileMaterialContent> Material { get; set; }

        public decimal Widthmm { get; set; }

        public decimal Heightmm { get; set; }

        public List<TileColour> AvailableColours { get; set; }
    }

    [SummaryType(typeof(TileSummary))]
    public class TileContent : ProductContent, IHasDefaultAddress
    {
        [AddressComponent("_0")]
        public string UrlSlug { get; set; }

        // This is how you have a reference to another content item. The editor gives you a drop down list of the titles of the
        // content items referred to. When building a view you can access the summary of the referred-to item as contentitem.Material.Summary().
        // You can follow references in reverse i.e. get a list of all content items with references to a specific item. See MaterialsLandingContent.cs.
        [Summary]
        public Reference<TileMaterialContent> Material { get; set; }

        [Summary]
        public List<TileColour> AvailableColours { get; set; }

        [Summary, DisplayBlock("Size"), Display(Name = "Width mm")]
        public decimal Widthmm { get; set; }

        [Summary, DisplayBlock("Size"), Display(Name = "Height mm")]
        public decimal Heightmm { get; set; }

        public Address GetDefaultAddress()
        {
            return new Address(typeof(TileContent), UrlSlug ?? "");
        }
    }
}
