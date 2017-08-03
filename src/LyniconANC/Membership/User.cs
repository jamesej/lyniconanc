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
    /// The base Lynicon user record definition
    /// </summary>
    [Table("Users"), Serializable]
    public class User : IUser
    {
        public const string AdminRole = "A";
        public const string EditorRole = "E";
        public const string UserRole = "U";
        public const string ReportingRoles = "R,A";

        public static string TableName { get; set; }

        /// <summary>
        /// The user's username
        /// </summary>
        public string UserName { get; set; }

        [NotMapped]
        public string NormalisedUserName
        {
            get { return UserName.ToUpper(); }
            set { }
        }

        [Summary, NotMapped, ScaffoldColumn(false)]
        public string Title { get { return UserName; } }

        /// <summary>
        /// User's primary key id
        /// </summary>
        [Key, Editable(false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Conversion of Id to string
        /// </summary>
        [NotMapped, ScaffoldColumn(false)]
        public string IdAsString
        {
            get { return Id.ToString(); }
            set { Id = new Guid(value); }
        }

        /// <summary>
        /// User's email
        /// </summary>
        public string Email { get; set; }

        [NotMapped]
        public string NormalisedEmail
        {
            get { return Email.ToUpper(); }
            set { }
        }

        /// <summary>
        /// When created
        /// </summary>
        [Editable(false)]
        public DateTime Created { get; set; }

        /// <summary>
        /// When last modified
        /// </summary>
        [Editable(false)]
        public DateTime Modified { get; set; }

        /// <summary>
        /// Password (as encrypted)
        /// </summary>
        [UIHint("PasswordWithEncrypter")]
        public string Password { get; set; }

        /// <summary>
        /// Roles as a string of role letters
        /// </summary>
        public string Roles { get; set; }


        public User()
        {
            Id = Guid.Empty;
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
        }

        /// <summary>
        /// Test for whether logged in
        /// </summary>
        /// <returns>True if logged in</returns>
        public virtual bool IsLoggedIn()
        {
            return UserName != null;
        }

        /// <summary>
        /// Test for whether the user is anonymous
        /// </summary>
        /// <returns>True is user is anonymous</returns>
        public virtual bool IsAnon()
        {
            return Id == Guid.Empty;
        }
    }
}
