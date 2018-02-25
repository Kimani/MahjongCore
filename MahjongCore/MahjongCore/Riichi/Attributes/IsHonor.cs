﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class IsHonor : Attribute, IAttribute<bool>
    {
        public bool Value { get; set; }

        public IsHonor(bool t)
        {
            Value = t;
        }
    }
}
