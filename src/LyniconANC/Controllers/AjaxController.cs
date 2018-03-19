using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Lynicon.Utility;
using Lynicon.Attributes;
using Lynicon.Membership;
using Lynicon.Models;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lynicon.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Lynicon.Controllers
{
    /// <summary>
    /// General webservices for managing the file manager, plus other utilities for the CMS UI
    /// </summary>
    [Area("Lynicon")]
    public class AjaxController : Controller
    {
        private readonly IHostingEnvironment hosting;
        private readonly LyniconSystem sys;

        public AjaxController(IHostingEnvironment hosting, LyniconSystem sys)
        {
            this.hosting = hosting;
            this.sys = sys;
        }
        /// <summary>
        /// Get the folders in a folder in the file manager
        /// </summary>
        /// <param name="dir">the path to the folder</param>
        /// <returns>JSON list of folder information</returns>
        [Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult FileTreeFolders(string dir)
        {
            if (string.IsNullOrEmpty(dir)) dir = "/";

            if (!dir.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");

            IDirectoryContents di = hosting.WebRootFileProvider.GetDirectoryContents(dir);
            var output = di
                .Where(fi => fi.IsDirectory)
                .OrderBy(cdi => cdi.Name)
                .Select(cdi => new
                    {
                        data = new { title = cdi.Name },
                        state = "closed",
                        attr = new { title = dir + cdi.Name + "/" }
                    }).ToArray();
            return Json(output);
        }

        /// <summary>
        /// Get the files in a folder in the file manager
        /// </summary>
        /// <param name="dir">the path to the folder</param>
        /// <returns>JSON list of file information</returns>
        [Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult FileTreeFiles(string dir)
        {
            if (string.IsNullOrEmpty(dir)) dir = "/";

            if (!dir.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");

            IDirectoryContents di = hosting.WebRootFileProvider.GetDirectoryContents(dir);
            var output = new
            {
                dir = !di.Exists ? null : dir + (dir[dir.Length - 1] == '/' ? "" : "/"),
                dirs = !di.Exists ? null : di.Where(fi => fi.IsDirectory)
                    .OrderBy(cdi => cdi.Name)
                    .Select(cdi => new
                        {
                            name = cdi.Name
                        }).ToArray(),
                files = !di.Exists ? null : di.Where(fi => !fi.IsDirectory)
                    .OrderBy(cfi => cfi.Name)
                    .Select(cfi => new
                        {
                            name = cfi.Name,
                            ext = cfi.Name.LastAfter("."),
                            size = cfi.Length
                        }).OrderBy(inf => inf.name).ToArray()
            };
            return Json(output);

        }

        /// <summary>
        /// Rename a file in the file manager
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <param name="newName">The new name for the file</param>
        /// <returns>The new path of the file</returns>
        [HttpPost, Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult Rename(string path, string newName)
        {
            if (!path.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string filePath = hosting.WebRootFileProvider.GetFileInfo(path).PhysicalPath;
                if (filePath.EndsWith(sep)) filePath = filePath.Substring(0, filePath.Length - 1);
                string newFilePath = Path.Combine(filePath.UpToLast(sep), newName);
                System.IO.Directory.Move(filePath, newFilePath);
            }
            catch
            {
                return Json("");
            }
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            return Json(path.UpToLast("/") + "/" + newName);
        }

        /// <summary>
        /// Move a file to a different folder
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <param name="newDir">The path of the folder to move it to</param>
        /// <returns>JSON true if succeeds otherwise false</returns>
        [HttpPost, Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult Move(string path, string newDir)
        {
            if (!path.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string filePath = hosting.WebRootFileProvider.GetFileInfo(path).PhysicalPath;
                if (filePath.EndsWith(sep)) filePath = filePath.Substring(0, filePath.Length - 1);
                string newDirPath = hosting.WebRootFileProvider.GetFileInfo(newDir).PhysicalPath;
                if (newDirPath.EndsWith(sep)) newDirPath = newDirPath.Substring(0, newDirPath.Length - 1);
                string newFilePath = Path.Combine(newDirPath, filePath.LastAfter(sep));
                System.IO.Directory.Move(filePath, newFilePath);
            }
            catch
            {
                return Json(false);
            }
            return Json(true);
        }

        /// <summary>
        /// Delete the file at a given file manager path
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <returns>JSON true if succeeds</returns>
        [HttpPost, Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult Delete(string path)
        {
            if (!path.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");
            try
            {
                string filePath = hosting.WebRootFileProvider.GetFileInfo(path).PhysicalPath;
                if (filePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    filePath = filePath.Substring(0, filePath.Length - 1);
                    DirectoryInfo d = new DirectoryInfo(filePath);
                    d.Delete(recursive: true);
                }
                else
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error deleting: " + ex.Message);
            }
            return Json(true);
        }

        /// <summary>
        /// Create a new folder in the file manager
        /// </summary>
        /// <param name="path">Path to the folder</param>
        /// <returns>JSON true if succeeds</returns>
        [HttpPost, Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult CreateDir(string path)
        {
            if (!path.StartsWith(LyniconSystem.Instance.Settings.FileManagerRoot))
                return new HttpStatusCodeResult(403, "Cannot access this directory");

            try
            {
                string filePath = hosting.WebRootFileProvider.GetFileInfo(path).PhysicalPath;
                if (filePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    filePath = filePath.Substring(0, filePath.Length - 1);
                    DirectoryInfo d = new DirectoryInfo(filePath);
                    d.CreateSubdirectory("New Folder");
                }
                else
                    throw new Exception("Can't create directory in file");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error deleting: " + ex.Message);
            }

            return Json(true);
        }

        /// <summary>
        /// Encrypt a password
        /// </summary>
        /// <param name="pw">The password</param>
        /// <returns>The encrypted password in JSON</returns>
        //[HttpPost, Authorize(Roles = Membership.User.AdminRole)]
        //public ActionResult EncryptPassword(string pw)
        //{
        //    return Json(new { encrypted = LyniconSecurityManager.Current.EncryptPassword(pw) });
        //}

        /// <summary>
        /// Get the possible values for a reference given a partial string typed in by the user
        /// </summary>
        /// <param name="query">The partial string typed in</param>
        /// <param name="listId">A listId which contains possible multiple content types representing the list of possible values</param>
        /// <param name="allowedVsn">Serialized version which indicates which versions are allowed in the list</param>
        /// <returns></returns>
        [Authorize(Roles = Membership.User.EditorRole)]
        public IActionResult RefQuery(string query, string listId, string allowedVsn)
        {
            var types = listId.Split('_').Select(cn => ContentTypeHierarchy.GetContentType(cn)).ToList();
            bool showType = types.Count > 1;
            var qWords = query.ToLower().Split(new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            ItemVersion maskVsn = null;
            ItemVersion currMaskedVsn = null;
            bool versioned = false;
            if (!string.IsNullOrEmpty(allowedVsn))
            {
                maskVsn = new ItemVersion(allowedVsn);
                ItemVersion curr = sys.Versions.CurrentVersion;
                ItemVersion vsn = curr.Superimpose(maskVsn);
                currMaskedVsn = curr.Mask(maskVsn);
                sys.Versions.PushState(VersioningMode.Specific, vsn);
                versioned = true;
            }

            try
            {
                var cachedTypes = types.Where(t => Cache.IsTotalCached(LyniconModuleManager.Instance, t, true)).ToList();
                var uncachedTypes = types.Except(cachedTypes).ToList();
                var items = Enumerable.Range(0, 1).Select(n => new { label = "", value = "" }).ToList();
                items.Clear();
                if (uncachedTypes.Count > 0)
                {
                    // TO DO add attribute for containers specifying which field or fields to scan for title, add code to create query to scan here
                }
                if (cachedTypes.Count > 0)
                {
                    items.AddRange(Collator.Instance.Get<Summary, Summary>(cachedTypes,
                        iq => iq.Where(s => qWords.All(w => ((s.Title ?? "").ToLower() + " " + s.Type.Name.ToLower()).Contains(w))).Take(30))                
                        .Select(summ => new
                        {
                            label = summ.Title + (showType ? " (" + LyniconUi.ContentClassDisplayName(summ.Type) + ")" : "")
                                    + (versioned && !currMaskedVsn.ContainedBy(summ.Version.Mask(maskVsn)) ? " [" + sys.Versions.DisplayVersion(summ.Version.Mask(maskVsn)).Select(dv => dv.Text).Join(" ") + "]" : ""),
                            value = versioned ? summ.ItemVersionedId.Mask(maskVsn).ToString() : summ.ItemId.ToString()
                        })
                        .OrderBy(s => s.label));
                }

                return Json(new { items });
            }
            finally
            {
                if (versioned)
                    sys.Versions.PopState();
            }
        }
    }
}
