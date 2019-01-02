using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DiscordMultiRP.Bot.Proxy
{
    public class AppSettingsProxyConfig : IProxyConfig
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IConfigurationSection cfg;

        private static Dictionary<ulong, User> users = new Dictionary<ulong, User>();

        public AppSettingsProxyConfig(IConfigurationSection cfg)
        {
            const string userIdKey = "user_id";
            this.cfg = cfg;

            foreach (var section in this.cfg.GetChildren())
            {
                log.Debug($"Found user section {section.Key}");
                try
                {
                    var userId = ulong.Parse(section[userIdKey]);
                    var proxies = section.GetChildren()
                        .Where(s => s.Key != userIdKey)
                        .Select(s => new ProxyDescription(
                            s["name"],
                            s["regex"],
                            bool.Parse(s["is_global"]),
                            ulong.Parse(s["channel"])));
                    var user = new User(userId, proxies.ToList());

                    log.Info($"User {section.Key} ({userId}): {user.Proxies.Count} registered proxies.");

                    users[userId] = user;
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Unable to parse proxies for user section {section.Key}.");
                }
            }
        }

        public User GetUserById(ulong userId)
        {
            return users.GetValueOrDefault(userId);
        }
    }
}