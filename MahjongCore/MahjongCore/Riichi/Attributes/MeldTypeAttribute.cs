// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldTypeAttribute : Attribute, IAttribute<MeldType>
    {
        public MeldType Value                { get; set; }
        public MeldTypeAttribute(MeldType v) { Value = v; }
    }
}
