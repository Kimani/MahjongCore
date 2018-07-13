// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldOpen : Attribute, IAttribute<bool>
    {
        public bool Value       { get; set; }
        public MeldOpen(bool v) { Value = v; }
    }
}
