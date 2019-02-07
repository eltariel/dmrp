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

        private class RequiresDiscordFilterImpl : IAsyncResourceFilter
        {
            private readonly DiscordHelper discordHelper;
            private readonly ProxyDataContext db;

            public RequiresDiscordFilterImpl(DiscordHelper discordHelper, ProxyDataContext db)
            {
                this.discordHelper = discordHelper;
                this.db = db;
            }

            public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
            {
                var discord = await discordHelper.LoginBot();
                if (discord == null)
                {
                    context.Result = new NotFoundObjectResult("Can't connect to Discord.");
                }

                var httpContext = context.HttpContext;
                var discordId = DiscordHelper.GetDiscordUserIdFor(httpContext.User);
                var botUser = await db.BotUsers.FirstOrDefaultAsync(u => u.DiscordId == discordId);
                httpContext.Items[typeof(BotUser)] = botUser;

                var resultContext = await next();
            }
        }
    }
}