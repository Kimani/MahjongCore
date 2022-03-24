// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class NextState : Attribute, IAttribute<PlayState>
    {
        public PlayState Value        { get; set; }
        public NextState(PlayState v) { Value = v; }
    }
}
