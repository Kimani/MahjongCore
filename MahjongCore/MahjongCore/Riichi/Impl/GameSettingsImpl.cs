// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class GameSettingsImpl : IGameSettings
    {
        // IGameSettings
        public void Reset()
        {
            CommonHelpers.Check(!Locked, "Settings are locked. Cannot edit!");
            _CustomSettings.Clear();
        }

        public T GetSetting<T>(GameOption option)
        {
            Type optionType = EnumAttributes.GetAttributeValue<OptionValueType, Type>(option);
            Global.Assert(optionType == typeof(T));
            if ((optionType == typeof(T)) && _CustomSettings.ContainsKey(option))
            {
                if (_CustomSettings.TryGetValue(option, out object settingObject))
                {
                    return (T)settingObject;
                }
            }
            return (T)EnumAttributes.GetAttributeValue<DefaultOptionValue, object>(option);
        }

        public void SetSetting(GameOption option, object value)
        {
            CommonHelpers.Check(!Locked, "Settings are locked. Cannot edit!");
            if (_CustomSettings.ContainsKey(option))
            {
                _CustomSettings.Remove(option);
            }
            _CustomSettings.Add(option, value);
        }

        public bool HasCustomSettings()
        {
            bool hasCustomSetting = false;
            foreach (KeyValuePair<GameOption, object> tuple in _CustomSettings)
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

        // ICloneable
        public object Clone() { return new GameSettingsImpl(_CustomSettings) { Locked = Locked }; }

        // GameSettingsImpl
        internal bool Locked { get; set; } = false;

        private Dictionary<GameOption, object> _CustomSettings = new Dictionary<GameOption, object>();

        internal GameSettingsImpl()                                               { _CustomSettings = new Dictionary<GameOption, object>(); }
        internal GameSettingsImpl(Dictionary<GameOption, object> settingsToClone) { _CustomSettings = new Dictionary<GameOption, object>(settingsToClone); }

        public void SetSettingField(uint bitfield, CustomBitfields field)
        {
            CommonHelpers.Check(!Locked, "Settings are locked. Cannot edit!");
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
    }
}
