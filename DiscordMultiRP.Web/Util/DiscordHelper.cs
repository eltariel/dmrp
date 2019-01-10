using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordMultiRP.Web.Util
{
    public class DiscordHelper
    {
        private readonly IConfiguration cfg;
        public DiscordHelper(IConfiguration cfg)
        {
            this.cfg = cfg;
        }

        public async Task<DiscordSocketClient> LoginBot()
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
    }
}