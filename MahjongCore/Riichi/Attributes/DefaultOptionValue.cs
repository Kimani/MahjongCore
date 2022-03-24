// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class DefaultOptionValue : Attribute, IAttribute<object>
    {
        public object Value                 { get; set; }
        public DefaultOptionValue(object v) { Value = v; }
    }
}
