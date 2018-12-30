using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordMultiRP.Bot.Dice
{
    public class Roll
    {
        private readonly int count;
        private readonly bool isFudge;
        private readonly int? faces;
        private readonly int? add;
        private readonly int? explode;
        private readonly int? threshold;
        private readonly List<Die> dice = new List<Die>();

        private Roll(int count, bool isFudge, int? faces, int? add, int? explode, int? threshold)
        {
            this.count = count;
            this.isFudge = isFudge;
            this.add = add;
            if (!isFudge)
            {
                this.faces = faces;
                this.explode = explode.HasValue && (explode <= 1 || explode > faces) ? faces : explode;
                this.threshold = threshold.HasValue && (threshold <= 1 || threshold > faces - 1) ? faces -1 : threshold;
            }
            else
            {
                this.faces = null;
                this.explode = null;
                this.threshold = null;
            }
        }

        public string DescribeResult => string.Join(" ", dice.Select(r => r.Show()))
                                        + (threshold.HasValue
                                            ? $" = {dice.Count(d => d.IsSuccess)} successes"
                                            : (add.HasValue
                                                  ? $" + {add}"
                                                  : string.Empty)
                                              + $" = {Result}");

        private int Result => dice.Aggregate(add??0, (a, r) => a + r.Result);

        private void DoRoll()
        {
            if (dice.Any())
            {
                throw new InvalidOperationException("Can't redo roll.");
            }

            for (var i = 0; i < count; i++)
            {
                Die d;
                do
                {
                    d = Die.Create(isFudge, faces, explode, threshold);
                    dice.Add(d);
                    d.Roll();
                } while (d.IsExploded);
            }
        }

        public static Roll Dice(int count, bool isFudge, int? faces, int? add, int? explode, int? threshold)
        {
            var r = new Roll(count, isFudge, faces, add, explode, threshold);
            r.DoRoll();
            return r;
        }
    }
}