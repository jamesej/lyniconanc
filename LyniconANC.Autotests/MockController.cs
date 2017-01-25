using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.AutoTests
{
    public class MockController : Controller
    {
        public IActionResult Mock()
        {
            return Content("OK");
        }
    }
}
