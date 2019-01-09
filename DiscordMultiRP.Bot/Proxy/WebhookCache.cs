using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;

namespace DiscordMultiRP.Bot.Proxy
{
    public class WebhookCache
    {
        private readonly DiscordSocketClient discord;

        public WebhookCache(DiscordSocketClient discord)
        {
            this.discord = discord;
        }

        public async Task<DiscordWebhookClient> GetWebhook(ITextChannel channel)
        {
            var hookClients = new Dictionary<ulong, DiscordWebhookClient>();
            if (!hookClients.TryGetValue(channel.Id, out var client))
            {
                var hooks = await channel.GetWebhooksAsync();
                var hook = hooks.FirstOrDefault(h => h.Creator.Id == discord.CurrentUser.Id) ??
                           await channel.CreateWebhookAsync($"DiscordMultiRP Proxy Hook - {channel.Name}");

                client = new DiscordWebhookClient(hook);
                hookClients[channel.Id] = client;
            }

            return client;
        }
    }
}