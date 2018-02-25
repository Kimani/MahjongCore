// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class Place1Value : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public Place1Value(int t)
        {
            Value = t;
        }
    }
}
