// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class MeldTypeAttribute : Attribute, IAttribute<MeldType>
    {
        public MeldType Value { set; get; }

        public MeldTypeAttribute(MeldType s)
        {
            Value = s;
        }
    }
}
