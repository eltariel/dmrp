using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public interface IProxyBuilder
    {
        Task<BotUser> GetBotUserById(ulong userId);

        Task<Proxy> GetLastProxyForUserAndChannel(BotUser botUser, ulong channelId);

        Task SetLastProxyForUserAndChannel(Proxy proxy, BotUser botUser, ulong channelId);

        Task ClearLastProxyForUserAndChannel(BotUser botUser, ulong channelId);
    }
}