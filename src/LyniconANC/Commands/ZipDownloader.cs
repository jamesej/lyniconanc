using Lynicon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Commands
{
    public class ZipDownloader
    {
        public async Task Download(Uri uri, string savePath, bool overwrite)
        {
            ZipArchive zip;
            using (var cli = new HttpClient())
            {
                try
                {
                    cli.BaseAddress = new Uri(uri.Scheme + "://" + uri.Host);
                    string path = uri.LocalPath.Substring(1);
                    var resp = await cli.GetAsync(path);
                    resp.EnsureSuccessStatusCode();
                    zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to download files from " + uri.ToString() + ": " + ex.ToString());
                    return;
                }
            }

            Console.WriteLine("Downloaded files for Lynicon:");

            foreach (var entry in zip.Entries)
            {
                if (entry.Name == "") // internal folder
                    continue;

                string filePath = "";
                try
                {
                    var entryPath = entry.FullName;
                    string partialPath = IOX.EnsurePathToFile(savePath, entryPath.After("/"), '/');

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

                    string localPath = filePath.Substring(savePath.Length);
                    Console.WriteLine(localPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to set up new file at " + filePath + ": " + ex.ToString());
                }
            }
        }
    }
}