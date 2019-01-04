using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.Proxy
{
    public class RegexCache
    {
        private readonly Dictionary<string, Regex> regexCache = new Dictionary<string, Regex>();

        public Regex GetRegexFor(Data.Proxy p)
        {
            if (!regexCache.TryGetValue(p.Regex, out var r))
            {
                r = new Regex(p.Regex);
                regexCache[p.Regex] = r;
            }
            return r;
        }

    }
}