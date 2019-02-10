using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public class ProxyHelper
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly DiscordSocketClient discord;
        private readonly IConfiguration cfg;
        private readonly string avatarBaseUrl;

        public ProxyHelper(DiscordSocketClient discord, IProxyBuilder proxyBuilder, IConfiguration cfg)
        {
            this.discord = discord;
            this.cfg = cfg;

            var host = cfg["Discord:bot-url"] ?? "http://localhost";
            host += host.EndsWith('/') ? string.Empty : "/";
            avatarBaseUrl = $"{host}Avatar/View";
        }

        public async Task SendMessage(DiscordWebhookClient webhook,
            Proxy proxy,
            string text,
            IReadOnlyCollection<IAttachment> attachments)
        {
            var proxyUser = discord.GetUser(proxy.BotUser.DiscordId);
            var username = $"{proxy.Name} [{proxyUser.Username}]";

            var avatarUrl = proxy.HasAvatar
                ? $"{avatarBaseUrl}/{proxy.AvatarGuid}"
                : proxyUser.GetAvatarUrl();

            if (attachments.Any())
            {
                var first = true;
                foreach (var a in attachments)
                {
                    // TODO: Single multipart request
                    var stream = await new HttpClient().GetStreamAsync(a.Url);
                    await webhook.SendFileAsync(stream, a.Filename, first ? text : String.Empty,
                        username: username,
                        avatarUrl: avatarUrl);
                    first = false;
                }
            }
            else
            {
                await webhook.SendMessageAsync(text,
                    username: username,
                    avatarUrl: avatarUrl);
            }
        }
    }
}