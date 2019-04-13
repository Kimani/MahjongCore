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

            testState.Player1Hand.SubmitOverride(OverrideHand.Reach, ReachType.None);
            Assert.AreEqual(30000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(29000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(ReachType.None, testState.Player1Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player2Hand.Reach);
            Assert.AreEqual(ReachType.Reach, testState.Player3Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player4Hand.Reach);
            Assert.AreEqual(testState.Pool, 1);
        }

        [TestMethod]
        public void TestAdvanceOneRoundWithRon()
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
            bool ronOccured = false;
            testState.Ron += (IWinResult result) => { ronOccured = true; };
            IResultCommand east1Result = ResultFactory.BuildRonResultCommand(Player.Player1, Player.Player4, 5, 40);
            testState.SubmitResultCommand(east1Result);

            Assert.IsTrue(ronOccured);
            Assert.AreEqual(38000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(22000, testState.Player4Hand.Score);
            Assert.AreEqual(Round.East2, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player1Hand.Seat);
            Assert.AreEqual(Player.Player1, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);
        }

        [TestMethod]
        public void TestAdvanceOneRoundWithTsumo()
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

            // Advance to east 2 with a mangan tsumo by player 1.
            bool tsumoOccured = false;
            testState.Tsumo += (IWinResult result) => { tsumoOccured = true; };
            IResultCommand east1Result = ResultFactory.BuildTsumoResultCommand(Player.Player1, 5, 30);
            testState.SubmitResultCommand(east1Result);

            Assert.IsTrue(tsumoOccured);
            Assert.AreEqual(38000, testState.Player1Hand.Score);
            Assert.AreEqual(28000, testState.Player2Hand.Score);
            Assert.AreEqual(28000, testState.Player3Hand.Score);
            Assert.AreEqual(26000, testState.Player4Hand.Score);
            Assert.AreEqual(Round.East2, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player1Hand.Seat);
            Assert.AreEqual(Player.Player1, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);
        }

        [TestMethod]
        public void TestAdvanceOneRoundWithExhaustiveDraw()
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

            // Advance to east 2 with a exhaustive draw with player 1 and 2 in tempai.
            bool exhaustiveDrawOccured = false;
            testState.ExhaustiveDraw += (IWinResult result) => { exhaustiveDrawOccured = true; };
            IResultCommand east1Result = ResultFactory.BuildDrawResultCommand(true, true, false, false);
            testState.SubmitResultCommand(east1Result);

            Assert.IsTrue(exhaustiveDrawOccured);
            Assert.AreEqual(31500, testState.Player1Hand.Score);
            Assert.AreEqual(31500, testState.Player2Hand.Score);
            Assert.AreEqual(28500, testState.Player3Hand.Score);
            Assert.AreEqual(28500, testState.Player4Hand.Score);
            Assert.AreEqual(Round.East2, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player1Hand.Seat);
            Assert.AreEqual(Player.Player1, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);
        }

        [TestMethod]
        public void TestAdvanceOneRoundWithAbortiveDraw()
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
            Assert.AreEqual(0, testState.Bonus);
            Assert.AreEqual(Round.East1, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player4Hand.Seat);
            Assert.AreEqual(Player.Player4, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);

            // Stay on east 1 with an abortive draw.
            bool abortiveDrawOccured = false;
            testState.AbortiveDraw += (Player p, AbortiveDrawType type) => { abortiveDrawOccured = true; };
            IResultCommand east1Result = ResultFactory.BuildAbortiveDrawCommand();
            testState.SubmitResultCommand(east1Result);

            Assert.IsTrue(abortiveDrawOccured);
            Assert.AreEqual(30000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(1, testState.Bonus);
            Assert.AreEqual(Round.East1, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player4Hand.Seat);
            Assert.AreEqual(Player.Player4, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);
        }

        [TestMethod]
        public void TestAdvanceOneRoundWithChombo()
        {
            IGameSettings settings = GameSettingsFactory.BuildGameSettings();
            settings.SetSetting(GameOption.StartingPoints, 30000);
            settings.SetSetting(GameOption.ChomboPenaltyOption, ChomboPenalty.Penalty20000);
            settings.SetSetting(GameOption.ChomboTypeOption, ChomboType.BeforeRanking);

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
            Assert.AreEqual(0, testState.Bonus);
            Assert.AreEqual(Round.East1, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player4Hand.Seat);
            Assert.AreEqual(Player.Player4, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);

            // Set reaches.
            testState.Player1Hand.SubmitOverride(OverrideHand.Reach, ReachType.Reach);
            testState.Player3Hand.SubmitOverride(OverrideHand.Reach, ReachType.Reach);
            Assert.AreEqual(29000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(29000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(ReachType.Reach, testState.Player1Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player2Hand.Reach);
            Assert.AreEqual(ReachType.Reach, testState.Player3Hand.Reach);
            Assert.AreEqual(ReachType.None, testState.Player4Hand.Reach);
            Assert.AreEqual(0, testState.Player1Hand.Chombo);
            Assert.AreEqual(0, testState.Player2Hand.Chombo);
            Assert.AreEqual(0, testState.Player3Hand.Chombo);
            Assert.AreEqual(0, testState.Player4Hand.Chombo);
            Assert.AreEqual(testState.Pool, 2);

            // Stay on east 1 with a chombo.
            bool chomboOccured = false;
            testState.Chombo += (Player p) => { chomboOccured = true; };
            IResultCommand east1Result = ResultFactory.BuildChomboResultCommand(Player.Player1);
            testState.SubmitResultCommand(east1Result);

            Assert.IsTrue(chomboOccured);
            Assert.AreEqual(10000, testState.Player1Hand.Score);
            Assert.AreEqual(30000, testState.Player2Hand.Score);
            Assert.AreEqual(30000, testState.Player3Hand.Score);
            Assert.AreEqual(30000, testState.Player4Hand.Score);
            Assert.AreEqual(0, testState.Bonus);
            Assert.AreEqual(0, testState.Pool);
            Assert.AreEqual(1, testState.Player1Hand.Chombo);
            Assert.AreEqual(0, testState.Player2Hand.Chombo);
            Assert.AreEqual(0, testState.Player3Hand.Chombo);
            Assert.AreEqual(0, testState.Player4Hand.Chombo);
            Assert.AreEqual(Round.East1, testState.Round);
            Assert.AreEqual(Wind.East, testState.Player4Hand.Seat);
            Assert.AreEqual(Player.Player4, testState.Dealer);
            Assert.AreEqual(Player.Player4, testState.FirstDealer);
        }
    }
}
