// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class PointLoss : Attribute, IAttribute<int>
    {
        public int Value { get; set; }

        public PointLoss(int v)
        {
            Value = v;
        }
    }
}