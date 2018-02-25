// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class OptionValueType : Attribute, IAttribute<Type>
    {
        public Type Value { set; get; }

        public OptionValueType(Type t)
        {
            Value = t;
        }
    }
}
