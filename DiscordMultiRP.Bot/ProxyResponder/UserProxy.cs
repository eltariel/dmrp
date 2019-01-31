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
            var botUser = await proxyBuilder.GetBotUserById(msg.Author.Id);
            if (botUser != null && msg.Channel is ITextChannel c)
            {
                log.Debug($"Found {botUser.Proxies.Count} registered proxies for user {msg.Author}");
                var m = MatchProxyContent(msg, botUser);

                try
                {
                    if (m.IsReset)
                    {
                        log.Debug($"Reset message for {msg.Author} in {c.Guild.Name}:{c.Name}");
                        await proxyBuilder.ClearLastProxyForUserAndChannel(botUser, c.Id);
                        await msg.DeleteAsync();
                    }
                    else
                    {
                        var (text, p) = (m.Text, m.Proxy);
                        if (!m.IsSuccess)
                        {
                            log.Debug($"Using last proxy for {msg.Author} in {c.Guild.Name}:{c.Name}");
                            p = await proxyBuilder.GetLastProxyForUserAndChannel(botUser, c.Id);
                            text = msg.Content;
                        }

                        if (p != null)
                        {
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                await ClaimLastMessage(msg, p, botUser);
                            }
                            else
                            {
                                log.Debug(
                                    $"{(p.IsGlobal ? "Global" : "Channel")} proxy message for {p.Name} [{msg.Author}] "
                                    + $"in channel {c.Guild.Name}:{c.Name} ({c.Id})");

                                await proxyBuilder.SetLastProxyForUserAndChannel(p, botUser, c.Id);
                                await SendMessage(msg, text, p);
                                await msg.DeleteAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn(ex, $"Can't handle message for {msg.Author}.");
                }
            }
        }

        private async Task ClaimLastMessage(SocketMessage msg, Proxy proxy, BotUser botUser)
        {
            log.Debug($"User {botUser} claiming last non-specific message for proxy {proxy.Name}");
            var c = msg.Channel;
            var chm = (await msg.Channel.GetMessagesAsync().ToList()).SelectMany(x => x).ToList();
            var last = chm.FirstOrDefault(m => m.Id != msg.Id &&
                                               m.Author.Id == msg.Author.Id ||
                                               IsOwnProxy(m.Author, msg.Author));

            if (last is IMessage lm)
            {
                var username = msg.Author.Username;
                await msg.DeleteAsync();
                await proxyBuilder.SetLastProxyForUserAndChannel(proxy, botUser, c.Id);
                await SendMessage(lm, last.Content, proxy, username);
                await last.DeleteAsync();
            }
        }

        private async Task SendMessage(IMessage msg, string text, Proxy proxy, string overrideUsername = null)
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

            var username = $"{proxy.Name} [{overrideUsername ?? msg.Author.Username}]";

            if (msg.Attachments.Any())
            {
                var first = true;
                foreach (var a in msg.Attachments)
                {
                    // TODO: Single multipart request
                    var stream = await new HttpClient().GetStreamAsync(a.Url);
                    await hc.SendFileAsync(stream, a.Filename, first ? text : string.Empty,
                        embeds: msg.Embeds.Select(e => (Embed)e),
                        username: username,
                        avatarUrl: avatarUrl);
                    first = false;
                }
            }
            else
            {
                await hc.SendMessageAsync(text,
                    username: username,
                    embeds: msg.Embeds.Select(e => (Embed)e),
                    avatarUrl: avatarUrl);
            }
        }

        private static bool IsOwnProxy(IUser author, IUser user) => author.IsWebhook && author.Username.Contains($"[{user.Username}]");

        private MatchDescription MatchProxyContent(SocketMessage msg, BotUser botUser)
        {
            var resetMatch = regexCache.GetRegexForReset(botUser)?.Match(msg.Content);
            if (resetMatch?.Success ?? false)
            {
                return new MatchDescription(resetMatch.Groups["text"].Value, botUser);
            }

            var validProxies = botUser.Proxies
                .Where(p => p.IsForChannel(msg))
                .OrderBy(p => p.IsGlobal);
            foreach (var p in validProxies)
            {
                var proxyMatch = regexCache.GetRegexFor(p).Match(msg.Content);
                if (proxyMatch.Success)
                {
                    return new MatchDescription(proxyMatch.Groups["text"].Value, p);
                }
            }

            return new MatchDescription();
        }
    }
}