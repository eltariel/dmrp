using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;
using NLog;

namespace DiscordMultiRP.Bot.Proxy
{
    public class UserProxy
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly DiscordSocketClient discord;
        private readonly IProxyBuilder proxyBuilder;
        private readonly RegexCache regexCache;

        public UserProxy(DiscordSocketClient discord, IProxyBuilder proxyBuilder)
        {
            this.discord = discord;
            this.proxyBuilder = proxyBuilder;
            regexCache = new RegexCache();
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
                        await SendMessage(msg, text, p);
                        await proxyBuilder.SetLastProxyForUserAndChannel(p, user, c.Id);
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

            var hooks = await c.GetWebhooksAsync();
            var wh = hooks.FirstOrDefault(h => h.Creator.Id == discord.CurrentUser.Id) ??
                       await c.CreateWebhookAsync("DiscordMultiRP Proxy Hook");

            var hc = new DiscordWebhookClient(wh);
            await hc.SendMessageAsync(text,
                username: $"{proxy.Name} [{msg.Author.Username}]",
                embeds: msg.Embeds,
                avatarUrl: msg.Author.GetAvatarUrl());
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