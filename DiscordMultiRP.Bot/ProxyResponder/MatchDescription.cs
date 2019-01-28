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

        public MatchDescription(string text, BotUser botUser) : this(true, text: text, botUser: botUser)
        {
        }

        private MatchDescription(bool isSuccess, string text = null, Proxy proxy = null, BotUser botUser = null)
        {
            IsSuccess = isSuccess;
            Proxy = proxy;
            BotUser = botUser;
            Text = text;
        }

        public bool IsSuccess { get; }
        public Proxy Proxy { get; }
        public BotUser BotUser { get; }
        public string Text { get; }

        public bool IsReset => BotUser != null;
    }
}