using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Test.Models;
using Lynicon.Utility;
using Microsoft.AspNetCore.Authorization;

namespace Lynicon.Test.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index(TestContent data)
        {
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
    }
}
