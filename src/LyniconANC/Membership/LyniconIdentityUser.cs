using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Lynicon.Extensibility;
using Lynicon.Attributes;
using Lynicon.Models;

namespace Lynicon.Membership
{
    /// <summary>
    /// Internal IUser for use with ASP.Net Identity
    /// </summary>
    public class LyniconIdentityUser : User
    {
        //public virtual int AccessFailedCount { get; set; }
        //public virtual bool LockoutEnabled { get; set; }
        //public virtual DateTimeOffset? LockoutEnd { get; set; }
        //public virtual string PhoneNumber { get; set; }
        //public virtual bool PhoneNumberConfirmed { get; set; }
        //public virtual bool TwoFactorEnabled { get; set; }

        public LyniconIdentityUser()
        {
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
        }

        /// <summary>
        /// Test for whether logged in
        /// </summary>
        /// <returns>True if logged in</returns>
        public override bool IsLoggedIn()
        {
            return UserName != null;
        }

        /// <summary>
        /// Test for whether the user is anonymous
        /// </summary>
        /// <returns>True is user is anonymous</returns>
        public override bool IsAnon()
        {
            return false;
        }
    }
}
