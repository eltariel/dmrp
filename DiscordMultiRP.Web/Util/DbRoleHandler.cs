using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordMultiRP.Web.Util
{
    public class DbRoleHandler : AuthorizationHandler<DbRoleRequirement>
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DbRoleRequirement requirement)
        {
            if (context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) is Claim claim &&
                ulong.TryParse(claim.Value, out var discordId))
            {
                var actionContext = (ActionContext)context.Resource;
                var db = actionContext.HttpContext.RequestServices.GetRequiredService<ProxyDataContext>();
                var dbUser = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
                if (dbUser?.IsAdmin ?? false)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}