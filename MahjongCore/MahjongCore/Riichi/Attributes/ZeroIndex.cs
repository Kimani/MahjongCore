// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class ZeroIndex : Attribute, IAttribute<int>
    {
        public int Value { get; set; }

        public ZeroIndex(int i)
        {
            Value = i;
        }
    }
}
