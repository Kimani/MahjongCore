﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using System;

namespace MahjongCore.Riichi.Helpers
{
    public static class GameStateHelpers
    {
        public static void InitializeToFirstDiscard(IGameState state)
        {
            // Stash the AI so we can clear them and put them back when we're done.
            IPlayerAI player1AI = state.Player1AI;
            IPlayerAI player2AI = state.Player2AI;
            IPlayerAI player3AI = state.Player3AI;
            IPlayerAI player4AI = state.Player4AI;
            state.Player1AI = null;
            state.Player2AI = null;
            state.Player3AI = null;
            state.Player4AI = null;

            // Advance to first discard.
            Exception stashedException = null;
            bool discardRequested = false;
            void discardAction(IDiscardInfo info) { discardRequested = true; }
            state.DiscardRequested += discardAction;

            try                 { state.Start(); }
            catch (Exception e) { stashedException = e; }
            Global.Assert(discardRequested);

            // Clean up.
            state.DiscardRequested -= discardAction;
            state.Player1AI = player1AI;
            state.Player2AI = player2AI;
            state.Player3AI = player3AI;
            state.Player4AI = player4AI;
            if (stashedException != null) { throw stashedException; }
        }

        public static void InitializeToDiceRolled(IGameState state)
        {
            // Stash the AI so we can clear them and put them back when we're done.
            IPlayerAI player1AI = state.Player1AI;
            IPlayerAI player2AI = state.Player2AI;
            IPlayerAI player3AI = state.Player3AI;
            IPlayerAI player4AI = state.Player4AI;
            state.Player1AI = null;
            state.Player2AI = null;
            state.Player3AI = null;
            state.Player4AI = null;

            // Advance to first discard. If we end up finishing the game, we're also done.
            Exception stashedException = null;
            bool diceRolledOrGameEnd = false;
            void diceRollAction()                  { diceRolledOrGameEnd = true; }
            void gameEndAction(IGameResult result) { diceRolledOrGameEnd = true; }
            void advanceCheck()                    { if (diceRolledOrGameEnd) { state.Pause(); } }

            state.DiceRolled += diceRollAction;
            state.GameComplete += gameEndAction;
            state.PreCheckAdvance += advanceCheck;

            try { state.Start(); }
            catch (Exception e) { stashedException = e; }
            Global.Assert(diceRolledOrGameEnd);

            // Clean up.
            state.DiceRolled -= diceRollAction;
            state.GameComplete -= gameEndAction;
            state.PreCheckAdvance -= advanceCheck;
            state.Player1AI = player1AI;
            state.Player2AI = player2AI;
            state.Player3AI = player3AI;
            state.Player4AI = player4AI;
            if (stashedException != null) { throw stashedException; }
        }

        public static void AdvanceToDeadWallMoved(IGameState state)
        {
            // Stash the AI so we can clear them and put them back when we're done.
            IPlayerAI player1AI = state.Player1AI;
            IPlayerAI player2AI = state.Player2AI;
            IPlayerAI player3AI = state.Player3AI;
            IPlayerAI player4AI = state.Player4AI;
            state.Player1AI = null;
            state.Player2AI = null;
            state.Player3AI = null;
            state.Player4AI = null;

            // Advance to randomize break.
            Exception stashedException = null;
            bool deadWallHasMoved = false;
            void deadWallMoved() { deadWallHasMoved = true; }
            void advanceCheck()  { if (deadWallHasMoved) { state.Pause(); } }

            state.DeadWallMoved += deadWallMoved;
            state.PreCheckAdvance += advanceCheck;

            try { state.Advance(); }
            catch (Exception e) { stashedException = e; }
            Global.Assert(deadWallHasMoved);

            // Clean up.
            state.DeadWallMoved -= deadWallMoved;
            state.PreCheckAdvance -= advanceCheck;
            state.Player1AI = player1AI;
            state.Player2AI = player2AI;
            state.Player3AI = player3AI;
            state.Player4AI = player4AI;
            if (stashedException != null) { throw stashedException; }
        }

