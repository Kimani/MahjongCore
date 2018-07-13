// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class NextRound : Attribute, IAttribute<Round>
    {
        public Round Value        { get; set; }
        public NextRound(Round v) { Value = v; }
    }
}
