using System;

namespace DiscordMultiRP.Bot.Dice
{
    public abstract class Die
    {
        public int Result { get; protected set; }
        public abstract bool IsSuccess { get; }
        public abstract bool IsExploded { get; }

        public abstract void Roll();

        public abstract string Show();

        public static Die Create(bool isFudge, int? faces, int? explode, int? threshold)
        {
            return isFudge ? (Die)new FudgeDie() : new StandardDie(faces??0, explode, threshold);
        }
    }

    public class StandardDie : Die
    {
        private readonly int faces;
        private readonly int? explode;
        private readonly int? threshold;

        public StandardDie(int faces, int? explode, int? threshold)
        {
            this.explode = explode;
            this.threshold = threshold;
            this.faces = faces > 0 ? faces : 6;
        }

        public override bool IsSuccess => threshold.HasValue && Result > threshold;

        public override bool IsExploded => explode.HasValue && Result >= explode;

        public override void Roll()
        {
            Result = new Random().Next(faces) + 1;
        }

        public override string Show()
        {
            var suc = IsSuccess ? "***" : "";
            var exp = IsExploded ? "!" : "";
            return $"[{suc}{Result}{exp}{suc}]";
        }
    }

    public class FudgeDie : Die
    {
        public override bool IsSuccess => false;
        public override bool IsExploded => false;

        public override void Roll()
        {
            Result = new Random().Next(3) - 1;
        }

        public override string Show()
        {
            return $"[{Result:+;-;0}]";
        }
    }
}