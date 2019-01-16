using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Discord;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot.Proxy
{
    public class UserProxy
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IProxyBuilder proxyBuilder;
        private readonly IConfiguration cfg;
        private readonly RegexCache regexCache;
        private readonly WebhookCache webhookCache;
        private readonly string avatarBaseUrl;

        public UserProxy(DiscordSocketClient discord, IProxyBuilder proxyBuilder, IConfiguration cfg)
        {
            this.proxyBuilder = proxyBuilder;
            this.cfg = cfg;
            regexCache = new RegexCache();
            webhookCache = new WebhookCache(discord);

            var host = cfg["bot-url"];
            host += host.EndsWith('/') ? string.Empty : "/";
            avatarBaseUrl = $"{host}/Proxies/Avatar";
        }

        public async Task HandleMessage(SocketMessage msg)
        {
            var user = await proxyBuilder.GetUserById(msg.Author.Id);
            if (user != null && msg.Channel is ITextChannel c)
            {
                log.Debug($"Found {user.Proxies.Count} registered proxies for user {msg.Author}");
                var (p, text) = MatchProxyContent(msg, user.Proxies);
                if (p == null)
                {
                    log.Debug($"Using last proxy for {msg.Author} in {c.Guild.Name}:{c.Name}");
                    p = await proxyBuilder.GetLastProxyForUserAndChannel(user, c.Id);
                    text = msg.Content;
                }

                if (p != null)
                {
                    try
                    {
                        log.Debug($"Proxying message in channel {c.Guild.Name}:{c.Name} ({c.Id})" +
                                  $" for user {msg.Author} - " +
                                  $"proxy name is {p.Name} ({(p.IsGlobal ? "global" : "channel")})");

                        await proxyBuilder.SetLastProxyForUserAndChannel(p, user, c.Id);
                        if (!p.IsReset)
                        {
                            await SendMessage(msg, text, p);
                        }

                        await msg.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex, $"Can't proxy message for {p.Name}.");
                    }
                }
            }
        }

        private async Task SendMessage(SocketMessage msg, string text, Data.Proxy proxy)
        {
            if (!(msg.Channel is ITextChannel c))
            {
                log.Debug($"Channel {msg.Channel} is not a text channel.");
                return;
            }

            if (!proxy.IsReset)
            {
                var avatarUrl = !string.IsNullOrWhiteSpace(proxy.AvatarContentType)
                    ? $"{avatarBaseUrl}/{proxy.Id}"
                    : msg.Author.GetAvatarUrl();

                var hc = await webhookCache.GetWebhook(c);
                await hc.SendMessageAsync(text,
                    username: $"{proxy.Name} [{msg.Author.Username}]",
                    embeds: msg.Embeds,
                    avatarUrl: avatarUrl);

                if (msg.Attachments.Any())
                {
                    foreach (var a in msg.Attachments)
                    {
                        // TODO: Single multipart request
                        var stream = await new HttpClient().GetStreamAsync(a.Url);
                        await hc.SendFileAsync(stream, a.Filename, "",
                            embeds: msg.Embeds,
                            username: proxy.Name,
                            avatarUrl: avatarUrl);
                    }
                }
            }
        }

        private (Data.Proxy proxy, string text) MatchProxyContent(SocketMessage msg, IEnumerable<Data.Proxy> proxies)
        {
            foreach (var p in proxies.Where(p => p.IsForChannel(msg)))
            {
                var m = regexCache.GetRegexFor(p).Match(msg.Content);
                if (m.Success)
                {
                    return (proxy: p, text: m.Groups["text"].Value);
                }
            }

            return (null, null);
        }
    }
}