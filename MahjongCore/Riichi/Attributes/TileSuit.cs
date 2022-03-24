// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class TileSuit : Attribute, IAttribute<Suit>
    {
        public Suit Value       { get; set; }
        public TileSuit(Suit v) { Value = v; }
    }
}
