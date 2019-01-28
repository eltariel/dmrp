using System;
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

namespace DiscordMultiRP.Bot.ProxyResponder
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

            var host = cfg["Discord:bot-url"] ?? "http://localhost";
            host += host.EndsWith('/') ? string.Empty : "/";
            avatarBaseUrl = $"{host}Avatar/View";
        }

        public async Task HandleMessage(SocketMessage msg)
        {
            var user = await proxyBuilder.GetBotUserById(msg.Author.Id);
            if (user != null && msg.Channel is ITextChannel c)
            {
                log.Debug($"Found {user.Proxies.Count} registered proxies for user {msg.Author}");
                var m = MatchProxyContent(msg, user);

                try
                {
                    if (m.IsReset)
                    {
                        log.Debug($"Reset message for {msg.Author} in {c.Guild.Name}:{c.Name}");
                        await proxyBuilder.ClearLastProxyForUserAndChannel(user, c.Id);
                        await msg.DeleteAsync();
                    }
                    else
                    {
                        var (text, p) = (m.Text, m.Proxy);
                        if (!m.IsSuccess)
                        {
                            log.Debug($"Using last proxy for {msg.Author} in {c.Guild.Name}:{c.Name}");
                            p = await proxyBuilder.GetLastProxyForUserAndChannel(user, c.Id);
                            text = msg.Content;
                        }

                        if (p != null)
                        {
                            log.Debug($"{(p.IsGlobal ? "Global" : "Channel")} proxy message for {p.Name} [{msg.Author}] in channel {c.Guild.Name}:{c.Name} ({c.Id})");

                            await proxyBuilder.SetLastProxyForUserAndChannel(p, user, c.Id);
                            await SendMessage(msg, text, p);
                            await msg.DeleteAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn(ex, $"Can't handle message for {msg.Author}.");
                }
            }
        }

        private async Task SendMessage(SocketMessage msg, string text, Proxy proxy)
        {
            if (!(msg.Channel is ITextChannel c))
            {
                log.Debug($"Channel {msg.Channel} is not a text channel.");
                return;
            }

            var avatarUrl = proxy.HasAvatar
                ? $"{avatarBaseUrl}/{proxy.AvatarGuid}"
                : msg.Author.GetAvatarUrl();

            log.Debug($"Proxy avatar url: {avatarUrl}");

            var hc = await webhookCache.GetWebhook(c);

            if (msg.Attachments.Any())
            {
                var first = true;
                foreach (var a in msg.Attachments)
                {
                    // TODO: Single multipart request
                    var stream = await new HttpClient().GetStreamAsync(a.Url);
                    await hc.SendFileAsync(stream, a.Filename, first ? text : string.Empty,
                        embeds: msg.Embeds,
                        username: proxy.Name,
                        avatarUrl: avatarUrl);
                    first = false;
                }
            }
            else
            {
                await hc.SendMessageAsync(text,
                    username: $"{proxy.Name} [{msg.Author.Username}]",
                    embeds: msg.Embeds,
                    avatarUrl: avatarUrl);
            }
        }

        private MatchDescription MatchProxyContent(SocketMessage msg, BotUser botUser)
        {
            foreach (var p in botUser.Proxies.Where(p => p.IsForChannel(msg)))
            {
                var proxyMatch = regexCache.GetRegexFor(p).Match(msg.Content);
                if (proxyMatch.Success)
                {
                    return new MatchDescription(proxyMatch.Groups["text"].Value, p);
                }
            }

            var resetMatch = regexCache.GetRegexForReset(botUser)?.Match(msg.Content);
            if (resetMatch?.Success ?? false)
            {
                return new MatchDescription(resetMatch.Groups["text"].Value, botUser);
            }

            return new MatchDescription();
        }
    }
}