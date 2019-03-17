using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public class WebhookCache
    {
        private readonly Dictionary<ulong, DiscordWebhookClient> hookClients = new Dictionary<ulong, DiscordWebhookClient>();

        public async Task<DiscordWebhookClient> GetWebhook(ITextChannel channel, DiscordSocketClient discord)
        {
            if (!hookClients.TryGetValue(channel.Id, out var client))
            {
                var hooks = await channel.GetWebhooksAsync();
                var hook = hooks.FirstOrDefault(h => h.Creator.Id == discord.CurrentUser.Id) ??
                           await channel.CreateWebhookAsync($"DMRP Proxy Hook");

                client = new DiscordWebhookClient(hook);
                hookClients[channel.Id] = client;
            }

            return client;
        }
    }
}