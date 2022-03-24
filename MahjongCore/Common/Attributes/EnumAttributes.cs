// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Reflection;

namespace MahjongCore.Common.Attributes
{
    public class EnumAttributes
    {
        // From http://joelforman.blogspot.com/2007/12/enums-and-custom-attributes.html
        public static R GetAttributeValue<T, R>(Enum @enum)
        {
            R attributeValue = default(R);
            if (@enum != null)
            {
                FieldInfo fi = @enum.GetType().GetField(@enum.ToString());
                if (fi != null)
                {
                    T[] attributes = fi.GetCustomAttributes(typeof(T), false) as T[];
                    if ((attributes != null) && (attributes.Length > 0))
                    {
                        IAttribute<R> attribute = attributes[0] as IAttribute<R>;
                        if (attribute != null)
                        {
                            attributeValue = attribute.Value;
                        }
                    }
                }
            }
            return attributeValue;
        }

        public static T GetAttributeValue<T>(Enum @enum, string tStr)
        {
            return GetAttributeValue<T>(@enum, Type.GetType(tStr));
        }

        public static T GetAttributeValue<T>(Enum @enum, Type t)
        {
            T attributeValue = default(T);
            if (@enum != null)
            {
                FieldInfo fi = @enum.GetType().GetField(@enum.ToString());
                if (fi != null)
                {
                    object[] attributes = fi.GetCustomAttributes(t, false) as object[];
                    if ((attributes != null) && (attributes.Length > 0))
                    {
                        IAttribute<T> attribute = attributes[0] as IAttribute<T>;
                        if (attribute != null)
                        {
                            attributeValue = attribute.Value;
                        }
                    }
                }
            }
            return attributeValue;
        }

        public static bool HasAttributeValue(Enum @enum, string tStr)
        {
            return HasAttributeValue(@enum, Type.GetType(tStr));
        }

        public static bool HasAttributeValue(Enum @enum, Type t)
        {
            bool fFound = false;
            if (@enum != null)
            {
                FieldInfo fi = @enum.GetType().GetField(@enum.ToString());
                if (fi != null)
                {
                    object[] attributes = fi.GetCustomAttributes(t, false);
                    fFound = ((attributes != null) && (attributes.Length > 0));
                }
            }
            return fFound;
        }
    }
}
