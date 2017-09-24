using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Models;
using Lynicon.Relations;
using Lynicon.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    public class TileMaterialContent : TilesPageBase
    {
        [Summary]
        public string Title { get; set; }

        public MinHtml Description { get; set; }

        [ScaffoldColumn(false), JsonIgnore]
        public LyniconSystem Lyn { private get; set; }

        public TileMaterialContent()
        {
            InitialiseProperties();
        }

        public List<TileSummary> TilesOfMaterial()
        {
            // This is how to get the list of tiles with this material i.e. the
            // TileContent items whose Material reference points to this TileMaterialContent instance.
            return Reference.GetReferencesFrom<TileContent>(Lyn, this, "Material")
                .Cast<TileSummary>()
                .ToList();
        }
    }
}
