// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class AdvancePlayer : Attribute, IAttribute<bool>
    {
        public bool Value            { get; set; }
        public AdvancePlayer(bool v) { Value = v; }
    }
}
