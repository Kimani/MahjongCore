// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Evaluator;

namespace MahjongCore.Riichi.Attributes
{
    public class Evaluator : Attribute, IAttribute<YakuEvaluator>
    {
        public YakuEvaluator Value { get; set; }

        public Evaluator(YakuEvaluator f)
        {
            Value = f;
        }
    }
}
