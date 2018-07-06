// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    public class CandidateHand : ICandidateHand
    {
        public int        Dora = 0;
        public int        UraDora = 0;
        public int        RedDora = 0;
        public int        Han = 0;
        public int        Fu = 0;
        public int        YakumanCounter = 0; // The yakuman count we have (not counting kazoe yakuman), single, double etc. ex: only Suuankou Tanki Machi will make this 2.
        public List<Yaku> Yaku = new List<Yaku>();

        public virtual CandidateHand Clone()
        {
            CandidateHand hand = new CandidateHand();
            hand.Dora = Dora;
            hand.UraDora = UraDora;
            hand.RedDora = RedDora;
            hand.Han = Han;
            hand.Fu = Fu;
            hand.YakumanCounter = YakumanCounter;
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
        public virtual bool Evaluate(Hand baseHand, bool fRon)                       { return false; }

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
