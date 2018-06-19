﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class GameSettingsImpl : IGameSettings
    {
        private Dictionary<GameOption, object> CustomSettings = new Dictionary<GameOption, object>();

        public T GetSetting<T>(GameOption option)
        {
            Type optionType = EnumAttributes.GetAttributeValue<OptionValueType, Type>(option);
            Global.Assert(optionType == typeof(T));
            if ((optionType == typeof(T)) && CustomSettings.ContainsKey(option))
            {
                object settingObject;
                if (CustomSettings.TryGetValue(option, out settingObject))
                {
                    return (T)settingObject;
                }
            }
            return (T)EnumAttributes.GetAttributeValue<DefaultOptionValue, object>(option);
        }

        public void SetSetting(GameOption option, object value)
        {
            if (CustomSettings.ContainsKey(option))
            {
                CustomSettings.Remove(option);
            }
            CustomSettings.Add(option, value);
        }

        public void SetSettingField(uint bitfield, CustomBitfields field)
        {
            CustomSettingType fieldType = field.GetCustomSettingType();
            int fieldValue = field.GetTargetTypeValue();

            foreach (GameOption go in Enum.GetValues(typeof(GameOption)))
            {
                if (((fieldType == CustomSettingType.Rule) && EnumAttributes.HasAttributeValue(go, typeof(RuleValue))) ||
                    ((fieldType == CustomSettingType.Yaku) && EnumAttributes.HasAttributeValue(go, typeof(YakuValue))))
                {
                    int goValue = (fieldType == CustomSettingType.Rule) ? EnumAttributes.GetAttributeValue<RuleValue, int>(go) :
                                                                          EnumAttributes.GetAttributeValue<YakuValue, int>(go);
                    if (fieldValue == goValue)
                    {
                        uint bitfieldMask = EnumAttributes.GetAttributeValue<BitfieldMask, uint>(go);
                        SetSetting(go, ((bitfieldMask & bitfield) == bitfieldMask));
                    }
                }
            }
        }

        public uint GetSettingField(CustomBitfields field)
        {
            CustomSettingType fieldType = field.GetCustomSettingType();
            int fieldValue = field.GetTargetTypeValue();

            uint bitfield = 0;
            foreach (GameOption go in Enum.GetValues(typeof(GameOption)))
            {
                // RuleValue / YakuValue / BitfieldMask
                if (((fieldType == CustomSettingType.Rule) && EnumAttributes.HasAttributeValue(go, typeof(RuleValue))) ||
                    ((fieldType == CustomSettingType.Yaku) && EnumAttributes.HasAttributeValue(go, typeof(YakuValue))))
                {
                    int goValue = (fieldType == CustomSettingType.Rule) ? EnumAttributes.GetAttributeValue<RuleValue, int>(go) :
                                                                          EnumAttributes.GetAttributeValue<YakuValue, int>(go);
                    if (fieldValue == goValue)
                    {
                        bitfield |= (uint)GetSetting<int>(go);
                    }
                }
            }
            return bitfield;
        }

        public bool HasCustomSettings()
        {
            bool hasCustomSetting = false;
            foreach (KeyValuePair<GameOption, object> tuple in CustomSettings)
            {
                object defaultValue = EnumAttributes.GetAttributeValue<DefaultOptionValue, object>(tuple.Key);
                if (!defaultValue.Equals(tuple.Value))
                {
                    hasCustomSetting = true;
                    break;
                }
            }
            return hasCustomSetting;
        }
    }
}
