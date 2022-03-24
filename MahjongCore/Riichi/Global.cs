// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;

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

    public class Global
    {
        public static IAssert AssertHandler;
        public static IRandomNumberGenerator RandomNumberGeneratorHandler;
        public static ILogger Logger;
        public static ILogger ExtraLogger;

        public static bool CanAssert   { get { return AssertHandler != null; } }
        public static bool CanLog      { get { return Logger != null; } }
        public static bool CanLogExtra { get { return ExtraLogger != null; } }

        public static void Log(string log)                        { Logger?.Log(log); }
        public static void LogExtra(string log)                   { ExtraLogger?.Log(log); }
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
