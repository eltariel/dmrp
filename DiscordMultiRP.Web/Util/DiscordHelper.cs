using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Web.Util
{
    public class DiscordHelper
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IConfiguration cfg;
        public DiscordHelper(IConfiguration cfg)
        {
            this.cfg = cfg;
        }

        public async Task<DiscordSocketClient> LoginBot()
        {
            try
            {
                var discord = new DiscordSocketClient();
                var ready = new TaskCompletionSource<bool>();
                discord.Ready += OnDiscordReady;

                await discord.LoginAsync(TokenType.Bot, cfg["Discord:bot-token"]);
                await discord.StartAsync();
                await ready.Task;
                return discord;

                Task OnDiscordReady()
                {
                    ready.SetResult(true);
                    discord.Ready -= OnDiscordReady;
                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error connecting to discord");
                return null;
            }
        }

        public static ulong GetDiscordUserIdFor(ClaimsPrincipal user)
        {
            return ulong.Parse(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}