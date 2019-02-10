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
            if (botUser != null && msg.Channel is ITextChannel channel)
            {
                log.Debug($"Found {botUser.Proxies.Count} registered proxies for user {msg.Author}");
                var m = await FindTarget(msg, botUser, channel);

                try
                {
                    switch (m)
                    {
                        case ResetMatch _:
                            await ClearProxy(msg, channel, botUser);
                            break;
                        case ClaimMessageMatch c:
                            await ClaimLastMessage(msg, c.Proxy, botUser);
                            break;
                        case ProxyMessageMatch p:
                            await ResendAsProxy(msg, p, channel, botUser);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    log.Warn(ex, $"Can't handle message for {msg.Author}.");
                }
            }
        }

        private async Task ClearProxy(SocketMessage msg, ITextChannel c, BotUser botUser)
        {
            log.Debug($"Reset message for {msg.Author} in {c.Guild.Name}:{c.Name}");
            await proxyBuilder.ClearCurrentProxy(botUser, c.Id);
            await msg.DeleteAsync();
        }

        private async Task ResendAsProxy(SocketMessage msg, ProxyMessageMatch p, ITextChannel c, BotUser botUser)
        {
            log.Debug(
                $"{(p.Proxy.IsGlobal ? "Global" : "Channel")} proxy message for {p.Proxy.Name} [{msg.Author}] "
                + $"in channel {c.Guild.Name}:{c.Name} ({c.Id})");

            await proxyBuilder.SetCurrentProxy(p.Proxy, botUser, c.Id);
            await SendMessage(msg, p.Text, p.Proxy);
            await msg.DeleteAsync();
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
                await proxyBuilder.SetCurrentProxy(proxy, botUser, c.Id);
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
                        username: username,
                        avatarUrl: avatarUrl);
                    first = false;
                }
            }
            else
            {
                await hc.SendMessageAsync(text,
                    username: username,
                    avatarUrl: avatarUrl);
            }
        }

        private static bool IsOwnProxy(IUser author, IUser user) => author.IsWebhook && author.Username.Contains($"[{user.Username}]");

        private async Task<MatchDescription> FindTarget(SocketMessage msg, BotUser botUser, ITextChannel channel)
        {
            var md = MatchReset(msg);

            if (md is NoMatch)
            {
                md = FindProxyForMessage(msg, botUser);
            }

            if (md is NoMatch)
            {
                md = await FindCurrentProxy(msg, botUser, channel);
            }


            return md;
        }

        private async Task<MatchDescription> FindCurrentProxy(SocketMessage msg, BotUser botUser, ITextChannel channel)
        {
            log.Debug($"Looking for current proxy for {msg.Author} in {channel.Guild.Name}:{channel.Name}");
            var proxy = await proxyBuilder.GetCurrentProxy(botUser, channel.Id);

            return proxy != null
                ? MatchDescription.ProxyMatch(proxy, msg, msg.Content)
                : MatchDescription.NoMatch;
        }

        private MatchDescription FindProxyForMessage(SocketMessage msg, BotUser botUser)
        {
            var md = MatchDescription.NoMatch;

            var validProxies = botUser.Proxies
                .Where(p => p.IsForChannel(msg))
                .OrderBy(p => p.IsGlobal);
            foreach (var p in validProxies)
            {
                var proxyMatch = regexCache.GetRegexFor(p).Match(msg.Content);
                if (proxyMatch.Success)
                {
                    md = MatchDescription.ProxyMatch(p, msg, proxyMatch.Groups["text"].Value);
                }
            }

            return md;
        }

        private MatchDescription MatchReset(IMessage msg)
        {
            var resetMatch = regexCache.ResetRegex.Match(msg.Content);
            return resetMatch.Success
                ? MatchDescription.ResetMatch
                : MatchDescription.NoMatch;
        }
    }
}