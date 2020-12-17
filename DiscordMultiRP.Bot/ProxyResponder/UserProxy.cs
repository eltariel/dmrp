using System;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly DiscordSocketClient discord;
        private readonly IProxyBuilder proxyBuilder;
        private readonly RegexCache regexCache;
        private readonly WebhookCache webhookCache;
        private readonly ProxyHelper helper;

        public UserProxy(DiscordSocketClient discord, IProxyBuilder proxyBuilder, IConfiguration cfg)
        {
            this.discord = discord;
            this.proxyBuilder = proxyBuilder;
            regexCache = new RegexCache();
            webhookCache = new WebhookCache();

            helper = new ProxyHelper(discord, cfg);
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
                        case ClaimMatch c:
                            await ClaimLastMessage(msg, c.Proxy, botUser);
                            break;
                        case MessageMatch p:
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

        private async Task ResendAsProxy(SocketMessage msg, MessageMatch p, ITextChannel c, BotUser botUser)
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
            var chm = (await msg.Channel.GetMessagesAsync().ToListAsync()).SelectMany(x => x).ToList();
            var last = chm.FirstOrDefault(m => m.Id != msg.Id &&
                                               m.Author.Id == msg.Author.Id ||
                                               IsOwnProxy(m.Author, msg.Author));

            if (last is IMessage lm)
            {
                await msg.DeleteAsync();
                await proxyBuilder.SetCurrentProxy(proxy, botUser, c.Id);
                await SendMessage(lm, last.Content, proxy);
                await last.DeleteAsync();
            }
        }

        private async Task SendMessage(IMessage msg, string text, Proxy proxy)
        {
            if (!(msg.Channel is ITextChannel c))
            {
                log.Error($"Channel {msg.Channel} is not a text channel.");
                return;
            }

            var webhook = await webhookCache.GetWebhook(c, discord);
            await helper.SendMessage(webhook, proxy, text, msg.Attachments);
        }

        private static bool IsOwnProxy(IUser author, IUser user) => author.IsWebhook && author.Username.Contains($"[{user.Username}]");

        private async Task<ProxyMatch> FindTarget(SocketMessage msg, BotUser botUser, ITextChannel channel)
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

        private async Task<ProxyMatch> FindCurrentProxy(SocketMessage msg, BotUser botUser, ITextChannel channel)
        {
            log.Debug($"Looking for current proxy for {msg.Author} in {channel.Guild.Name}:{channel.Name}");
            var proxy = await proxyBuilder.GetCurrentProxy(botUser, channel.Id);

            return proxy != null
                ? ProxyMatch.GetMatch(proxy, msg, msg.Content)
                : ProxyMatch.NoMatch;
        }

        private ProxyMatch FindProxyForMessage(SocketMessage msg, BotUser botUser)
        {
            var md = ProxyMatch.NoMatch;

            var validProxies = botUser.Proxies
                .Where(p => p.IsForChannel(msg))
                .OrderBy(p => p.IsGlobal);
            foreach (var p in validProxies)
            {
                var proxyMatch = regexCache.GetRegexFor(p).Match(msg.Content);
                if (proxyMatch.Success)
                {
                    md = ProxyMatch.GetMatch(p, msg, proxyMatch.Groups["text"].Value);
                }
            }

            return md;
        }

        private ProxyMatch MatchReset(IMessage msg)
        {
            var resetMatch = regexCache.ResetRegex.Match(msg.Content);
            return resetMatch.Success
                ? ProxyMatch.ResetMatch
                : ProxyMatch.NoMatch;
        }
    }
}