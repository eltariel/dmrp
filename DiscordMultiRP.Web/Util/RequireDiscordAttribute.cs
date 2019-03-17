using System.Security.Claims;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DiscordMultiRP.Web.Util
{
    public class RequireDiscordAttribute : TypeFilterAttribute
    {
        public RequireDiscordAttribute() : base(typeof(RequiresDiscordFilterImpl))
        {
        }

        private class RequiresDiscordFilterImpl : IAsyncActionFilter
        {
            private readonly DiscordHelper discordHelper;
            private readonly ProxyDataContext db;

            public RequiresDiscordFilterImpl(DiscordHelper discordHelper, ProxyDataContext db)
            {
                this.discordHelper = discordHelper;
                this.db = db;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var discord = await discordHelper.LoginBot();
                if (discord == null)
                {
                    context.Result = new NotFoundObjectResult("Can't connect to Discord.");
                    return;
                }

                var httpContext = context.HttpContext;
                var discordId = DiscordHelper.GetDiscordUserIdFor(httpContext.User);
                var botUser = await db.BotUsers.FirstOrDefaultAsync(u => u.DiscordId == discordId);
                if (botUser == null)
                {
                    botUser = new BotUser {DiscordId = discordId, Role = Role.User};
                    db.BotUsers.Add(botUser);
                    db.SaveChanges();
                }

                httpContext.Items[typeof(BotUser)] = botUser;

                var resultContext = await next();
            }
        }
    }
}