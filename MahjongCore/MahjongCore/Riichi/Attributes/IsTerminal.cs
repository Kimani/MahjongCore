// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class IsTerminal : Attribute, IAttribute<bool>
    {
        public bool Value         { get; set; }
        public IsTerminal(bool v) { Value = v; }
    }
}
