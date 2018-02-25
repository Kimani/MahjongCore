// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class DescriptionName : Attribute, IAttribute<string>
    {
        public string Value { set; get; }

        public DescriptionName(string s)
        {
            Value = s;
        }
    }
}
