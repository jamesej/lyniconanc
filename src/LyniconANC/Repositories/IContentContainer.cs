using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Models;
using Lynicon.Extensibility;
using Lynicon.Services;

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
        /// <param name="extender">The TypeExtender which knows the correct extension if any for the content object</param>
        /// <returns>The content object</returns>
        object GetContent(TypeExtender extender);
        /// <summary>
        /// Get the summary object
        /// </summary>
        /// <returns>The summary object</returns>
        Summary GetSummary(LyniconSystem sys);
        /// <summary>
        /// Set the content
        /// </summary>
        /// <param name="o">The content object</param>
        void SetContent(LyniconSystem sys, object o);
        /// <summary>
        /// The content type
        /// </summary>
        Type ContentType { get; }
    }
}
