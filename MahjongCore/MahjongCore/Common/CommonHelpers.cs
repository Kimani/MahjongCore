// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Xml;

namespace MahjongCore.Common
{
    public enum IterateCount
    {
        All,
        One
    }

    public static class CommonHelpers
    {
        public static bool IsFlagSet(this uint value, uint flag) { return (flag != 0) && ((value & flag) == flag); }
        public static void Check(bool condition, string message) { if (!condition) { throw new Exception(message); } }

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

        public static IReadOnlyList<T> SafeCopy<T>(IReadOnlyList<T> sourceList) where T : ICloneable
        {
            List<T> targetList = new List<T>();
            if (sourceList != null)
            {
                foreach (T t in sourceList)
                {
                    targetList.Add((T)t.Clone());
                }
            }
            return targetList.AsReadOnly();
        }

        public static List<T> SafeCopyByValue<T>(List<T> sourceList)
        {
            List<T> targetList = new List<T>();
            if (sourceList != null) { targetList.AddRange(sourceList); }
            return targetList;
        }

        public static IReadOnlyList<T> SafeCopyByValue<T>(IReadOnlyList<T> sourceList)
        {
            List<T> targetList = new List<T>();
            if (sourceList != null) { targetList.AddRange(sourceList); }
            return targetList.AsReadOnly();
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

        public static void IterateList<T>(IList<T> sourceList, Action<T> callback)
        {
            if (sourceList != null)
            {
                foreach (T item in sourceList) { callback(item); }
            }
        }

        public static bool Iterate<T>(T[] sourceList, Action<T> callback)
        {
            bool foundItems = false;
            if (sourceList != null)
            {
                int count = sourceList.Length;
                if (count > 0)
                {
                    foundItems = true;
                    for (int i = 0; i < sourceList.Length; ++i)
                    {
                        callback(sourceList[i]);
                    }
                }
            }
            return foundItems;
        }

        public static bool Iterate<T>(T[] sourceList, Action<T, int> callback)
        {
            bool foundItems = false;
            if (sourceList != null)
            {
                int count = sourceList.Length;
                if (count > 0)
                {
                    foundItems = true;
                    for (int i = 0; i < sourceList.Length; ++i)
                    {
                        callback(sourceList[i], i);
                    }
                }
            }
            return foundItems;
        }

        public static bool Iterate<T>(IList<T> sourceList, Action<T, int> callback)
        {
            bool foundItems = false;
            if (sourceList != null)
            {
                int count = sourceList.Count;
                if (count > 0)
                {
                    foundItems = true;
                    for (int i = 0; i < sourceList.Count; ++i)
                    {
                        callback(sourceList[i], i);
                    }
                }
            }
            return foundItems;
        }

        public static bool Iterate<T>(IReadOnlyList<T> sourceList, Action<T> callback)
        {
            bool foundItems = false;
            if (sourceList != null)
            {
                int count = sourceList.Count;
                if (count > 0)
                {
                    foundItems = true;
                    for (int i = 0; i < sourceList.Count; ++i)
                    {
                        callback(sourceList[i]);
                    }
                }
            }
            return foundItems;
        }

        public static bool Iterate<T>(IReadOnlyList<T> sourceList, Action<T, int> callback)
        {
            bool foundItems = false;
            if (sourceList != null)
            {
                int count = sourceList.Count;
                if (count > 0)
                {
                    foundItems = true;
                    for (int i = 0; i < sourceList.Count; ++i)
                    {
                        callback(sourceList[i], i);
                    }
                }
            }
            return foundItems;
        }

        public static T Find<T>(IReadOnlyList<T> sourceList, Func<T, bool> matchCallback)
        {
            if (sourceList != null)
            {
                foreach (T item in sourceList)
                {
                    if (matchCallback(item)) { return item; }
                }
            }
            return default(T);
        }

        public static void IterateDictionary<T, U>(IDictionary<T, U> sourceDictionary, Action<T, U> callback)
        {
            if (sourceDictionary != null)
            {
                foreach (KeyValuePair<T, U> item in sourceDictionary)
                {
                    callback(item.Key, item.Value);
                }
            }
        }

        public static void TryIterateTagElements(XmlElement root, string tag, Action<XmlElement> callback, IterateCount count = IterateCount.All)
        {
            foreach (XmlElement child in root.ChildNodes)
            {
                if (child.Name.Equals(tag))
                {
                    callback(child);
                    if (count == IterateCount.One)
                    {
                        return;
                    }
                }
            }
        }

        public static void TryIterateTagElements(XmlElement root, string tag, Action<XmlElement, int> callback, IterateCount count = IterateCount.All)
        {
            int i = 0;
            foreach (XmlElement child in root.ChildNodes)
            {
                if (child.Name.Equals(tag))
                {
                    callback(child, i++);
                    if (count == IterateCount.One)
                    {
                        return;
                    }
                }
            }
        }

        public static bool TryGetFirstElement(XmlElement root, string tag, out XmlElement element)
        {
            foreach (XmlElement child in root.ChildNodes)
            {
                if (child.Name.Equals(tag))
                {
                    element = child;
                    return true;
                }
            }

            element = null;
            return false;
        }

        public static int CountChildElements(XmlElement root, string name)
        {
            int count = 0;
            foreach (XmlElement child in root.ChildNodes)
            {
                if (child.Name.Equals(name)) { ++count; }
            }
            return count;
        }
    }
}
