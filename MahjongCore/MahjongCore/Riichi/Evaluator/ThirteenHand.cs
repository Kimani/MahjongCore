// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Evaluator
{
    public class ThirteenHand : CandidateHand
    {
        Yaku Type; // Will be either KokushiMusou or ShiidsanBudou

        public ThirteenHand(Yaku type)
        {
            Type = type;
        }

        public override bool Evaluate(Hand hand, bool fRon)
        {
            Global.Assert(Type == Riichi.Yaku.KokushiMusou || !fRon); // We're either kokushi musou or if we're shiisanbudou, we're tsumo. Should have been enforced by Evaluate_Shiisanbudou.

            ResetValues();
            Han += EvaluateYakuList(hand, fRon, new Yaku[] { Riichi.Yaku.KokushiMusou,
                                                             Riichi.Yaku.ShiisanBudou,
                                                             Riichi.Yaku.Chiihou,
                                                             Riichi.Yaku.Renhou,
                                                             Riichi.Yaku.Tenhou });

            if (!hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && (Han < -1))
            {
                Han = -1;
            }
            return true;
        }

        public override void ExpandAndInsert(List<CandidateHand> chBucket, Hand baseHand)
        {
            chBucket.Add(this);
        }
    }
}
