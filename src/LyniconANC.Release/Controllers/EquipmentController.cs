using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Release.Models;

namespace Lynicon.Test.Controllers
{
    public class EquipmentController : Controller
    {
        public IActionResult Equipment(EquipmentContent data)
        {
            return View(data);
        }
    }
}
