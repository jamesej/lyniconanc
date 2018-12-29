using Lynicon.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Membership;
using Lynicon.Services;
using Microsoft.AspNetCore.Routing;

namespace LyniconANC.Test
{
    public class TestVersioner : Versioner
    {
        public TestVersioner(LyniconSystem sys) : base(sys)
        { }

        public override object[] AllVersionValues
        {
            get
            {
                return new object[] { "en-GB", "es-ES" };
            }
        }

        public override bool IsAddressable
        {
            get
            {
                return true;
            }
        }

        public override object PublicVersionValue
        {
            get
            {
                return null;
            }
        }

        public override string VersionKey
        {
            get
            {
                return "testV";
            }
        }

        public override VersionDisplay DisplayItemVersion(ItemVersion version)
        {
            return new VersionDisplay
            {
                CssClass = "testVVersion",
                ListItem = (string)version[VersionKey],
                Text = (string)version[VersionKey],
                Title = (string)version[VersionKey]
            };
        }

        public override List<object> GetAllowedVersions(IUser u)
        {
            return this.AllVersionValues.ToList();
        }

        public override object GetItemValue(object container)
        {
            return "en-GB";
        }

        public override object CurrentValue(VersioningMode mode, RouteData rd)
        {
            return "en-GB";
        }

        public override void SetItemValue(object value, object container)
        {
        }

        public override bool TestVersioningMode(object container, VersioningMode mode)
        {
            switch (mode)
            {
                case VersioningMode.All:
                    return true;
                case VersioningMode.Current:
                    return true;
                case VersioningMode.Public:
                    return true;
                case VersioningMode.Specific:
                    return true;
            }

            return false;
        }

        public override bool Versionable(Type containerType)
        {
            return true;
        }
    }
}
