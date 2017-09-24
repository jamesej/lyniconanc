using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Release.Models;
using Lynicon.Services;

namespace Lynicon.Test.Controllers
{
    public class HomeController : Controller
    {
        LyniconSystem lyn;

        // Lynicon sets up a context within which CMS operations occur called LyniconSystem.
        // This is constructor injected by asp.net core
        public HomeController(LyniconSystem lyn)
        {
            this.lyn = lyn;
        }

        public IActionResult Index(HomeContent data)
        {
            // property injection of the Collator used as the gateway for the data API so that
            // the model can fetch information about other content items
            data.Collator = lyn.Collator;

            return View(data);
        }
    }
}
