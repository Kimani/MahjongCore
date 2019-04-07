// [Ready Design Corps] - [Mahjong Core Unit Tests] - Copyright 2018

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MahjongCore.Riichi.UnitTests
{
    [TestClass]
    public class SaveStateTests
    {
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
