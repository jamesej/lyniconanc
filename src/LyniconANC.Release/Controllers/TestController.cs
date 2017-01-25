using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Test.Models;

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
    }
}
