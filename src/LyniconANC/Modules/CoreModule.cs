using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Lynicon.Routing;
using Lynicon.Membership;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Editors;
using Lynicon.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Lynicon.Extensions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Lynicon.Services;

namespace Lynicon.Modules
{
    /// <summary>
    /// The core module activates and manages functionality for Lynicon to operate at all.  If not registered, no Lynicon
    /// features will be active, just utility code will be available
    /// </summary>
    public class CoreModule : Lynicon.Extensibility.Module
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(CoreModule));

        public CoreModule([FromServices]LyniconSystem sys, params string[] dependentOn)
            : base(sys, "Core", dependentOn)
        {
        }

        public override void MapRoutes(IRouteBuilder builder)
        {
            builder.MapRoute("lyniconembedded",
                "Lynicon/Embedded/{*innerUrl}",
                new { controller = "Embedded", action = "Index", Area = "Lynicon" });
            // Get dynamically generated content
            builder.MapRoute("lynicondynamic",
                "Lynicon/Dynamic/{action}/{urlTail}",
                new { controller = "Dynamic", Area = "Lynicon" });
            builder.MapDataRoute<List<User>>("lyniconusers",
                "Lynicon/Users",
                new { controller = "User", action = "List", Area = "Lynicon" },
                new { },
                new { listView = "UserList", top = "15" });
            builder.MapRoute("lyniconadmin",
                "Lynicon/{controller:regex(ajax|admin|filemanager|items|login|nav|ui|upload|urlmanager|version)}/{action}",
                new { controller = "Ajax", action = "Index", Area = "Lynicon" }
            );
        }

        public override bool Initialise()
        {
            log.Info("Starting Core Module");

            // Set up Url Management
            UrlRequestInterceptor.Register();

            // Set up base UI
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbMainLogout",
                Caption = "Log Out",
                DisplayPermission = new ContentPermission("E"),
                Url = "/Lynicon/Login/Logout?returnUrl=$$CurrentUrl$$",
                Section = "Global"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbMainLogin",
                Caption = "Log In",
                DisplayPermission = new ContentPermission { TestPermitted = (roles, data) => !roles.Contains("E") },
                Url = "/Lynicon/Login",
                Section = "Global"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbListItems",
                Caption = "List",
                DisplayPermission = new ContentPermission("E"),
                Url = "/Lynicon/Items",
                Section = "Global"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbFilterItems",
                Caption = "Filtering",
                DisplayPermission = new ContentPermission("E"),
                Url = "/Lynicon/Items/List",
                Section = "Global"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbAdmin",
                Caption = "Admin",
                DisplayPermission = new ContentPermission("A"),
                Url = "/Lynicon/Admin",
                Section = "Global"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbUsers",
                Caption = "Users",
                DisplayPermission = new ContentPermission("A"),
                Url = "/Lynicon/Users?$orderby=Email&$top=15",
                Section = "Global"
            });

            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "save",
                Caption = "SAVE",
                DisplayPolicy = "CanEditData",
                Section = "Record",
                BackgroundColor = "#cbdfdf"
            });
            LyniconUi.Instance.FuncPanelButtons.Add(new FuncPanelButton
            {
                Id = "fpbMainDelete",
                Caption = "Delete",
                DisplayPolicy = "CanDeleteData",
                ClientClickScript = @"var $itemIdx = $('#lynicon_itemIndex');
                    var data = ($itemIdx.length > 0 ? { idx: $itemIdx.val() } : {});
                    if (!confirm('Are you sure you want to delete this item?'))
                        return;
                    $.post(""$$BaseUrl$$?$mode=delete$$OriginalQuery$$"",
                    data, function (res) {
                        window.location = window.location.href;
                    });",
                Section = "Record",
                BackgroundColor = "#8c8c8c"
            });

            var modifiedWhenFilter = new DateFieldFilter("Modified When", typeof(IBasicAuditable).GetProperty("Updated"));
            LyniconUi.Instance.Filters.Add(modifiedWhenFilter);
            var modifiedByFilter = new ForeignKeyFilter("Modified By", typeof(User), "UserName", typeof(IBasicAuditable).GetProperty("UserUpdated"));
            LyniconUi.Instance.Filters.Add(modifiedByFilter);
            var createdWhenFilter = new DateFieldFilter("Created When", typeof(IBasicAuditable).GetProperty("Created"));
            LyniconUi.Instance.Filters.Add(createdWhenFilter);
            var createdByFilter = new ForeignKeyFilter("Created By", typeof(User), "UserName", typeof(IBasicAuditable).GetProperty("UserCreated"));
            LyniconUi.Instance.Filters.Add(createdByFilter);

            log.Info("Core Module Initialised");

            return true;
        }
    }
}
