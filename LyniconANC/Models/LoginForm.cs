using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lynicon.Attributes;
using System.ComponentModel;

namespace Lynicon.Models
{
    /// <summary>
    /// Class to hold user inputs from the login form
    /// </summary>
    public class LoginForm
    {
        /// <summary>
        /// The user's user name
        /// </summary>
        [DisplayName("User Name")]
        public string UserName { get; set; }
        
        /// <summary>
        /// The user's password
        /// </summary>
        [DisplayName("Password")]
        public string Password { get; set; }
    }
}
