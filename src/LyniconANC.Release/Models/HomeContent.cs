using Lynicon.Collation;
using Lynicon.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    public class HomeContent : TilesPageBase
    {
        public MinHtml Intro { get; set; }

        [ScaffoldColumn(false), JsonIgnore]
        public Collator Collator { set; private get; }

        public HomeContent()
        {
            // Sets all object properties to the result of the default constructor for their types
            InitialiseProperties();
        }

        // Get the discounted products
        public List<ProductSummary> Discounted()
        {
            // Get ProductSummary type objects as these are designed to be used as references to other pages.
            // The first type argument to Get is the type returned, the second is the type in which you
            // define the filter/query
            // Rather than providing an IQueryable as a source, the content data API takes an argument which
            // is a function on an IQueryable returning another IQueryable of the same type.
            // Note that you can use parent types here and this will return both TileSummary and EquipmentSummary
            // typed objects, running the query across every type which inherits from ProductSummary. This is very
            // useful for information architecture of any complexity.
            var discounted = Collator.Get<ProductSummary, ProductSummary>(iq => iq.Where(ps => ps.Discount > 0m));

            return discounted.ToList();
        }
    }
}
