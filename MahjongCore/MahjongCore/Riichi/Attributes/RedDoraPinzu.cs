// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class RedDoraPinzu : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public RedDoraPinzu(int s)
        {
            Value = s;
        }
    }
}