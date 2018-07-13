// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class IsYakuman : Attribute, IAttribute<bool>
    {
        public bool Value        { get; set; }
        public IsYakuman(bool v) { Value = v; }
    }
}
