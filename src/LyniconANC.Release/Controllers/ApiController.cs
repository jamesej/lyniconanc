using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LyniconANC.Release.Models;

namespace LyniconANC.Release.Controllers
{
    public class ApiController : Controller
    {
        public IActionResult Tiles(List<TileContent> data)
        {
            return Ok(data);
        }
    }
}