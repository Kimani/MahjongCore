// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;

namespace MahjongCore.Common.Attributes
{
    public class EnumHelper
    {
        public static bool TryGetEnumByCode<T, R>(string codeString, out T enumResult) where T : struct, IComparable
        {
            int code;
            bool found = int.TryParse(codeString, out code);
            if (found)
            {
                T? gaResult = GetEnumValueFromAttribute<T, R, int>(code);
                enumResult = (gaResult != null) ? gaResult.Value : default(T);
            }
            else
            {
                enumResult = default(T);
            }
            return found;
        }

        public static bool TryGetEnumByString<T>(string value, out T result) where T : struct
        {
            T[] tValArray = (T[])Enum.GetValues(typeof(T));
            for (int i = 0; i < tValArray.Length; ++i)
            {
                if (tValArray[i].ToString().Equals(value))
                {
                    result = tValArray[i];
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        // Uses http://stackoverflow.com/questions/2230657/help-with-c-sharp-generics-error-the-type-t-must-be-a-non-nullable-value-ty to get T contraints.
        public static T? GetEnumValueFromAttribute<T, R, N>(N attributeMatchValue) where T : struct, IComparable
        {
            T? foundValue = null;
            if (attributeMatchValue != null)
            {
                T[] tValArray = (T[])Enum.GetValues(typeof(T));
                Array enumValArray = Enum.GetValues(typeof(T));

                for (int i = 0; i < enumValArray.Length; ++i)
                {
                    Enum enumVal = (Enum)enumValArray.GetValue(i);
                    if (EnumAttributes.HasAttributeValue(enumVal, typeof(R)))
                    {
                        N enumValMatchValue = EnumAttributes.GetAttributeValue<R, N>(enumVal);
                        if (enumValMatchValue.Equals(attributeMatchValue))
                        {
                            foundValue = new T?(tValArray[i]);
                        }
                    }
                }
            }
            return foundValue;
        }
    }
}
