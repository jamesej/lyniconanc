using Lynicon.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    [Table("TestData")]
    public class TestData
    {
        [Key]
        public int Id { get; set; }

        public string Value1 { get; set; }

        [AddressComponent(UsePath = true)]
        public string Path { get; set; }

        public string Title { get; set; }

        public int ValueInt { get; set; }

        public string ExtVal { get; set; }
    }
}
