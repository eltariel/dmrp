using System;
using System.Linq;
using System.Threading.Tasks;
using DiscordMultiRP.Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot.ProxyResponder
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

        public async Task<BotUser> GetBotUserById(ulong userId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await db.BotUsers
                    .Include(u => u.Proxies).ThenInclude(p => p.Channels).ThenInclude(c => c.Channel)
                    .FirstOrDefaultAsync(u => u.DiscordId == userId);
                return dbUser;
            }
        }

        public async Task<Proxy> GetLastProxyForUserAndChannel(BotUser botUser, ulong channelId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await GetUser(db, botUser);
                var uc = await db.UserChannels
                    .Include(x => x.BotUser)
                    .Include(x => x.Channel)
                    .Include(x => x.LastProxy.Channels).ThenInclude(c => c.Channel)
                    .FirstOrDefaultAsync(x => dbUser != null && x.BotUser.Id == dbUser.Id && x.Channel.DiscordId == channelId);
                return uc?.LastProxy ?? dbUser?.LastGlobalProxy;
            }
        }

        public async Task SetLastProxyForUserAndChannel(Proxy proxy, BotUser botUser, ulong channelId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await GetUser(db, botUser);
                if (dbUser != null)
                {
                    var dbProxy = dbUser.Proxies.FirstOrDefault(p => p.Id == proxy.Id);
                    var uc = dbUser.Channels.FirstOrDefault(c => c.Channel.DiscordId == channelId);

                    if (proxy.IsGlobal && dbUser.IsAllowedGlobal)
                    {
                        dbUser.LastGlobalProxy = dbProxy;
                    }
                    else
                    {
                        if (uc == null)
                        {
                            uc = new UserChannel
                            {
                                Channel = await db.Channels.FirstOrDefaultAsync(c => c.DiscordId == channelId) ??
                                          new Channel {DiscordId = channelId},
                                BotUser = dbUser,
                            };
                            dbUser.Channels.Add(uc);
                        }

                        uc.LastProxy = dbProxy;
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        public async Task ClearLastProxyForUserAndChannel(BotUser botUser, ulong channelId)
        {
            using (var db = GetDataContext())
            {
                var dbUser = await GetUser(db, botUser);
                if (dbUser != null)
                {
                    var uc = dbUser.Channels.FirstOrDefault(c => c.Channel.DiscordId == channelId);
                    if (uc?.LastProxy != null)
                    {
                        if (dbUser.LastGlobalProxy == uc.LastProxy)
                        {
                            dbUser.LastGlobalProxy = null;
                        }

                        uc.LastProxy = null;
                    }
                    else
                    {
                        dbUser.LastGlobalProxy = null;
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        private static async Task<BotUser> GetUser(ProxyDataContext db, BotUser botUser)
        {
            var dbUser = await db.BotUsers
                .Include(u => u.Channels).ThenInclude(c => c.Channel)
                .Include(u => u.Proxies)
                .FirstOrDefaultAsync(u => u.Id == botUser.Id);
            return dbUser;
        }

        private ProxyDataContext GetDataContext()
        {
            return new ProxyDataContext(dbOptions);
        }
    }
}