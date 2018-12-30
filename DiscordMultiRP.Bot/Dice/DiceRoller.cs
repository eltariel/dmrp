using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using NLog;

namespace DiscordMultiRP.Bot.Dice
{
    public class DiceRoller
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Regex rollRegex = new Regex(
            @"
                    (?<count>\d+)[dD]       # Number of dice, and D
                    (
                        (?<faces>\d+)           # Number of faces per dice OR
                            (!(?<bang>\d*))?        # Optional !n - explode dice greater than n, or number of faces if n not specified
                            (>(?<threshold>\d*))?   # Optional >n - count dice greater than n, or number of faces if n not specified. Use count for result.
                        |(?<fudge>[fF])         # Fudge dice - + / 0 / -, sum the result.
                    )
                    (\+(?<add>\d+))?        # Optional +n - add n to final result.",
            RegexOptions.IgnorePatternWhitespace);

        public async Task HandleRolls(ISocketMessageChannel channel, string cmd, ulong replyTo)
        {
            foreach (Match m in rollRegex.Matches(cmd))
            {
                var rollCmd = m.Groups[0].Value;
                log.Info($"Found a dice roll: {rollCmd}");

                var count = int.Parse(m.Groups["count"].Value);
                var isFudge = m.Groups["fudge"].Success;
                var faces = GetNullableFromRegexGroup(m.Groups["faces"]);
                var add = GetNullableFromRegexGroup(m.Groups["add"]);
                var explode = GetNullableFromRegexGroup(m.Groups["bang"]);
                var threshold = GetNullableFromRegexGroup(m.Groups["threshold"]);

                if (IsValidRoll(count, isFudge, faces))
                {
                    var roll = Roll.Dice(count, isFudge, faces, add, explode, threshold);

                    await channel.SendMessageAsync($"<@{replyTo}> {rollCmd}: {roll.DescribeResult}");
                }
                else
                {
                    await channel.SendMessageAsync($"<@{replyTo}> {rollCmd}: You idiot.");
                }
            }
        }

        private static bool IsValidRoll(int count, bool isFudge, int? faces)
        {
            return 0 < count && count <= 300
                             && (faces ?? 2) > 1;
        }

        private static int? GetNullableFromRegexGroup(Group g)
        {
            return g.Success ? int.TryParse(g.Value, out var x) ? x : 0 : (int?)null;
        }
    }
}