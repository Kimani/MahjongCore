// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class TextValue : Attribute, IAttribute<string>
    {
        public string Value { set; get; }

        public TextValue(string s)
        {
            Value = s;
        }
    }
}
