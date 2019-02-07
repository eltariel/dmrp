using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    internal class MatchDescription
    {
        public static MatchDescription NoMatch { get; } = new NoMatch();
        public static MatchDescription ResetMatch { get; } = new ResetMatch();

        public static MatchDescription ProxyMatch(Proxy proxy, string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? (MatchDescription)new ClaimMessageMatch(proxy)
                : (MatchDescription)new ProxyMessageMatch(proxy, text);
        }
    }

    internal class ProxyMessageMatch : MatchDescription
    {
        public ProxyMessageMatch(Proxy proxy = null, string text = null)
        {
            Proxy = proxy;
            Text = text;
        }

        public Proxy Proxy { get; }
        public string Text { get; }
    }

    internal class ClaimMessageMatch : MatchDescription
    {
        public ClaimMessageMatch(Proxy proxy = null)
        {
            Proxy = proxy;
        }

        public Proxy Proxy { get; }
    }

    internal class ResetMatch : MatchDescription
    {
    }

    internal class NoMatch : MatchDescription { }
}