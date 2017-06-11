using Lynicon.Attributes;
using Lynicon.Collation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Models
{
    /// <summary>
    /// Indicates that the content item has a method to generate a default address to be
    /// used by the Data API
    /// </summary>
    public interface ICoreMetadata
    {
        [ScaffoldColumn(false)]
        Guid Id { get; set; }
        [ScaffoldColumn(false)]
        Guid Identity { get; set; }
        [ScaffoldColumn(false), AddressComponent(UsePath = true)]
        string Path { get; set; }
        [ScaffoldColumn(false)]
        DateTime Created { get; set; }
        [ScaffoldColumn(false)]
        string UserCreated { get; set; }
        [ScaffoldColumn(false)]
        DateTime Updated { get; set; }
        [ScaffoldColumn(false)]
        string UserUpdated { get; set; }
    }

    public static class ICoreMetadataX
    {
        public static bool HasMetadata(this ICoreMetadata data)
        {
            return data.Id != Guid.Empty;
        }

        public static void CopyPropertiesTo(this ICoreMetadata data, ICoreMetadata copiedTo)
        {
            copiedTo.Id = data.Id;
            copiedTo.Identity = data.Identity;
            copiedTo.Path = data.Path;
            copiedTo.Created = data.Created;
            copiedTo.UserCreated = data.UserCreated;
            copiedTo.Updated = data.Updated;
            copiedTo.UserUpdated = data.UserUpdated;
        }
    }
}
