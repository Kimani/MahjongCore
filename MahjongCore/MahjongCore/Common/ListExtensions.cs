// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Common
{
    // https://stackoverflow.com/questions/24855908/how-to-dequeue-element-from-a-list
    internal static class ListExtension
    {
        public static T Pop<T>(this List<T> list)
        {
            T ripItem = default(T);
            if (list.Count > 0)
            {
                int index = list.Count - 1;
                ripItem = list[index];
                list.RemoveAt(index);
            }
            return ripItem;
        }
    }
}
