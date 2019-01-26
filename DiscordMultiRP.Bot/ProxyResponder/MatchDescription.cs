using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    internal class MatchDescription
    {
        public MatchDescription() : this(false)
        {
        }

        public MatchDescription(string text, Proxy proxy) : this(true, text: text, proxy: proxy)
        {
        }

        public MatchDescription(string text, User user) : this(true, text: text, user: user)
        {
        }

        private MatchDescription(bool isSuccess, string text = null, Proxy proxy = null, User user = null)
        {
            IsSuccess = isSuccess;
            Proxy = proxy;
            User = user;
            Text = text;
        }

        public bool IsSuccess { get; }
        public Proxy Proxy { get; }
        public User User { get; }
        public string Text { get; }

        public bool IsReset => User != null;
    }
}