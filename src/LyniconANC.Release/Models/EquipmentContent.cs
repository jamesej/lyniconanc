using Lynicon.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LyniconANC.Release.Models
{
    public class EquipmentSummary : ProductSummary
    { }

    // We declare this empty type because we do want to have Tile products and Equipment products
    // be distinct content types rather than one inheriting from the other creating a confusing information
    // architecture
    [SummaryType(typeof(EquipmentSummary))]
    public class EquipmentContent : ProductContent
    {

    }
}
