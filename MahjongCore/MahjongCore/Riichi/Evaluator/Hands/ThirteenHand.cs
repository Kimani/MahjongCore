// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class ThirteenHand : CandidateHand
    {
        private Yaku _Type;

        internal ThirteenHand(Yaku type)                                                     { _Type = type; }
        internal override void ExpandAndInsert(List<CandidateHand> chBucket, IHand baseHand) { chBucket.Add(this); }

        internal override bool Evaluate(IHand hand, bool ron)
        {
            CommonHelpers.Check(((_Type == Riichi.Yaku.KokushiMusou) || (_Type == Riichi.Yaku.KokushiMusou)), "Expected kokushi or shiisan budou, found: " + _Type);
            CommonHelpers.Check(((_Type == Riichi.Yaku.KokushiMusou) || !ron),                                "Expected to be tsumo if this is shiisan budou.");

            ResetValues();
            Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.KokushiMusou,  Riichi.Yaku.ShiisanBudou,
                                                            Riichi.Yaku.Chiihou,       Riichi.Yaku.Renhou,       Riichi.Yaku.Tenhou });
            AdjustForPaaRenchan(hand, ron);
            AdjustForDoubleYakumanSetting(hand);
            UpdateYakumanCount();
            return true;
        }
    }
}
