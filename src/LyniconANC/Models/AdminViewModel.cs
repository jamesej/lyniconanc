using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Repositories;

namespace Lynicon.Models
{

    /// <summary>
    /// View model for rending the Admin page
    /// </summary>
    public class AdminViewModel
    {
        /// <summary>
        /// View model for rending a module's information on the Admin page
        /// </summary>
        public class AdminModuleViewModel
        {
            /// <summary>
            /// The title of the module
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// The view name for rending the module's information
            /// </summary>
            public string ViewName { get; set; }

            /// <summary>
            /// Create a new AdminModuleViewModel for a module
            /// </summary>
            /// <param name="m">The module</param>
            public AdminModuleViewModel(Module m)
            {
                Title = m.Name;
                ViewName = m.ManagerView;
            }
        }

        /// <summary>
        /// List of modules which have been blocked
        /// </summary>
        public List<ModuleAdminViewModel> BlockedModules { get; set; }
        /// <summary>
        /// List of modules which are running
        /// </summary>
        public List<ModuleAdminViewModel> RunningModules { get; set; }
        /// <summary>
        /// List of change problems in the current state of the CMS
        /// </summary>
        public List<ChangeProblem> ChangeProblems { get; set; }

        /// <summary>
        /// Build the current admin view model
        /// </summary>
        public AdminViewModel()
        {
            BlockedModules = LyniconModuleManager.Instance.Modules.Values
                .Except(LyniconModuleManager.Instance.ModuleSequence.Where(m => !m.Blocked))
                .OrderBy(m => m.Name)
                .Select(m => m.GetViewModel())
                .ToList();
            RunningModules = LyniconModuleManager.Instance.ModuleSequence
                .Where(m => !m.Blocked)
                .Select(m => m.GetViewModel())
                .ToList();
            ChangeProblems = ContentRepository.ChangeProblems;
        }
    }
}
