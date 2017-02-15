using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Tools
{
    public class ZipDownloader
    {
        public void Download(Uri uri, string savePath, bool overwrite)
        {
            ZipArchive zip;
            try
            {
                var req = HttpWebRequest.CreateHttp(uri);
                var resp = req.GetResponse();
                zip = new ZipArchive(resp.GetResponseStream());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to download files from " + uri.ToString() + ": " + ex.ToString());
                return;
            }

            Console.WriteLine("Downloaded files for Lynicon");

            foreach (var entry in zip.Entries)
            {
                if (entry.Name == "") // internal folder
                    continue;

                string filePath = "";
                try
                {
                    var entryPath = entry.FullName;
                    var pathEls = entryPath.Split('/').Skip(1).ToList();
                    var partialPath = savePath;
                    foreach (string pathEl in pathEls.Take(pathEls.Count - 1))
                    {
                        var dirPath = partialPath + "\\" + pathEl;
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        partialPath = dirPath;
                    }

                    using (var strm = entry.Open())
                    {
                        filePath = partialPath + "\\" + entry.Name;
                        bool fileExists = File.Exists(filePath);

                        if (fileExists && overwrite)
                            File.Delete(filePath);

                        if (overwrite || !fileExists)
                        {
                            using (var writeStream = File.OpenWrite(filePath))
                            {
                                strm.CopyTo(writeStream);
                            }
                        }
                    }

                    Console.WriteLine("File written " + filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to set up new file at " + filePath + ": " + ex.ToString());
                }
            }
        }
    }
}