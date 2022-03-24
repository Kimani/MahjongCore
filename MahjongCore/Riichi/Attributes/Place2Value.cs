// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class Place2Value : Attribute, IAttribute<int>
    {
        public int Value          { get; set; }
        public Place2Value(int v) { Value = v; }
    }
}
