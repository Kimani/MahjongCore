// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class Place3Value : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public Place3Value(int t)
        {
            Value = t;
        }
    }
}
