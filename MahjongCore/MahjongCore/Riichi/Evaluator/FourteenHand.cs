// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    public class FourteenHand : CandidateHand
    {
        public override bool Evaluate(Hand baseHand, bool fRon)
        {
            RiichiGlobal.Assert(!fRon);

            ResetValues();
            Han = -1;
            Yaku.Add(Riichi.Yaku.ShiisuuPuuta);
            return true;
        }

        public override void ExpandAndInsert(List<CandidateHand> chBucket, Hand baseHand)
        {
            chBucket.Add(this);
        }
    }
}
