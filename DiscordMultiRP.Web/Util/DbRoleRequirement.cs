using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Authorization;

namespace DiscordMultiRP.Web.Util
{
    public class DbRoleRequirement : IAuthorizationRequirement
    {
        public const string RequiresAdmin = "RequiresAdmin";
        public const string RequiresGlobal = "RequiresGlobal";

        public DbRoleRequirement(Role role)
        {
            Role = role;
        }

        public Role Role { get; }
    }
}