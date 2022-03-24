// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class Place1Value : Attribute, IAttribute<int>
    {
        public int Value          { get; set; }
        public Place1Value(int v) { Value = v; }
    }
}
