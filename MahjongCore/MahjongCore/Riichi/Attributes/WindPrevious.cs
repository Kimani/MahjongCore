// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class WindPrevious : Attribute, IAttribute<Wind>
    {
        public Wind Value               { get; set; }
        public WindPrevious(Wind value) { Value = value; }
    }
}
