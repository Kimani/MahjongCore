// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class Place3Value : Attribute, IAttribute<int>
    {
        public int Value          { get; set; }
        public Place3Value(int v) { Value = v; }
    }
}
