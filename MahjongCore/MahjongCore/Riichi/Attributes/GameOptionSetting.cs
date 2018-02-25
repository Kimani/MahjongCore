// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class GameOptionSetting : Attribute, IAttribute<GameOption>
    {
        public GameOption Value { get; set; }

        public GameOptionSetting(GameOption v)
        {
            Value = v;
        }
    }
}
