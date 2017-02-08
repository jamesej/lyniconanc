using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Test.Models;
using Lynicon.Utility;
using Microsoft.AspNetCore.Authorization;
using LyniconANC.Release.Models;
using Lynicon.Services;

namespace Lynicon.Test.Controllers
{
    public class TestController : Controller
    {
        LyniconSystem cms;

        public TestController(LyniconSystem cms)
        {
            this.cms = cms;
        }

        public IActionResult Index(TestContent data)
        {
            data.Hdrs = cms.Collator.Get<HeaderSummary>().ToList();
            return View(data);
        }

        public IActionResult Header(HeaderContent data)
        {
            return View(data);
        }

        [Authorize(Policy = "CanEditData")]
        public IActionResult List(List<TestContent> data)
        {
            return Content(data.Select(t => t.Title).Join(", "), "text/plain");
        }

        public IActionResult Data(TestData data)
        {
            return View(data);
        }
    }
}
