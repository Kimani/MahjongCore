// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class AdvancePlayer : Attribute, IAttribute<bool>
    {
        public bool Value { set; get; }

        public AdvancePlayer(bool v)
        {
            Value = v;
        }
    }
}
