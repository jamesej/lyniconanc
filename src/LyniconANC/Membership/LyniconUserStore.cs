using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Lynicon.Repositories;
using Lynicon.DataSources;
using Lynicon.Collation;

namespace Lynicon.Membership
{
    public class LyniconUserStore<TUser> : IUserStore<TUser>, IUserEmailStore<TUser>, IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>, IQueryableUserStore<TUser>
        where TUser : User
    {
        public IQueryable<TUser> Users
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            if (!user.Roles.Contains(roleName))
                user.Roles += roleName;
            Collator.Instance.Set(user, false);
            return Task.FromResult(0);
        }

        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            Collator.Instance.Set(user, true);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            Collator.Instance.Delete(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public void Dispose()
        { }

        public Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            var user = Collator.Instance
                .Get<TUser, TUser>(iq => iq.Where(u => u.NormalisedEmail == normalizedEmail))
                .FirstOrDefault();

            return Task.FromResult(user);
        }

        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            Guid id = new Guid(userId);
            var user = Collator.Instance
                .Get<TUser, TUser>(iq => iq.Where(u => u.Id == id))
                .FirstOrDefault();

            return Task.FromResult(user);
        }

        public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            TUser user = Collator.Instance
                .Get<TUser, TUser>(iq => iq.Where(u => u.UserName.ToUpper() == normalizedUserName))
                .FirstOrDefault();

            return Task.FromResult(user);
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalisedEmail);
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalisedUserName);
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Password);
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            IList<string> list = (user.Roles ?? "").ToCharArray().Select(c => new string(new char[] { c })).ToList();
            return Task.FromResult(list);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.IdAsString);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            IList<TUser> list = Collator.Instance
                .Get<TUser, TUser>(iq => iq.Where(u => (u.Roles ?? "").Contains(roleName)))
                .ToList();
            return Task.FromResult(list);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.Password));
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            return Task.FromResult((user.Roles ?? "").Contains(roleName));
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            user.Roles = (user.Roles ?? "").Replace(roleName, "");
            Collator.Instance.Set(user);
            return Task.FromResult(0);
        }

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalisedEmail = normalizedEmail;
            return Task.FromResult(0);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalisedUserName = normalizedName;
            return Task.FromResult(0);
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.Password = passwordHash;
            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.FromResult(0);
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            Collator.Instance.Set(user);
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
