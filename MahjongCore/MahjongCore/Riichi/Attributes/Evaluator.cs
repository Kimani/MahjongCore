// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Evaluator;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class Evaluator : Attribute, IAttribute<YakuEvaluator>
    {
        public YakuEvaluator Value        { get; set; }
        public Evaluator(YakuEvaluator v) { Value = v; }
    }
}
