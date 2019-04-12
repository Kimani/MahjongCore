// [Ready Design Corps] - [Mahjong Core Unit Tests] - Copyright 2019

using MahjongCore.Common.UnitTests;
using MahjongCore.Riichi.Impl.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MahjongCore.Riichi.UnitTests
{
    [TestClass]
    public class GameStateTests
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
        public void TestGameStateAdvanceToEndOfGame()
        {
            // Create a state that should play the game to it's completion by only hitting start.
            IGameState testState = GameStateFactory.CreateNewGame();
            testState.Player1AI = new AIRandom();
            testState.Player2AI = new AIRandom();
            testState.Player3AI = new AIRandom();
            testState.Player4AI = new AIRandom();

            bool completed = false;
            testState.GameComplete += (IGameResult result) => { completed = true; };

            // Play the game and ensure we hit completion.
            testState.Start();
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void TestForceReach()
        {
            // Create a state without any AI. Advance until everyone has
            // hands and tiles. Break on DiscardRequested to achieve this.
            IGameSettings settings = GameSettingsFactory.BuildGameSettings();
            settings.SetSetting(GameOption.StartingPoints, 30000);
            IGameState testState = GameStateFactory.CreateNewGame(settings);

            bool discardRequested = false;
            testState.DiscardRequested += (IDiscardInfo info) => { discardRequested = true; };
            testState.Start();
            Assert.IsTrue(discardRequested);

            // Confirm scores and non-reach status.
            Assert.AreEqual(30000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(ReachType.None, testState.Player1Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player2Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player3Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player4Hand.Reach);
            Assert.AreEqual(testState.Pool, 0);

            // Set reaches.
            testState.Player1Hand.SubmitOverride(OverrideHand.Reach, ReachType.Reach);
            Assert.AreEqual(29000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(ReachType.Reach, testState.Player1Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player2Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player3Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player4Hand.Reach);
            Assert.AreEqual(testState.Pool, 1);

            testState.Player3Hand.SubmitOverride(OverrideHand.Reach, ReachType.Reach);
            Assert.AreEqual(29000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(29000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(ReachType.Reach, testState.Player1Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player2Hand.Reach);
            Assert.AreEqual(ReachType.Reach, testState.Player3Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player4Hand.Reach);
            Assert.AreEqual(testState.Pool, 2);
        }

        [TestMethod]
        public void TestAdvanceOneRound()
        {
            IGameSettings settings = GameSettingsFactory.BuildGameSettings();
            settings.SetSetting(GameOption.StartingPoints, 30000);

            IGameState testState = GameStateFactory.CreateNewGame(settings);

            bool discardRequested = false;
            testState.DiscardRequested += (IDiscardInfo info) => { discardRequested = true; };
            testState.Start();
            Assert.IsTrue(discardRequested);

            // Make sure Player 4 is dealer (and first dealer at that)
            testState.SubmitOverride(OverrideState.Dealer, Player.Player4);
            Assert.AreEqual(30000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(Round.East1, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player4Hand.Seat);
            Assert.AreEqual(Player.Player4, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);

            // Advance to east 2 with a mangan ron by player 1 off player 4.
            IResultCommand east1Result = ResultFactory.BuildRonResultCommand(Player.Player1, Player.Player4, 5, 40);
            testState.SubmitResultCommand(east1Result);

            Assert.AreEqual(38000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(22000, testState.Player4Hand.Score);
            Assert.AreEqual(Round.East2, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player1Hand.Seat);
            Assert.AreEqual(Player.Player1, testState.Dealer);
            Assert.AreEqual(Player.Player1, testState.FirstDealer);
        }
    }
}
