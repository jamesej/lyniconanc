using Lynicon.Extensibility;
using Lynicon.Membership;
using Lynicon.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.ViewComponents
{
    public class ListFiltersViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            var u = SecurityManager.Current.User;
            var v = VersionManager.Instance.CurrentVersion;
            ViewData["VersionSelector"] = VersionManager.Instance.SelectionViewModel(u, v);
            return Task.FromResult<IViewComponentResult>(View(new ItemListerViewModel()));
        }
    }
}
