// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class NextState : Attribute, IAttribute<PlayState>
    {
        public PlayState Value { get; set; }

        public NextState(PlayState s)
        {
            Value = s;
        }
    }
}
