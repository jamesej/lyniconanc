using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Relations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    public class MaterialsLandingContent : TilesPageBase
    {
        public MinHtml Intro { get; set; }

        [ScaffoldColumn(false), JsonIgnore]
        public Collator Collator { private get; set; }

        // Get all the tile materials
        public List<Summary> TileMaterials()
        {
            // Notice this is a request for TileMaterialContent content type, but you can
            // specify that the collator returns Summary typed objects.
            return Collator.Get<Summary, TileMaterialContent>().ToList();
        }
    }
}
