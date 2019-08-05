// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MahjongCore.Riichi.UnitTests
{
    [TestClass]
    public class BoardOverrideTests
    {
        [TestInitialize]
        public void SaveStateTestSetup()
        {
            Global.AssertHandler = new TestAssertHandler();
            Global.RandomNumberGeneratorHandler = new TestKnownRNG();
        }

        [TestCleanup]
        public void SaveStateTestCleanup()
        {
            Global.AssertHandler = null;
            Global.RandomNumberGeneratorHandler = null;
        }

        [TestMethod]
        public void TestSimple()
        {
        }
    }
}
