
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Membership;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Base class for data describing a UI element in the Lynicon UI
    /// </summary>
    public class UIElement
    {
        /// <summary>
        /// The section of the UI in which the element appears
        /// 'Global' for all locations in the UI, 'Record' just for when a record is being edited
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// The view name to render this element
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// The authorization policy to use when displaying the element (takes priority over
        /// DisplayPermission property)
        /// </summary>
        public string DisplayPolicy { get; set; }

        /// <summary>
        /// The content permission describing when to display the element
        /// </summary>
        public ContentPermission DisplayPermission { get; set; }

        public UIElement()
        {
            DisplayPermission = new ContentPermission();
        }

        public async Task<bool> CanDisplay(IAuthorizationService authService, ClaimsPrincipal user, object model)
        {
            if (!string.IsNullOrEmpty(DisplayPolicy))
            {
                var authResult = await authService.AuthorizeAsync(user, model, DisplayPolicy);
                return authResult.Succeeded;
            }
            else if (DisplayPermission != null)
                return DisplayPermission.Permitted(model);
            else
                return true;
        }
        /// <summary>
        /// Apply macro substitutions to a string based on things in the ViewContext
        /// </summary>
        /// <param name="s">The string with macros</param>
        /// <param name="viewContext">The ViewContext in which the string will be displayed</param>
        /// <returns>The string with macros substituted</returns>
        public string ApplySubstitutions(string s, ViewContext viewContext)
        {
            dynamic viewBag = viewContext.ViewBag;
            string subs = (s ?? "")
             .Replace("$$CurrentUrl$$", viewContext.HttpContext.Request.GetEncodedUrl())
             .Replace("$$BaseUrl$$", viewBag._Lyn_BaseUrl)
             .Replace("$$OriginalQuery$$", viewBag.OriginalQuery);

            if (viewContext.ViewData.Model != null)
            {
                var address = new Address(viewContext.ViewData.Model.GetType().UnextendedType(), viewContext.RouteData);
                subs = subs
                    .Replace("$$Path$$", address.GetAsContentPath())
                    .Replace("$$Type$$", address.Type.FullName);
            }

            return subs;
        }
    }
}
