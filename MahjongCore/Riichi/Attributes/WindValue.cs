// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class WindValue : Attribute, IAttribute<Wind>
    {
        public Wind Value { get; set; }
        public WindValue(Wind value) { Value = value; }
    }
}
