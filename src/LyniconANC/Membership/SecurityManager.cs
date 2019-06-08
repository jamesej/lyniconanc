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

        public static void EnsureAdminUser(string password)
        {
            Current.EnsureRoles("AEU");

            var adminUser = Collator.Instance.Get<User, User>(iq => iq.Where(u => u.UserName == "administrator")).FirstOrDefault();
            if (adminUser == null)
            {
                Guid adminUserId = Guid.NewGuid();
                //adminUser = Collator.Instance.GetNew<User>(new Address(typeof(User), adminUserId.ToString()));
                adminUser = Collator.Instance.GetNew<User>((Address)null);
                adminUser.Email = "admin@lynicon-user.com";
                adminUser.Id = adminUserId;
                adminUser.Roles = "AEU";
                adminUser.UserName = "administrator";
                Collator.Instance.Set(adminUser, true);
            }

            Current.SetPassword(adminUser.IdAsString, password);
        }

    }
}
