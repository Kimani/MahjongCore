// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using System;

namespace MahjongCore.Riichi
{
    public interface IAssert
    {
        void PerformAssert(bool condition);
        void PerformAssert(bool condition, string message);
    }

    public interface IRandomNumberGenerator
    {
        int   Range(int loInclusive, int hiExclusive);
        float Range(float loInclusive, float hiInclusive);
    }

    public interface ILogger
    {
        void Log(string log);
    }

    public class RiichiGlobal
    {
        public static IAssert AssertHandler;
        public static IRandomNumberGenerator RandomNumberGeneratorHandler;
        public static ILogger Logger;

        public static void Log(string log)                        { Logger?.Log(log); }
        public static void Assert(string message)                 { Assert(false, message); }
        public static void Assert(bool condition)                 { Assert(condition, null); }
        public static void Assert(bool condition, string message) { AssertHandler?.PerformAssert(condition, message); }

        public static int RandomRange(int loInclusive, int hiExclusive)
        {
            CommonHelpers.Check((RandomNumberGeneratorHandler != null), "RNG not set!");
            return RandomNumberGeneratorHandler.Range(loInclusive, hiExclusive);
        }

        public static float Range(float loInclusive, float hiInclusive)
        {
            CommonHelpers.Check((RandomNumberGeneratorHandler != null), "RNG not set!");
            return RandomNumberGeneratorHandler.Range(loInclusive, hiInclusive);
        }
    }
}
