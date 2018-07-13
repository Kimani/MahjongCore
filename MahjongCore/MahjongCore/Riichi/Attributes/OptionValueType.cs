// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class OptionValueType : Attribute, IAttribute<Type>
    {
        public Type Value              { get; set; }
        public OptionValueType(Type v) { Value = v; }
    }
}
