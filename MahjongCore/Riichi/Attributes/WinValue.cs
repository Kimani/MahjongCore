// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class WinValue : Attribute, IAttribute<WinType>
    {
        public WinType Value       { get; set; }
        public WinValue(WinType v) { Value = v; }
    }
}
