// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class WindPrevious : Attribute, IAttribute<Wind>
    {
        public Wind Value { set; get; }
        public WindPrevious(Wind value) { Value = value; }
    }
}
