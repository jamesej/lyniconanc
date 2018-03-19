using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Extensibility;
using Lynicon.Models;
using Lynicon.Repositories;
using Lynicon.Utility;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Lynicon.Services;

namespace Lynicon.Modules
{
    /// <summary>
    /// The content schema module activates and manages functionality for ensuring no dangerous changes to content types have occurred since the last run
    /// of the CMS
    /// </summary>
    public class ContentSchemaModule : Module
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ContentSchemaModule));

        public const string ControllerName = "ContentSchema";

        IHostingEnvironment hosting;

        public ContentSchemaModule([FromServices] LyniconSystem sys, [FromServices] IHostingEnvironment hosting, params string[] dependentOn)
            : base(sys, "ContentSchema", dependentOn)
        {
            this.hosting = hosting;
        }

        /// <summary>
        /// The previous content schema for all content types
        /// </summary>
        public ContentModelSchema LastSchema { get; set; }
        /// <summary>
        /// The current content schema for all content types
        /// </summary>
        public ContentModelSchema CurrentSchema { get; set; }

        public override bool Initialise()
        {
            LastSchema = null;
            Load();
            CurrentSchema = ContentModelSchema.Build();
            if (LastSchema != null)
            {
                ContentRepository.ChangeProblems = LastSchema.FindProblems(CurrentSchema);
                if (ContentRepository.ChangeProblems.Any())
                    LyniconUi.Instance.ShowProblemAlert = true;
            }
            Dump(); // save current schema with reversions for unresolved problems

            return true;
        }

        public void Load()
        {
            FileInfo fi = new FileInfo(Path.Combine(hosting.WebRootPath, "data", "LastSchema.json"));

            if (fi.Exists)
            {
                try
                {
                    var sz = new JsonSerializer();
                    using (var stream = fi.OpenRead())
                    using (var reader = new StreamReader(stream))
                    using (var jsonTextReader = new JsonTextReader(reader))
                    {
                        LastSchema = sz.Deserialize<ContentModelSchema>(jsonTextReader);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        public override void MapRoutes(IRouteBuilder builder)
        {
            builder.MapRoute("contentschema",
                "Lynicon/" + ControllerName + "/{action}",
                new { controller = ControllerName, action = "Index", Area = "Lynicon" }
            );
        }

        public void Dump()
        {
            try
            {
                var schema = this.CurrentSchema.Copy();
                ContentRepository.ChangeProblems.Do(cp => schema.ApplyProblem(this.LastSchema, cp));

                DirectoryInfo di = new DirectoryInfo(Path.Combine(hosting.WebRootPath, "data"));
                if (!di.Exists)
                    di.Create();
                FileInfo fi = new FileInfo(Path.Combine(hosting.WebRootPath, "data", "LastSchema.json"));
                var sz = new JsonSerializer();
                sz.Formatting = Formatting.Indented;
                using (var stream = fi.OpenWrite())
                using (var writer = new StreamWriter(stream))
                using (var jsonTextWriter = new JsonTextWriter(writer))
                {
                    sz.Serialize(writer, schema);
                }
            }
            catch (Exception ex)
            {
                log.Error("Error creating content schema module dump: ", ex);
            }
        }

        public override void Shutdown()
        {
            Dump();
        }

        public override ModuleAdminViewModel GetViewModel()
        {
            return new CacheAdminViewModel
            {
                ItemCount = () => CurrentSchema.ContentTypes.Count,
                MemoryBytes = null,
                ViewName = "/Areas/Lynicon/Views/ContentSchema/Manager.cshtml",
                ManagerController = ControllerName,
                WriteToFileUrl = "/lynicon/" + ControllerName + "/WriteToFile",
                Error = Error,
                Title = this.Name
            };
        }
    }
}
