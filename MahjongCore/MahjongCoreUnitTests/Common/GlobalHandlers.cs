// [Ready Design Corps] - [Mahjong Core Unit Tests] - Copyright 2019

using MahjongCore.Riichi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MahjongCore.Common.UnitTests
{
    class TestKnownRNG : IRandomNumberGenerator
    {
        // IRandomNumberGenerator
        public int Range(int loInclusive, int hiExclusive)       { return _RNG.Next(loInclusive, hiExclusive); }
        public float Range(float loInclusive, float hiInclusive) { return (float)(_RNG.NextDouble() * (double)(hiInclusive - loInclusive)) + loInclusive; }

        // TestKnownRNG
        private Random _RNG;

        public TestKnownRNG() { _RNG = new Random(1234); }
    }

    class TestAssertHandler : IAssert
    {
        // IAssert
        public void PerformAssert(bool condition)                 { Assert.IsTrue(condition); }
        public void PerformAssert(bool condition, string message) { Assert.IsTrue(condition, message); }
    }
}
