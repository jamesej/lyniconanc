using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Utility;
using Lynicon.Routing;
using Lynicon.Map;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Membership;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Lynicon.Services;

namespace Lynicon.Editors
{
    /// <summary>
    /// Controller for showing the Dual Frame editor, the standard content editor which shows the resulting page in an iframe
    /// </summary>
    [Area("Lynicon")]
    public class ApiController : EditorController
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ApiController));

        /// <summary>
        /// Get data in standard format
        /// </summary>
        /// <param name="data">content data</param>
        /// <returns></returns>
        [HttpGet, ActionName("Index")]
        public IActionResult IndexGet(object data)
        {
            return Ok(data);
        }

        [HttpPost, ActionName("Index")]
        public async Task<IActionResult> IndexPost([FromBody]object data)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var redirectUrl = base.SaveWithRedirect(data);

                    if (ModelState.IsValid)
                    {
                        // set UpdatedCheck if its an auditable item
                        var container = Collator.Instance.GetContainer(data);

                        if (redirectUrl != null)
                            return Redirect(redirectUrl);

                        // post, redirect, get pattern to avoid resubmits
                        return Redirect(UriHelper.GetEncodedUrl(Request));
                    }
                }
                else
                {
                    var errs = ModelState.SelectMany(kvp => kvp.Value.Errors.Select(e => new { kvp.Key, err = e })).ToList();
                    return BadRequest("Form errors: " + errs.Select(e => e.Key + ": " + (string.IsNullOrEmpty(e.err.ErrorMessage) ? e.err.Exception.Message : e.err.ErrorMessage)).Join("; "));
                }
            }
            catch (Exception ex)
            {
                log.Error("Save edit failed", ex);
                throw ex;
            }

            return Ok(data);
        }

        public IActionResult ErrorUnauthorized()
        {
            return Unauthorized();
        }

        public IActionResult ErrorNotFound()
        {
            return NotFound();
        }
    }
}
