// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class BitfieldMask : Attribute, IAttribute<uint>
    {
        public uint Value           { get; set; }
        public BitfieldMask(uint v) { Value = v; }
    }
}
