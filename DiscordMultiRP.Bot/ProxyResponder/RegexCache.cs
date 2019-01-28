using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscordMultiRP.Bot.Data;
using NLog;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public class RegexCache
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<(string, string), Regex> proxyCache = new Dictionary<(string, string), Regex>();
        private readonly Dictionary<string, Regex> resetCache = new Dictionary<string, Regex>();

        public Regex GetRegexFor(Proxy p)
        {
            if (!proxyCache.TryGetValue((p.Prefix, p.Suffix), out var r))
            {
                r = new Regex($@"^\s*{p.Prefix ?? ""}\s*(?<text>.*)\s*{p.Suffix ?? ""}\s*$", RegexOptions.IgnoreCase);
                proxyCache[(p.Prefix, p.Suffix)] = r;
            }

            return r;
        }

        public Regex GetRegexForReset(BotUser u)
        {
            if (string.IsNullOrWhiteSpace(u?.ResetCommand))
            {
                log.Debug($"User {u?.DiscordId} has no reset command.");
                return null;
            }

            if (!resetCache.TryGetValue(u.ResetCommand, out var r))
            {
                r = new Regex($@"^\s*{u.ResetCommand}\s*$", RegexOptions.IgnoreCase);
                resetCache[u.ResetCommand] = r;
            }

            return r;
        }
    }
}