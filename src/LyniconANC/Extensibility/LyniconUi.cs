using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using System.Web.Script.Serialization;
using Lynicon.Attributes;
using Lynicon.Collation;
using Lynicon.Extensions;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using LyniconANC.Extensibility;
using Lynicon.Services;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// Indicates the implementing class contains an indication of what roles are required
    /// </summary>
    public interface IRolesRequired
    {
        /// <summary>
        /// Sequence of role letters indicating all roles required
        /// </summary>
        string RequiredRoles { get; set; }
    }

    /// <summary>
    /// Global object model of Lynicon's UI
    /// </summary>
    public class LyniconUi
    {
        static readonly LyniconUi instance = new LyniconUi();
        public static LyniconUi Instance { get { return instance; } }

        static LyniconUi() { }

        /// <summary>
        /// Take a content type and display its name in a user-friendly way
        /// </summary>
        /// <param name="t">The content type</param>
        /// <returns>Friendly display of its name</returns>
        public static string ContentClassDisplayName(Type t)
        {
            if (t == null)
                return "";
            if (t.Name == "Reference`1")
                t = t.GenericTypeArguments[0];
            var attr = t.GetCustomAttribute<ContentTypeDisplayNameAttribute>();
            if (attr != null)
                return attr.DisplayName;
            string name = t.Name.UpToLast("Content").ExpandCamelCase();
            return name.UpTo("`").Trim();
        }

        private List<FuncPanelButton> funcPanelButtons = new List<FuncPanelButton>();
        /// <summary>
        /// List of buttons shown on the Function Panel
        /// </summary>
        public List<FuncPanelButton> FuncPanelButtons
        {
            get
            {
                return funcPanelButtons;
            }
        }

        private List<ItemsListButton> itemsListButtons = new List<ItemsListButton>();
        /// <summary>
        /// List of operation buttons shown on the Filters page
        /// </summary>
        public List<ItemsListButton> ItemsListButtons { get { return itemsListButtons; } }

        /// <summary>
        /// Get the list of operation buttons for the Filters page according to the roles of the current user
        /// </summary>
        /// <returns>List of operation buttons to show</returns>
        public List<ItemsListButton> CurrentItemsListButtons()
        {
            return ItemsListButtons.Where(ilb => HaveRequiredRoles(ilb)).ToList();
        }

        private List<EditorPanel> editorPanels = new List<EditorPanel>();
        /// <summary>
        /// List of custom editor panels to add to the editor if appropriate to the editing context
        /// </summary>
        public List<EditorPanel> EditorPanels
        {
            get
            {
                return editorPanels;
            }
        }

        /// <summary>
        /// Get the list of editor panels to show in the editor according to the roles of the current user
        /// and the content type
        /// </summary>
        /// <param name="contentType">The content type for which to get editor panels</param>
        /// <returns>The editor panels to show</returns>
        public List<EditorPanel> CurrentEditorPanels(Type contentType)
        {
            return editorPanels.Where(ep => HaveRequiredRoles(ep) && ep.ContentTypeSelector(contentType)).ToList();
        }

        public List<string> adminViews = new List<string>();
        /// <summary>
        /// List of custom views to show on the Admin page
        /// </summary>
        public List<string> AdminViews { get { return adminViews; } }

        // RevealPanelViews can contain scripts because they are loaded using jquery .load which will run those scripts on load,
        // however the script manager shouldn't be used
        private ConstraintOrderedCollection<KeyValuePair<string, string>> revealPanelViews = new ConstraintOrderedCollection<KeyValuePair<string, string>>(kvp => kvp.Key);
        /// <summary>
        /// List of custom views to show in the Reveal Panel (which appears above the Function Panel)
        /// </summary>
        public ConstraintOrderedCollection<KeyValuePair<string, string>> RevealPanelViews { get { return revealPanelViews; } }

        private ConstraintOrderedCollection<KeyValuePair<string, string>> editorScripts = new ConstraintOrderedCollection<KeyValuePair<string, string>>(kvp => kvp.Key);
        /// <summary>
        /// List of scripts to include after the markup for the editor.  Either the url of the script, or else the script itself prefixed by 'javascript:'
        /// </summary>
        public ConstraintOrderedCollection<KeyValuePair<string, string>> EditorScripts { get { return editorScripts; } }

        private Dictionary<string, Func<object, object>> variableSetters = new Dictionary<string, Func<object, object>>();
        /// <summary>
        /// List of javascript global variable assignments to run before the markup for the editor.  The key is the name of the variable, the value function when
        /// applied to the content item being edited, returns the value to which to set the variable.
        /// </summary>
        public Dictionary<string, Func<object, object>> VariableSetters { get { return variableSetters;  } }

        private List<ListFilter> filters = new List<ListFilter>();

        /// <summary>
        /// List of filters to show in the Filters page
        /// </summary>
        public List<ListFilter> Filters {
            get
            {
                for (int i = 0; i < filters.Count; i++)
                    filters[i].Idx = i;
                return filters;
            }
        }

        /// <summary>
        /// Whether to show the Admin page button in red to indicate there is a problem with the state of the CMS
        /// </summary>
        public bool ShowProblemAlert { get; set; }

        public LyniconUi()
        {
            ShowProblemAlert = false;
            if (Collator.Instance.RepositoryBuilt)
                BuildFilters(null);
            else
                EventHub.Instance.RegisterEventProcessor("Repository.Built", BuildFilters, "LyniconUI");
        }

        /// <summary>
        /// Called by infrastructure to automatically build filters from attributes on content types
        /// </summary>
        /// <param name="ehd">Event data when called via an event being raised after the repository is built</param>
        /// <returns>Returns null (needed to have signature of an event processor)</returns>
        public object BuildFilters(EventHubData ehd)
        {
            // Get filters on summary types
            foreach (Type t in ContentTypeHierarchy.SummaryBaseTypes.Keys.ToList().Append(typeof(Summary)))
            {
                foreach (PropertyInfo pi in t.GetProperties(BindingFlags.DeclaredOnly |
                        BindingFlags.Public |
                        BindingFlags.Instance)) // just the properties declared in this class, not any base class
                {
                    var lfa = pi.GetCustomAttribute<FieldFilterAttribute>();
                    if (lfa != null)
                    {
                        Filters.Add(FieldFilter.Create(lfa, pi));
                    }
                }
            }

            // Get filters on container types
            foreach (Type t in ContentTypeHierarchy.AllContentTypes.Select(ct => Collator.Instance.ContainerType(ct)).Distinct())
            {
                foreach (PropertyInfo pi in t.GetProperties())
                {
                    var lfa = pi.GetCustomAttribute<FieldFilterAttribute>();
                    if (lfa != null)
                    {
                        Filters.Add(FieldFilter.Create(lfa, pi));
                    }
                } 
            }
            
            // Get filters on container extension types
            foreach (Type extT in LyniconSystem.Instance.Extender.ExtensionTypes())
            {
                Type baseT = extT.BaseType();
                foreach (PropertyInfo pi in extT.GetProperties(BindingFlags.DeclaredOnly |
                        BindingFlags.Public |
                        BindingFlags.Instance))
                {
                    var lfa = pi.GetCustomAttribute<FieldFilterAttribute>();
                    if (lfa != null)
                    {
                        PropertyInfo mappedPi = LyniconSystem.Instance.Extender[baseT]
                            .GetProperty(pi.Name);
                        Filters.Add(FieldFilter.Create(lfa, mappedPi));
                    }
                }
            }

            return null;
        }

        private string CurrentRoles()
        {
            IUser u = SecurityManager.Current.User;
            if (u == null) return "";
            return u.Roles;
        }

        private bool HaveRequiredRoles(IRolesRequired item)
        {
            return item.RequiredRoles.All(rc => CurrentRoles().Contains(rc));
        }

        /// <summary>
        /// Applies standard substitutions to the text for the click url or click script of a Function panel button
        /// These include $$CurrentUrl$$, $$BaseUrl$$, $$OriginalQuery$$, $$Path$$ and $$Type$$
        /// </summary>
        /// <param name="s">String in with to make substitutions</param>
        /// <param name="viewContext">The ViewContext of the view in which the substitutions are being made</param>
        /// <param name="viewBag">The ViewBag of the view in which the substitutions are being made</param>
        /// <returns></returns>
        public string ApplySubstitutions(string s, ViewContext viewContext, dynamic viewBag)
        {
            string subs = (s ?? "")
             .Replace("$$CurrentUrl$$", viewContext.HttpContext.Request.GetEncodedUrl())
             .Replace("$$BaseUrl$$", viewBag._Lyn_BaseUrl)
             .Replace("$$OriginalQuery$$", viewBag.OriginalQuery);

            if (viewContext.ViewData.Model != null)
            {
                var type = viewContext.ViewData.Model.GetType().UnextendedType();
                if (ContentTypeHierarchy.AllContentTypes.Contains(type))
                {
                    var address = new Address(type, viewContext.RouteData);
                    subs = subs
                        .Replace("$$Path$$", address.GetAsContentPath())
                        .Replace("$$Type$$", address.Type.FullName);
                }
            }

            return subs;
        }

        /// <summary>
        /// Get the script to run to set the variables in VariableSetters
        /// </summary>
        /// <param name="Model">The model i.e. the content item being editted</param>
        /// <returns>The script as an MvcHtmlString (so no HTML encoding is done on it)</returns>
        public HtmlString GetVariableSetScript(object Model)
        {
            var s = new StringBuilder();
            foreach (var kvp in this.VariableSetters)
            {
                s.AppendLine(kvp.Key + "=" + JsonConvert.SerializeObject(kvp.Value(Model)) + ";");
            }

            return new HtmlString(s.ToString());
        }
    }
}
