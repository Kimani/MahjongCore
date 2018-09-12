// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class CandidateHand : ICandidateHand
    {
        // ICandidateHand
        public int         Dora    { get; internal set; } = 0;
        public int         UraDora { get; internal set; } = 0;
        public int         RedDora { get; internal set; } = 0;
        public int         Han     { get; internal set; } = 0;
        public int         Fu      { get; internal set; } = 0;
        public int         Yakuman { get; internal set; } = 0;
        public IList<Yaku> Yaku    { get { return YakuRaw; } }

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
            hand.Yakuman = Yakuman;
            hand.YakuRaw.AddRange(YakuRaw);
            return hand;
        }

        protected void ResetValues()
        {
            Dora = 0;
            UraDora = 0;
            RedDora = 0;
            Han = 0;
            Fu = 0;
            Yakuman = 0;
            YakuRaw.Clear();
        }

        protected void CleanNonYakumanYaku()
        {
            for (int i = Yaku.Count - 1; i >= 0; --i)
            {
                if (!Yaku[i].IsYakuman())
                {
                    Yaku.RemoveAt(i);
                }
            }
        }

        protected void AdjustForPaaRenchan(IHand hand, bool ron)
        {
            if (Han != 0)
            {
                int paaRenchanValue = Riichi.Yaku.PaaRenchan.Evaluate(hand, null, ron);
                if (paaRenchanValue != 0)
                {
                    Dora = 0;
                    UraDora = 0;
                    RedDora = 0;
                    Yakuman = Math.Abs(paaRenchanValue);
                    Yaku.Clear();
                    Yaku.Add(Riichi.Yaku.PaaRenchan);
                    Han = paaRenchanValue;
                    Fu = 0;
                }
            }
        }

        protected void AdjustForDoubleYakumanSetting(IHand hand)
        {
            if (!hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && (Han < -1))
            {
                Han = -1;
                Yakuman = 1;
            }
        }

        /**
         * Adds this CandidateHand to the given bucket. In the case that there are other permutations of the current hand,
         * it adds all permutations to the bucket. Also sets the winning tile flag on the appropriate tile.
         */
        internal virtual void ExpandAndInsert(List<CandidateHand> bucket, IHand hand) { }
        protected virtual bool Evaluate(IHand baseHand, bool ron)                     { return false; }

        protected int EvaluateYakuList(IHand hand, bool ron, Yaku[] yakuList)
        {
            int han = 0;
            foreach (Yaku y in yakuList)
            {
                han += EvaluateYaku(y.Evaluate(hand, this, ron), y);
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
