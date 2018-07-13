// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class TextValue : Attribute, IAttribute<string>
    {
        public string Value        { get; set; }
        public TextValue(string v) { Value = v; }
    }
}
