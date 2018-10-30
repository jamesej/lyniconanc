using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace Lynicon.Controllers
{
    [Area("Lynicon")]
    public class UploadController : Controller
    {
        private IHostingEnvironment hosting;

        public UploadController(IHostingEnvironment hosting)
        {
            this.hosting = hosting;
        }

        /// <summary>
        /// Do a file upload to the FileManager
        /// </summary>
        /// <param name="dir">Folder into which to load the file</param>
        /// <returns>Status of operation</returns>
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Index(string dir, ICollection<IFormFile> files)
        {
            string status = "No file sent";
            
            try
            {
                string uploadBasePath = this.hosting.WebRootFileProvider.GetFileInfo(dir).PhysicalPath;

                if (Request.Query.ContainsKey("qqfile"))
                {
                    string filename = Request.Query["qqfile"][0];
                    var input = Request.Body;
                    using (var fileStream = new FileStream(Path.Combine(uploadBasePath, filename), FileMode.Create))
                    {
                        await input.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            using (var fileStream = new FileStream(Path.Combine(uploadBasePath, file.FileName), FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                        }
                    }
                }
                status = "OK";
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }

            return Content("{ 'success' : \"" + status.Replace("\"", "'") + "\" }");
        }

    }
}
