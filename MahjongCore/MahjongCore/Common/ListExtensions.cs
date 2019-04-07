// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Common
{
    // https://stackoverflow.com/questions/24855908/how-to-dequeue-element-from-a-list
    internal static class ListExtension
    {
        public static T Peek<T>(this IList<T> list) where T : class
        {
            T peekItem = null;
            if (list.Count > 0)
            {
                int index = list.Count - 1;
                peekItem = list[index];
            }
            return peekItem;
        }

        public static T Pop<T>(this IList<T> list)
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

        public static void AddUnique<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }
    }
}
