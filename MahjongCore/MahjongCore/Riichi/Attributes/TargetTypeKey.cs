// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi.Attributes
{
    public class TargetTypeKey : Attribute, IAttribute<CustomSettingType>
    {
        public CustomSettingType Value { set; get; }

        public TargetTypeKey(CustomSettingType t)
        {
            Value = t;
        }
    }
}
