using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscordMultiRP.Bot.Data;

namespace DiscordMultiRP.Bot.Proxy
{
    public class RegexCache
    {
        private readonly Dictionary<(string, string), Regex> regexCache = new Dictionary<(string, string), Regex>();

        public Regex GetRegexFor(Data.Proxy p)
        {
            if (!regexCache.TryGetValue((p.Prefix, p.Suffix), out var r))
            {
                r = new Regex($@"^\s*{p.Prefix ?? ""}\s*(?<text>.*)\s*{p.Suffix ?? ""}\s*$", RegexOptions.IgnoreCase);
                regexCache[(p.Prefix, p.Suffix)] = r;
            }
            return r;
        }

    }
}