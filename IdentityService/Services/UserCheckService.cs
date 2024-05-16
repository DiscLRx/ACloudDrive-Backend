using System.Text.RegularExpressions;
using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services
{
    public partial class UserCheckService(MsSqlContext msSqlContext)
    {
        public UserFormatChecker FormatChecker { get; } = new();
        public UserUniqueChecker UniqueChecker { get; } = new(msSqlContext.Users);

        public partial class UserFormatChecker
        {
            private static readonly string[] AllowedRoles = ["USER", "ADMIN"];

            [GeneratedRegex("^[A-Za-z0-9_]{4,50}$")]
            private partial Regex CommonStringRegex();
            public bool CommonCheck(string? str)
            {
                return CommonStringRegex().IsMatch(str ?? "");
            }

            [GeneratedRegex("^[\\w\\-\\.]+@([\\w-]+\\.)+[\\w-]{2,}$")]
            private partial Regex EmailFormatRegex();
            public bool EmailCheck(string? email)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                if (email.Length > 50)
                {
                    return false;
                }

                var reg = EmailFormatRegex();
                if (!reg.IsMatch(email))
                {
                    return false;
                }

                return true;
            }

            public bool DisplayNameCreateCheck(string displayName)
            {
                return displayName.Length <= 50;
            }

            public bool DisplayNameUpdateCheck(string displayName)
            {
                return displayName.Length is > 0 and <= 50;
            }

            public bool TotalSpaceCheck(long size) => size >= 0;

            public bool RoleCheck(string role) => AllowedRoles.Contains(role);
        }

        public class UserUniqueChecker(DbSet<User> users)
        {
            private readonly DbSet<User> _users = users;

            public async Task<bool> UsernameCheckAsync(string username)
            {
                return (await _users.Where(u => u.Username == username).ToListAsync()).Count == 0;
            }

            public async Task<bool> EmailCheckAsync(string email)
            {
                return (await _users.Where(u => u.Email == email).ToListAsync()).Count == 0;
            }
        }

    }
}
