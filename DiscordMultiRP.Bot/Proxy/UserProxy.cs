using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using NLog;

namespace DiscordMultiRP.Bot.Proxy
{
    public class UserProxy
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly DiscordSocketClient discord;
        private readonly IProxyConfig config;

        public UserProxy(DiscordSocketClient discord, IProxyConfig config)
        {
            this.discord = discord;
            this.config = config;
        }

        public async Task HandleMessage(SocketMessage msg)
        {
            var user = LookupUser(msg.Author);
            if (user != null && msg.Channel is ITextChannel c)
            {
                log.Debug($"Found {user.Proxies.Count} registered proxies for user {msg.Author}");
                var (p, text) = MatchProxyContent(msg, user.Proxies);
                if (p == null)
                {
                    log.Debug($"Using last proxy for {msg.Author} in {c.Guild.Name}:{c.Name}");
                    p = user.LastProxies[c];
                    text = msg.Content;
                }

                if (p != null)
                {
                    try
                    {
                        log.Debug($"Proxying message in channel {c.Guild.Name}:{c.Name} ({c.Id})" +
                                  $" for user {msg.Author} - " +
                                  $"proxy name is {p.Name} ({(p.IsGlobal ? "global" : "channel")})");
                        await msg.DeleteAsync();
                        await SendMessage(msg, p, text);
                        user.LastProxies[c] = p;
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex, $"Can't proxy message for {p.Name}.");
                    }
                }
            }
        }

        private async Task SendMessage(SocketMessage msg, ProxyDescription proxy,
            string text)
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

        private (ProxyDescription proxy, string text) MatchProxyContent(SocketMessage msg, List<ProxyDescription> proxyDescriptions)
        {
            foreach (var p in proxyDescriptions.Where(p => p.IsForChannel(msg)))
            {
                var m = p.Regex.Match(msg.Content);
                if (m.Success)
                {
                    return (proxy: p, text: m.Groups["text"].Value);
                }
            }

            return (null, null);
        }

        private User LookupUser(SocketUser msgAuthor)
        {
            return config.GetUserById(msgAuthor.Id);
        }
    }
}