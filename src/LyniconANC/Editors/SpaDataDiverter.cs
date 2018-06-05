
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Lynicon.Routing;
using Lynicon.Membership;
using Microsoft.Extensions.Primitives;

namespace Lynicon.Editors
{
    /// <summary>
    /// Common functionality for deciding on editor redirection
    /// </summary>
    public class SpaDataDiverter : DataDiverter
    {
        public SpaDataDiverter()
        {
            this.DefaultHandler = new SpaDiversionStrategy("Lynicon", "DualFrameEditor");
            Register(typeof(List<>), new SpaDiversionStrategy("Lynicon", "ListEditor"));
        }
    }
}
