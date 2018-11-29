// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class FourteenHand : CandidateHand
    {
        internal override void ExpandAndInsert(List<CandidateHand> bucket, IHand hand) { bucket.Add(this); }

        internal override bool Evaluate(IHand hand, bool ron)
        {
            CommonHelpers.Check(!ron, "ShiisuuPuuta expected to be tsumo, found ron!");

            ResetValues();
            Han = Riichi.Yaku.ShiisuuPuuta.GetHan(true, hand.Parent.Settings);
            Yaku.Add(Riichi.Yaku.ShiisuuPuuta);

            AdjustForPaaRenchan(hand, ron);
            AdjustForDoubleYakumanSetting(hand);
            UpdateYakumanCount();
            return true;
        }
    }
}
