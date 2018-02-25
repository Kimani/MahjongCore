// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldCode : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public MeldCode(int s)
        {
            Value = s;
        }
    }
}