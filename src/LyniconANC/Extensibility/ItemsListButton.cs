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
    }
}
