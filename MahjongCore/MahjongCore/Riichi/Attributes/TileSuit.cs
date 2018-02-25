// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class TileSuit : Attribute, IAttribute<Suit>
    {
        public Suit Value { set; get; }

        public TileSuit(Suit s)
        {
            Value = s;
        }
    }
}
