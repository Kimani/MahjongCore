// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class DefaultOptionValue : Attribute, IAttribute<object>
    {
        public object Value { set; get; }

        public DefaultOptionValue(object o)
        {
            Value = o;
        }
    }
}
