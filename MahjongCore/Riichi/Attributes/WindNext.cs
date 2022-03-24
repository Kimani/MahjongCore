// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class WindNext : Attribute, IAttribute<Wind>
    {
        public Wind Value           { get; set; }
        public WindNext(Wind value) { Value = value; }
    }
}
