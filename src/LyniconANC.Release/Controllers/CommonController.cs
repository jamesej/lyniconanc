using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Release.Models;
using Microsoft.AspNetCore.Authorization;

namespace Lynicon.Test.Controllers
{
    // Only users with editing rights in the CMS can see the route to this controller
    [Authorize(Policy = "CanEditData")]
    public class CommonController : Controller
    {
        public IActionResult Common(CommonContent data)
        {
            return View(data);
        }
    }
}
