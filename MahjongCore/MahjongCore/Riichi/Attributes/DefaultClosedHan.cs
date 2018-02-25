// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class DefaultClosedHan : Attribute, IAttribute<int>
    {
        public int Value { get; set; }

        public DefaultClosedHan(int h)
        {
            Value = h;
        }
    }
}
