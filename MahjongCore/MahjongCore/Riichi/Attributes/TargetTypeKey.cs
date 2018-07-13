// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class TargetTypeKey : Attribute, IAttribute<CustomSettingType>
    {
        public CustomSettingType Value            { get; set; }
        public TargetTypeKey(CustomSettingType v) { Value = v; }
    }
}
