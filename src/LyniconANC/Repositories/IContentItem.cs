using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Collation;
using Lynicon.Extensibility;
using Lynicon.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lynicon.Repositories
{
    public interface IContentItem
    {
        Guid Id { get; set; }

        Guid Identity { get; set; }
        string DataType { get; set; }
        string Path { get; set; }
        string Locale { get; set; }
        string Summary { get; set; }
        string Content { get; set; }
    }
}
