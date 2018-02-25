// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldCalled : Attribute, IAttribute<bool>
    {
        public bool Value { set; get; }

        public MeldCalled(bool s)
        {
            Value = s;
        }
    }
}