        public static void AdvanceToNextDiscard(IGameState state)
        {
            if (state.State != PlayState.DecideMove)
            {
                // Stash the AI so we can clear them and put them back when we're done.
                IPlayerAI player1AI = state.Player1AI;
                IPlayerAI player2AI = state.Player2AI;
                IPlayerAI player3AI = state.Player3AI;
                IPlayerAI player4AI = state.Player4AI;
                state.Player1AI = null;
                state.Player2AI = null;
                state.Player3AI = null;
                state.Player4AI = null;

                // Advance to first discard. If we end up finishing the game, we're also done.
                Exception stashedException = null;
                bool discardRequestedOrGameEnded = false;
                void discardAction(IDiscardInfo info) { discardRequestedOrGameEnded = true; }
                void gameEndAction(IGameResult result) { discardRequestedOrGameEnded = true; }
                state.DiscardRequested += discardAction;
                state.GameComplete += gameEndAction;

                try { state.Resume(); }
                catch (Exception e) { stashedException = e; }
                Global.Assert(discardRequestedOrGameEnded);

                // Clean up.
                state.DiscardRequested -= discardAction;
                state.GameComplete -= gameEndAction;
                state.Player1AI = player1AI;
                state.Player2AI = player2AI;
                state.Player3AI = player3AI;
                state.Player4AI = player4AI;
                if (stashedException != null) { throw stashedException; }
            }
        }

        public static int GetOffset(Player dealer, int roll)
        {
            int offset = (dealer == Player.Player1) ? 0 :               // Bottom wall, right side, top tile.
                         (dealer == Player.Player2) ? 102 :             // Right wall, bottom side, top tile.
                         (dealer == Player.Player3) ? 68 :              // Top wall, left side, top tile.
                                                      34;               // Left wall, top side, top tile.
            offset = TileHelpers.ClampTile(offset - ((roll - 1) * 34)); // Pick the wall.
            return offset + (roll * 2);                                 // Offset for the dead wall. *2 cause there's 2 tiles on top of each other...
        }

        public static int GetNextDrawIndex(IGameState state)
        {
            bool pickDeadWall = (state.NextAction == GameAction.ReplacementTilePick);
            int doraCount = state.DoraCount;
            return pickDeadWall ? (state.Offset - ((doraCount == 1) ? -1 :
                                                   (doraCount == 2) ? 1 :
                                                   (doraCount == 3) ? 4 : 3)) :
                                  TileHelpers.ClampTile(state.Offset + (122 - state.TilesRemaining));
        }

        public static int GetDoraIndicatorTileIndex(IGameState state, int doraIndex)
        {
            CommonHelpers.Check((doraIndex < 5), "Dora indicator out of range.");
            int deadWallOffset = (doraIndex == 0) ? 6 :
                                 (doraIndex == 1) ? 8 :
                                 (doraIndex == 2) ? 10 :
                                 (doraIndex == 3) ? 12 :
                                                    14;
            return TileHelpers.ClampTile(state.Offset - deadWallOffset);
        }

        public static IHand GetHand(IGameState state, Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Requested player is not a player.");
            return (p == Player.Player1) ? state.Player1Hand :
                   (p == Player.Player2) ? state.Player2Hand :
                   (p == Player.Player3) ? state.Player3Hand :
                                           state.Player4Hand;
        }

        public static IHand GetHandZeroIndex(IGameState state, int i)
        {
            CommonHelpers.Check(((i >= 0) && (i <= 3)), "Requested player is not a player.");
            return (i == 0) ? state.Player1Hand :
                   (i == 1) ? state.Player2Hand :
                   (i == 2) ? state.Player3Hand :
                              state.Player4Hand;
        }

        public static IPlayerAI GetAI(IGameState state, Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get hand for non-player: " + p);
            return (p == Player.Player1) ? state.Player1AI :
                   (p == Player.Player2) ? state.Player2AI :
                   (p == Player.Player3) ? state.Player3AI :
                                           state.Player4AI;
        }

        public static IMeld GetMeldFromDiscard(IGameState state, Player discardPlayer, uint slot)
        {
            IHand calleeHand = GameStateHelpers.GetHand(state, discardPlayer);
            if ((slot < calleeHand.Discards.Count) && (calleeHand.Discards[(int)slot] is ITile calledTile) && calledTile.Called)
            {
                IHand callerHand = GameStateHelpers.GetHand(state, calledTile.Ancillary);
                IMeld foundMeld = null;
                HandHelpers.IterateMeldsOR(callerHand, (IMeld callerMeld) =>
                {
                    if ((callerMeld.Target == discardPlayer) && (callerMeld.CalledTile.Slot == slot))
                    {
                        foundMeld = callerMeld;
                        return true;
                    }
                    return false;
                });
                Global.Assert(foundMeld != null, "Didn't find a meld that matches called tile? Strange.");
                return foundMeld;
            }
            return null;
        }

        public static void IterateHands(IGameState state, Action<IHand> callback)
        {
            callback(state.Player1Hand);
            callback(state.Player2Hand);
            callback(state.Player3Hand);
            callback(state.Player4Hand);
        }
    }
}
