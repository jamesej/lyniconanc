using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    public static class IOX
    {
        /// <summary>
        /// Ensure all the directories in a path exist
        /// </summary>
        /// <param name="basePath">The path to the last directory we know exists</param>
        /// <param name="relPath">the rest of the path including the filename (this could be a dummy)</param>
        /// <param name="delimiter">the delimiter used in the relPath</param>
        /// <returns>The file system path to the end of the relPath</returns>
        public static string EnsurePathToFile(string basePath, string relPath, char delimiter)
        {
            var pathEls = relPath.Split(delimiter).ToList();
            var partialPath = basePath;
            foreach (string pathEl in pathEls.Take(pathEls.Count - 1))
            {
                var dirPath = Path.Combine(partialPath, pathEl);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                partialPath = dirPath;
            }

            return partialPath;
        }
    }
}
