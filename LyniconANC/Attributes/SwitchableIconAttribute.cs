using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Lynicon.Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Lynicon.Attributes
{
    /// <summary>
    /// When attached to the properties of a subclass of Switchable, indicates an icon that should
    /// be used to display the property in the UI as an option which can be switched to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SwitchableIconAttribute : Attribute, IMetadataAware
    {
        public const string Markup = "_SwitchableIconMarkup";
        /// <summary>
        /// The file name of the icon image which must be a png found in /Content/Icons
        /// </summary>
        public string IconName { get; private set; }
        /// <summary>
        /// XOffset of top left corner of area of icon image to display when using a sprite
        /// </summary>
        public int XOffset { get; private set; }
        /// <summary>
        /// YOffset of top left corner of area of icon image to display when using a sprite
        /// </summary>
        public int YOffset { get; private set; }
        /// <summary>
        /// Text to show when user hovers over icon
        /// </summary>
        public string HoverText { get; private set; }

        /// <summary>
        /// Create a SwitchableIconAttribute giving the icon file name
        /// </summary>
        /// <param name="iconName">Icon image file name, a png in /Content/Icons</param>
        public SwitchableIconAttribute(string iconName)
        {
            IconName = iconName;
        }
        /// <summary>
        /// Create a SwitchableIconAttribute giving the icon file name and hover text
        /// </summary>
        /// <param name="iconName">Icon image file name, a png in /Content/Icons</param>
        /// <param name="hoverText">Text to show when user hovers over icon</param>
        public SwitchableIconAttribute(string iconName, string hoverText)
            : this(iconName)
        {
            HoverText = hoverText;
        }
        /// <summary>
        /// Create a SwitchableIconAttribute giving the icon sprint file name and sprite position
        /// </summary>
        /// <param name="spriteFileName">Icon image sprite filename, a png in /Content/Icons</param>
        /// <param name="xOffset">x offset of top left of sprite area for icon</param>
        /// <param name="yOffset">y offset of top left of sprite area for icon</param>
        public SwitchableIconAttribute(string spriteFileName, int xOffset, int yOffset)
        {
            IconName = spriteFileName;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
        }
        /// <summary>
        /// Create a SwitchableIconAttribute giving the icon sprint file name, sprite position and hover text
        /// </summary>
        /// <param name="spriteFileName">Icon image sprite filename, a png in /Content/Icons</param>
        /// <param name="xOffset">x offset of top left of sprite area for icon</param>
        /// <param name="yOffset">y offset of top left of sprite area for icon</param>
        /// <param name="hoverText">Text to show when user hovers over icon</param>
        public SwitchableIconAttribute(string spriteFileName, int xOffset, int yOffset, string hoverText)
            : this(spriteFileName, xOffset, yOffset)
        {
            HoverText = hoverText;
        }

        #region IMetadataAware Members

        public void OnMetadataCreated(DisplayMetadataProviderContext context)
        {
            var metadata = context.DisplayMetadata;
            metadata.AdditionalValues.Add(Markup, string.Format("<div class='switchable-icon' style='background-image: url(\"/Content/Icons/{0}.png\"); background-position: left -{1}px top -{2}px;' title=\"{3}\" data-property-name=\"{4}\">&nbsp</div>",
                IconName, XOffset, YOffset,
                string.IsNullOrEmpty(HoverText) ? metadata.DisplayName().ExpandCamelCase() : HoverText,
                metadata.DisplayName()));
        }

        #endregion
    }
}
