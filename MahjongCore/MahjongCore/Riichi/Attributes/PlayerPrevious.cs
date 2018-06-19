// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class PlayerPrevious : Attribute, IAttribute<Player>
    {
        public Player Value                 { set; get; }
        public PlayerPrevious(Player value) { Value = value; }
    }
}
