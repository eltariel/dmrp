using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public interface IProxyBuilder
    {
        Task<BotUser> GetBotUserById(ulong userId);

        Task<Proxy> GetCurrentProxy(BotUser botUser, ulong channelId);

        Task SetCurrentProxy(Proxy proxy, BotUser botUser, ulong channelId);

        Task ClearCurrentProxy(BotUser botUser, ulong channelId);
    }
}