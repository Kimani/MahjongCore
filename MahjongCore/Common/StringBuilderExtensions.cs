// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Text;

namespace MahjongCore.Common
{
    internal static class StringBuilderExtensionMethods
    {
        public static void AppendWithSpace(this StringBuilder sb, string str)
        {
            sb.Append(str);
            sb.Append(' ');
        }
    }
}
