using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.Proxy
{
    public interface IProxyBuilder
    {
        Task<User> GetUserById(ulong userId);

        Task<Data.Proxy> GetLastProxyForUserAndChannel(User user, ulong channelId);

        Task SetLastProxyForUserAndChannel(Data.Proxy proxy, User user, ulong channelId);
    }
}