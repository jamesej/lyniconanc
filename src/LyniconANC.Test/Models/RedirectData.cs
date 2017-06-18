using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Lynicon.Attributes;
using Lynicon.Models;

namespace LyniconANC.Test.Models
{
    [Table("RedirectData"), RedirectPropertySource("Redirected > X", ContentType = typeof(RedirectTargetContent), SourceDescriptor = "{0}")]
    public class RedirectData
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Summary, AddressComponent(UsePath=true)]
        public string Title { get; set; }

        public string Data { get; set; }

        public string Redirected { get; set; }
    }
}