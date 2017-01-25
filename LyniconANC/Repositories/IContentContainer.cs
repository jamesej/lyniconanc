using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;

namespace Lynicon.Repositories
{
    /// <summary>
    /// Interface for a type which is a content container
    /// </summary>
    public interface IContentContainer
    {
        /// <summary>
        /// Get the content object
        /// </summary>
        /// <returns>The content object</returns>
        object GetContent();
        /// <summary>
        /// Get the summary object
        /// </summary>
        /// <returns>The summary object</returns>
        Summary GetSummary();
        /// <summary>
        /// Set the content
        /// </summary>
        /// <param name="o">The content object</param>
        void SetContent(object o);
        /// <summary>
        /// The content type
        /// </summary>
        Type ContentType { get; }
    }
}
