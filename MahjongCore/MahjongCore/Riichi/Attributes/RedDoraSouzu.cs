﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class RedDoraSouzu : Attribute, IAttribute<int>
    {
        public int Value { set; get; }

        public RedDoraSouzu(int s)
        {
            Value = s;
        }
    }
}
