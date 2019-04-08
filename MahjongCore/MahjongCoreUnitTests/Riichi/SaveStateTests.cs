// [Ready Design Corps] - [Mahjong Core Unit Tests] - Copyright 2019

using MahjongCore.Common.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MahjongCore.Riichi.UnitTests
{
    [TestClass]
    public class SaveStateTests
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
        public void TestSimpleSaveStateMarshalAndUnmarshal()
        {
            // Create a GameState and create a save state.
            IGameState originalState = GameStateFactory.CreateNewGame();
            ISaveState originalSave = originalState.Save();
            string originalMarshaledSave = originalSave.Marshal();

            // Take originalMarshaledSave and turn it into an IGameState.
            ISaveState loadedSave = SaveStateFactory.Unmarshal(originalMarshaledSave);
            IGameState loadedState = GameStateFactory.LoadGame(loadedSave);
        }
    }
}
