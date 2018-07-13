// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class PlayerPrevious : Attribute, IAttribute<Player>
    {
        public Player Value             { get; set; }
        public PlayerPrevious(Player v) { Value = v; }
    }
}
