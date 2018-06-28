// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class IsReach : Attribute, IAttribute<bool>
    {
        public bool Value { set; get; }
        public IsReach(bool value) { Value = value; }
    }
}
