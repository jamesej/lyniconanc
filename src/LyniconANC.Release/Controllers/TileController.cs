using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Release.Models;
using Lynicon.Services;

namespace Lynicon.Test.Controllers
{
    public class TileController : Controller
    {
        LyniconSystem lyn;

        // Lynicon sets up a context within which CMS operations occur called LyniconSystem.
        // This is constructor injected by asp.net core
        public TileController(LyniconSystem lyn)
        {
            this.lyn = lyn;
        }

        public IActionResult Tile(TileContent data)
        {
            return View(data);
        }

        public IActionResult List(List<TileContent> data)
        {
            return null;
        }

        public IActionResult TileMaterial(TileMaterialContent data)
        {
            // property injection of the full LyniconSystem so that
            // the model can fetch information about other content items
            data.Lyn = lyn;

            return View(data);
        }

        public IActionResult MaterialsLanding(MaterialsLandingContent data)
        {
            // property injection of the Collator used as the gateway for the data API so that
            // the model can fetch information about other content items
            data.Collator = lyn.Collator;

            return View(data);
        }
    }
}
