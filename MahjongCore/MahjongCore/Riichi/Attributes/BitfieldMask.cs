// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class BitfieldMask : Attribute, IAttribute<uint>
    {
        public uint Value { set; get; }

        public BitfieldMask(uint s)
        {
            Value = s;
        }
    }
}
