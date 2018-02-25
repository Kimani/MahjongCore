// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldOpen : Attribute, IAttribute<bool>
    {
        public bool Value { set; get; }

        public MeldOpen(bool s)
        {
            Value = s;
        }
    }
}
