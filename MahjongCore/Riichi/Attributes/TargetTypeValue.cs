// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class TargetTypeValue : Attribute, IAttribute<int>
    {
        public int Value              { get; set; }
        public TargetTypeValue(int v) { Value = v; }
    }
}
