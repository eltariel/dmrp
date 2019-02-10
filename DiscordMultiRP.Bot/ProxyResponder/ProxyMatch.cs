using Discord.WebSocket;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    internal class ProxyMatch
    {
        public static ProxyMatch NoMatch { get; } = new NoMatch();
        public static ProxyMatch ResetMatch { get; } = new ResetMatch();

        public static ProxyMatch GetMatch(Proxy proxy, SocketMessage msg, string text)
        {
            var shouldClaim = string.IsNullOrWhiteSpace(text) &&
                              msg.Attachments.Count == 0 &&
                              msg.Embeds.Count == 0;

            return shouldClaim
                ? (ProxyMatch)new ClaimMatch(proxy)
                : (ProxyMatch)new MessageMatch(proxy, text);
        }
    }

    internal class MessageMatch : ProxyMatch
    {
        public MessageMatch(Proxy proxy = null, string text = null)
        {
            Proxy = proxy;
            Text = text;
        }

        public Proxy Proxy { get; }
        public string Text { get; }
    }

    internal class ClaimMatch : ProxyMatch
    {
        public ClaimMatch(Proxy proxy = null)
        {
            Proxy = proxy;
        }

        public Proxy Proxy { get; }
    }

    internal class ResetMatch : ProxyMatch
    {
    }

    internal class NoMatch : ProxyMatch { }
}