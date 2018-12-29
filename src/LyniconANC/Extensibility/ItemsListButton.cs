using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Action button for operating on content items from the items list
    /// </summary>
    public class ItemsListButton : IRolesRequired
    {
        #region IRolesRequired Members

        public string RequiredRoles { get; set; }

        #endregion

        /// <summary>
        /// Button caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Button posts a request to this url with data indicating all checked content items (see script in ItemLister.cshtml)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Confirm message: if not empty, show this message before sending request
        /// </summary>
        public string ConfirmMessage { get; set; }

        /// <summary>
        /// Optional dropdown list for user data
        /// </summary>
        public List<SelectListItem> DropDown { get; set; }

        /// <summary>
        /// Optional placeholder item for dropdown
        /// </summary>
        public string DropDownPlaceholder { get; set; }

        /// <summary>
        /// Version mask to determine with what versions the button is visible (null for all)
        /// </summary>
        public Func<ItemVersion> VisibilityVersionMask { get; set; }
    }
}
