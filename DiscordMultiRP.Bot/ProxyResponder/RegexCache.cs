using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscordMultiRP.Bot.Data;
using NLog;

namespace DiscordMultiRP.Bot.ProxyResponder
{
    public class RegexCache
    {
        private static readonly Regex resetRegex = new Regex($@"^\s*!:\s*$", RegexOptions.IgnoreCase);

        private readonly Dictionary<(string, string), Regex> proxyCache = new Dictionary<(string, string), Regex>();

        public Regex GetRegexFor(Proxy p)
        {
            if (!proxyCache.TryGetValue((p.Prefix, p.Suffix), out var r))
            {
                r = new Regex($@"^\s*{p.Prefix ?? ""}\s*(?<text>.*)\s*{p.Suffix ?? ""}\s*$", RegexOptions.IgnoreCase);
                proxyCache[(p.Prefix, p.Suffix)] = r;
            }

            return r;
        }

        public Regex ResetRegex => resetRegex;
    }
}