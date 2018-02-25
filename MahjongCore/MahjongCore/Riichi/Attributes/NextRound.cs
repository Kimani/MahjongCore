// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class NextRound : Attribute, IAttribute<Round>
    {
        public Round Value { set; get; }

        public NextRound(Round s)
        {
            Value = s;
        }
    }
}
