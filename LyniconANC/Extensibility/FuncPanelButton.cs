using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Data to define a button on the Function Panel
    /// </summary>
    public class FuncPanelButton : UIElement
    {
        /// <summary>
        /// Caption for the button
        /// </summary>
        public string Caption { get; set; }
        /// <summary>
        /// HTML Id attribute for the button
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Client side script to run on click of button
        /// </summary>
        public string ClientClickScript { get; set; }
        /// <summary>
        /// Background colour of button
        /// </summary>
        public string BackgroundColor { get; set; }
        /// <summary>
        /// Url button links to when clicked
        /// </summary>
        public string Url { get; set; }
    }
}
