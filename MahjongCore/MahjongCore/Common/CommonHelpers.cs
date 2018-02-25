// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;

namespace MahjongCore.Common
{
    public static class CommonHelpers
    {
        public static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        public static void SafeCopyIntoValueList<T>(ICollection<T> targetList, ICollection<T> sourceList) where T : struct
        {
            targetList.Clear();
            if (sourceList != null)
            {
                foreach (T t in sourceList)
                {
                    targetList.Add(t);
                }
            }
        }

        public static void SafeCopyIntoList<T>(ICollection<T> targetList, ICollection<T> sourceList) where T : ICloneable
        {
            targetList.Clear();
            if (sourceList != null)
            {
                foreach (T t in sourceList)
                {
                    targetList.Add((T)t.Clone());
                }
            }
        }

        public static List<T> SafeCopy<T>(List<T> sourceList) where T : ICloneable
        {
            List<T> targetList = new List<T>();
            if (sourceList != null)
            {
                foreach (T t in sourceList)
                {
                    targetList.Add((T)t.Clone());
                }
            }
            return targetList;
        }

        public static List<T> SafeCopyByValue<T>(List<T> sourceList)
        {
            List<T> targetList = new List<T>();
            if (sourceList != null)
            {
                targetList.AddRange(sourceList);
            }
            return targetList;
        }

        public static List<T> SafeCopyIfNotNull<T>(List<T> sourceList) where T : ICloneable
        {
            List<T> targetList = null;
            if (sourceList != null)
            {
                targetList = new List<T>();
                foreach (T t in sourceList)
                {
                    targetList.Add((T)t.Clone());
                }
            }
            return targetList;
        }
    }
}
