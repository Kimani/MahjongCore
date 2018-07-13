// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class GameOptionSetting : Attribute, IAttribute<GameOption>
    {
        public GameOption Value                { get; set; }
        public GameOptionSetting(GameOption v) { Value = v; }
    }
}
