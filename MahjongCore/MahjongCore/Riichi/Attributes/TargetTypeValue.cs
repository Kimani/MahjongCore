// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class TargetTypeValue : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public TargetTypeValue(int t)
        {
            Value = t;
        }
    }
}
