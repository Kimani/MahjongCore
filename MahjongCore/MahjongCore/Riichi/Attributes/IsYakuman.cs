// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class IsYakuman : Attribute, IAttribute<bool>
    {
        public bool Value { get; set; }

        public IsYakuman(bool t)
        {
            Value = t;
        }
    }
}
