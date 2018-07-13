// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class CandidateHand : ICandidateHand
    {
        // ICandidateHand
        public int        Dora    { get; internal set; } = 0;
        public int        UraDora { get; internal set; } = 0;
        public int        RedDora { get; internal set; } = 0;
        public int        Han     { get; internal set; } = 0;
        public int        Fu      { get; internal set; } = 0;
        public int        Yakuman { get; internal set; } = 0;
        public IList<Yaku> Yaku   { get { return YakuRaw; } }

        // CandidateHand
        internal List<Yaku> YakuRaw = new List<Yaku>();

        internal virtual CandidateHand Clone()
        {
            CandidateHand hand = new CandidateHand();
            hand.Dora = Dora;
            hand.UraDora = UraDora;
            hand.RedDora = RedDora;
            hand.Han = Han;
            hand.Fu = Fu;
            hand.YakumanCount = YakumanCount;
            hand.Yaku.AddRange(Yaku);
            return hand;
        }

        protected void ResetValues()
        {
            Dora = 0;
            UraDora = 0;
            RedDora = 0;
            Han = 0;
            Fu = 0;
            YakumanCounter = 0;
            Yaku.Clear();
        }

        public void CleanNonYakumanYaku()
        {
            for (int i = Yaku.Count - 1; i >= 0; --i)
            {
                if (!Yaku[i].IsYakuman())
                {
                    Yaku.RemoveAt(i);
                }
            }
        }

        /**
         * Adds this CandidateHand to the given bucket. In the case that there are other permutations of the current hand,
         * it adds all permutations to the bucket. Also sets the winning tile flag on the appropriate tile.
         */
        public virtual void ExpandAndInsert(List<CandidateHand> chBucket, Hand hand) { /* Nothing */ }
        public virtual bool Evaluate(IHand baseHand, bool fRon)                       { return false; }

        protected int EvaluateYakuList(Hand hand, bool fRon, Yaku[] yakuList)
        {
            int han = 0;
            foreach (Yaku y in yakuList)
            {
                han += EvaluateYaku(y.Evaluate(hand, this, fRon), y);
            }
            return han;
        }

        private int EvaluateYaku(int HanDifference, Yaku yaku)
        {
            if (HanDifference != 0)
            {
                Yaku.Add(yaku);
            }
            return HanDifference;
        }
    }
}
