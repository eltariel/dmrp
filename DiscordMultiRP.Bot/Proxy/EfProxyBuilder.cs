using System;
using System.Linq;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot.Proxy
{
    public class EfProxyBuilder : IProxyBuilder
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly DbContextOptions dbOptions;

        public EfProxyBuilder(IConfiguration cfg)
        {
            try
            {
                dbOptions = new DbContextOptionsBuilder()
                    .UseSqlServer(cfg.GetConnectionString("ProxyDataContext"))
                    .Options;

                using (var db = GetDataContext())
                {
                    db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                log.Fatal(ex, "Can't connect to DB for proxy!");
            }
        }

        public async Task<User> GetUserById(ulong userId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await db.Users
                    .Include(u => u.Proxies).ThenInclude(p => p.Channel)
                    .FirstOrDefaultAsync(u => u.DiscordId == userId);
                return dbUser;
            }
        }

        public async Task<Data.Proxy> GetLastProxyForUserAndChannel(User user, ulong channelId)
        {
            using (var db = GetDataContext())
            {
                var uc = await db.UserChannels
                    .Include(x => x.User)
                    .Include(x => x.Channel)
                    .Include(x => x.LastProxy)
                    .FirstOrDefaultAsync(x => x.User.Id == user.Id && x.Channel.DiscordId == channelId);
                return uc?.LastProxy;
            }
        }

        public async Task SetLastProxyForUserAndChannel(Data.Proxy proxy, User user, ulong channelId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await db.Users
                    .Include(u => u.Channels).ThenInclude(c => c.Channel)
                    .Include(u => u.Proxies)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);
                if (dbUser != null)
                {
                    var uc = dbUser.Channels.FirstOrDefault(c => c.Channel.DiscordId == channelId);
                    if (uc == null)
                    {
                        uc = new UserChannel
                        {
                            Channel = await db.Channels.FirstOrDefaultAsync(c => c.DiscordId == channelId) ??
                                      new Channel {DiscordId = channelId},
                            User = dbUser,
                        };
                        dbUser.Channels.Add(uc);
                    }

                    uc.LastProxy = dbUser.Proxies.FirstOrDefault(p => p.Id == proxy.Id);
                }

                await db.SaveChangesAsync();
            }
        }

        private ProxyDataContext GetDataContext()
        {
            return new ProxyDataContext(dbOptions);
        }
    }
}