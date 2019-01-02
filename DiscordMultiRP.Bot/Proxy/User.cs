using System.Collections.Generic;
using Discord;

namespace DiscordMultiRP.Bot.Proxy
{
    public class User
    {
        public User(ulong userId, List<ProxyDescription> proxies)
        {
            UserId = userId;
            Proxies = proxies;
        }

        public ulong UserId { get; }
        public List<ProxyDescription> Proxies { get; }

        public Dictionary<ITextChannel, ProxyDescription> LastProxies { get; } = new Dictionary<ITextChannel, ProxyDescription>();
    }
}