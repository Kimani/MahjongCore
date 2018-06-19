// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class Tile : Attribute, IAttribute<TileType>
    {
        public TileType Value       { set; get; }
        public Tile(TileType value) { Value = value; }
    }
}
