// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class WindNext : Attribute, IAttribute<Wind>
    {
        public Wind Value { set; get; }
        public WindNext(Wind value) { Value = value; }
    }
}
