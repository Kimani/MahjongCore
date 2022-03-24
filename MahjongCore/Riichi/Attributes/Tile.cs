// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class Tile : Attribute, IAttribute<TileType>
    {
        public TileType Value   { get; set; }
        public Tile(TileType v) { Value = v; }
    }
}
