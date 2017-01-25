using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Extensibility
{
    /// <summary>
    /// View model for displaying data about a cache module on the /Lynicon/Admin page
    /// </summary>
    public class CacheAdminViewModel : ModuleAdminViewModel
    {
        public Func<int> ItemCount { get; set; }
        public long? MemoryBytes { get; set; }
        public string ManagerController { get; set; }
        public string ReloadUrl { get; set; }
        public string WriteToFileUrl { get; set; }
    }
}
