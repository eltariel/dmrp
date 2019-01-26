using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public interface IProxyBuilder
    {
        Task<User> GetUserById(ulong userId);

        Task<Proxy> GetLastProxyForUserAndChannel(User user, ulong channelId);

        Task SetLastProxyForUserAndChannel(Proxy proxy, User user, ulong channelId);

        Task ClearLastProxyForUserAndChannel(User user, ulong channelId);
    }
}