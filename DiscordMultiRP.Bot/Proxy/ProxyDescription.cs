using System.Text.RegularExpressions;
using Discord.WebSocket;

namespace DiscordMultiRP.Bot.Proxy
{
    public class ProxyDescription
    {
        public ProxyDescription(string name, string regex, bool isGlobal, ulong channelId)
        {
            Name = name;
            IsGlobal = isGlobal;
            ChannelId = channelId;
            Regex = new Regex(regex, RegexOptions.IgnoreCase);
        }

        public string Name { get; }
        public Regex Regex { get; }
        public bool IsGlobal { get; }
        public ulong ChannelId { get; }

        public bool IsForChannel(SocketMessage msg) => IsGlobal || ChannelId == msg.Channel.Id;
    }
}