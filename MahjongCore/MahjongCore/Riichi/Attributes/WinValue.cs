// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class WinValue : Attribute, IAttribute<WinType>
    {
        public WinType Value       { set; get; }
        public WinValue(WinType v) { Value = v; }
    }
}
