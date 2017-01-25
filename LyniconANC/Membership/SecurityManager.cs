using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using Lynicon.Utility;
using System.Threading;
using System.Security.Claims;
using Lynicon.Collation;

namespace Lynicon.Membership
{
    /// <summary>
    /// Security Manager provides methods to assist in forms authentication using the Lightweight Membership classes.
    /// </summary>
    public class SecurityManager
    {
        static ISecurityManager current = null;
        public static ISecurityManager Current { get { return current; } set { current = value; } }
        public static TSecM CurrentAs<TSecM>()
            where TSecM : class, ISecurityManager
        {
            return current as TSecM;
        }

        static SecurityManager() { }

    }
}
