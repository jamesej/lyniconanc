using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Lynicon.Membership
{
    public class LyniconRoleStore : IRoleStore<IdentityRole>
    {
        public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public void Dispose()
        { }

        public Task<IdentityRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityRole>(new IdentityRole { Id = roleId, Name = roleId, NormalizedName = roleId });
        }

        public Task<IdentityRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityRole>(new IdentityRole { Id = normalizedRoleName, Name = normalizedRoleName, NormalizedName = normalizedRoleName });
        }

        public Task<string> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(role.NormalizedName);
        }

        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(role.Id);
        }

        public Task<string> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(IdentityRole role, string normalizedName, CancellationToken cancellationToken)
        {
            return Task.FromResult<int>(0);
        }

        public Task SetRoleNameAsync(IdentityRole role, string roleName, CancellationToken cancellationToken)
        {
            return Task.FromResult<int>(0);
        }

        public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }
    }
}
