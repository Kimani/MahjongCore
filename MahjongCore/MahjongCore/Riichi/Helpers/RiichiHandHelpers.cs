// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Helpers
{
    public class RiichiHandHelpers
    {
        public static List<IMeld> GetCalls(IGameSettings settings, TileType calledTile, TileType[] sourceTiles, CalledDirection direction)
        {
            List<IMeld> calls = null;

            // Prepare the data for analysis.
            Array.Sort(sourceTiles);
            int calledValue = calledTile.GetValue();
            Suit calledSuit = calledTile.GetSuit();

            // Go through and find chiis. Be mindful of encountering 5s along the way, because we'll need an alternate red 5 option if applicable.
            int sourceTileCount = sourceTiles.Length;
            if (!calledTile.IsHonor() && (direction == CalledDirection.Left))
            {
                bool suitFound = false;
                int lastValue = -1;
                Suit lastSuit = Suit.None;
                for (int i = 0; i < sourceTiles.Length; ++i)
                {
                    TileType sourceTile = sourceTiles[i];
                    int sourceTileValue = sourceTile.GetValue();
                    Suit sourceTileSuit = sourceTile.GetSuit();

                    if (sourceTileSuit == calledSuit)
                    {
                        // Make sure we're hitting a unique value. Ex: Don't collect two (2-3-4) chii's if we have 334 in the hand.
                        suitFound = true;
                        if ((sourceTileValue == lastValue) && (sourceTileSuit == lastSuit)) { continue; }

                        // Do different things if the value is -2 from calledValue, -1, and +1.
                        if ((sourceTileValue + 2) == calledValue)
                        {
                            // Chii with the called tile at the end. Need to see if the front or middle tiles are 5s.
                            // Ensure that we have the iValue + 1 tile in our hand. If so, perform a Chii.
                            int middleTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, i);
                            if ((middleTileSlot >= 0) && sourceTile.IsNext(sourceTiles[middleTileSlot]))
                            {
                                // See if the front or middle tiles should vary. If we found a tile that's a five, it's guaranteed to be
                                // the non-red if we do indeed have both. So search for the red version from the next slot onward
                                // (if it's not already red)
                                if ((sourceTileValue == 5) && !sourceTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[redSlot], sourceTiles[middleTileSlot], redSlot, middleTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 4) && !sourceTiles[middleTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, middleTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[middleTileSlot], i, middleTileSlot));
                            }
                        }
                        else if ((sourceTileValue + 1 == calledValue) && (calledValue <= 8))
                        {
                            // Chii with the called tile at the middle. Need to see if the front or end tiles are 5s.
                            int lastTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, i);
                            if ((lastTileSlot >= 0) && sourceTiles[lastTileSlot].GetValue() == sourceTileValue) { lastTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, lastTileSlot); }

                            if ((lastTileSlot >= 0) && calledTile.IsNext(sourceTiles[lastTileSlot]))
                            {
                                // See if the front or end tiles should vary.
                                if ((sourceTileValue == 5) && !sourceTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[redSlot], sourceTiles[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 3) && !sourceTiles[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[lastTileSlot], i, lastTileSlot));
                            }
                        }
                        else if ((sourceTileValue - 1 == calledValue) && (calledValue <= 7))
                        {
                            // Chii with the called tile at the front. Need to see if the middle or end tiles are 5s.
                            int lastTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, i);

                            if ((lastTileSlot >= 0) && sourceTile.IsNext(sourceTiles[lastTileSlot]))
                            {
                                // See if the middle or end tiles should vary.
                                if ((sourceTileValue == 5) && !sourceTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[redSlot], sourceTiles[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 4) && !sourceTiles[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetChii(calledTile, sourceTiles[i], sourceTiles[lastTileSlot], i, lastTileSlot));
                            }
                        }
                    }
                    else if (suitFound)
                    {
                        break;
                    }

                    lastValue = sourceTileValue;
                    lastSuit = sourceTileSuit;
                }
            }

            // Go through and find pons and kans. Go over the active tiles and count the number of tiles
            // of the same suit and value. Once we go past those types of tiles, we can break.
            int sameTileCount = 0;
            int sameTileStartSlot = -1;
            for (int i = 0; i < sourceTiles.Length; ++i)
            {
                if ((sourceTiles[i].GetValue() == calledValue) && (sourceTiles[i].GetSuit() == calledSuit))
                {
                    if (sameTileStartSlot == -1)
                    {
                        sameTileStartSlot = i;
                    }
                    sameTileCount++;
                }
                else if (sameTileCount > 0)
                {
                    break;
                }
            }

            if (sameTileCount == 3)
            {
                if ((calledValue == 5) && (settings.GetSetting<RedDora>(GameOption.RedDoraOption) != RedDora.RedDora_0))
                {
                    // These are fives and there are red fives. We should figure out how to spread this out. Should figure out how many red dora there are.
                    // If the tile being called on is the red one, then it's easy. Otherwise get one with the red one and one without.
                    // We know that the red one is the one at the end IE tStartSlot+2.
                    if (calledTile.IsRedDora())
                    {
                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile - 1, calledTile - 1, sameTileStartSlot, sameTileStartSlot + 1));

                        // Actually! If callTile is a Red 5 Pin, and if we have 4 red dora, then we can make two different pons - one with two
                        // red fives, and one with just one. So check to see if we can make a call with two red five to show both of them.
                        // Again, the red five we had concealed is in slot tStartSlot+2.
                        if ((calledTile == TileType.Circles5Red) && (settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile, calledTile - 1, sameTileStartSlot + 2, sameTileStartSlot + 1));
                        }
                    }
                    else
                    {
                        // Here, we make two calls with fives, because we have [5][5][R5] in our hand.
                        // We can also have [5][R5][R5] in our hand if we have 4 dora and this is the
                        // circle suit. Either way we can only make two pons:
                        // [ 5 ][5][5]  and [ 5 ][5][R5] if 3 dora OR
                        // [ 5 ][5][R5] and [ 5 ][R5][R5] if 4 dora and pinzu.
                        if ((calledTile == TileType.Circles5) && (settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile, calledTile + 1, sameTileStartSlot, sameTileStartSlot + 1));
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile + 1, calledTile + 1, sameTileStartSlot + 1, sameTileStartSlot + 2));
                        }
                        else
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile, calledTile, sameTileStartSlot, sameTileStartSlot + 1));
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile, calledTile + 1, sameTileStartSlot + 1, sameTileStartSlot + 2));
                        }
                    }
                }
                else
                {
                    calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, calledTile, calledTile, sameTileStartSlot, sameTileStartSlot + 1));
                }

                // Make a regular old kan.
                calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetKan(direction, calledTile, sourceTiles[sameTileStartSlot], sourceTiles[sameTileStartSlot + 1], sourceTiles[sameTileStartSlot + 2], sameTileStartSlot, sameTileStartSlot + 1, sameTileStartSlot + 2));
            }
            else if (sameTileCount == 2)
            {
                calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, CallOption.GetPon(direction, calledTile, sourceTiles[sameTileStartSlot], sourceTiles[sameTileStartSlot + 1], sameTileStartSlot, sameTileStartSlot + 1));
            }
            return calls;
        }

        internal static List<CallOption> GetCalls(Hand hand, GameStateImpl state)
        {
            // If this is the 4th kan and noone is suukantsu tempai, then we cannot make any calls.
            if ((state.PrevAction == GameAction.ReplacementTilePick) &&
                (state.DoraCount >= 4) &&
                !state.GetHand(Player.Player1).IsFourKans() &&
                !state.GetHand(Player.Player2).IsFourKans() &&
                !state.GetHand(Player.Player3).IsFourKans() &&
                !state.GetHand(Player.Player4).IsFourKans())
            {
                return null;
            }

            // Make a copy of hand's ActiveHand.
            TileType[] activeHandCopy = new TileType[hand.ActiveTileCount];
            for (int i = 0; i < hand.ActiveTileCount; ++i)
            {
                activeHandCopy[i] = hand.ActiveHand[i];
            }

            CalledDirection direction = (state.CurrentPlayer.GetNext() == hand.Player)           ? CalledDirection.Left :
                                        (state.CurrentPlayer.GetNext().GetNext() == hand.Player) ? CalledDirection.Across :
                                                                                                   CalledDirection.Right;
            List<CallOption> calls = GetCalls(state.Settings, state.NextActionTile, activeHandCopy, direction);

            // If there is a kan make sure it is in the bottom right most slot. Slots go:
            //
            // 2 3 5
            // 0 1 4 6
            //
            // Which means: If the kan is in slot 2 or 5, move it down one.
            // If the kan is in slot 3, move down 2.
            bool containsKan = false;
            foreach (CallOption co in calls)
            {
                if (co.Type.GetMeldType() == MeldType.Kan)
                {
                    containsKan = true;
                    break;
                }
            }

            if (containsKan)
            {
                int callSize = calls.Count;
                int kanSlot = callSize - 1;
                if ((kanSlot == 2) || (kanSlot == 5))
                {
                    CallOption cOpt = calls[callSize - 2];
                    calls.RemoveAt(callSize - 2);
                    calls.Add(cOpt);
                }
                if (kanSlot == 3)
                {
                    CallOption cOptA = calls[callSize - 2];
                    CallOption cOptB = calls[callSize - 3];
                    calls.RemoveAt(callSize - 2);
                    calls.RemoveAt(callSize - 2);
                    calls.Add(cOptA);
                    calls.Add(cOptB);
                }
            }

            // Return the list of calls.
            return calls;
        }

        private static List<CallOption> AddCallToListAndCheckValid(GameSettings settings, TileType[] sourceTiles, List<CallOption> calls, CallOption co)
        {
            // Check the CallOption for validity.
            bool fValid = true;
            if (!settings.GetSetting<bool>(GameOption.SequenceSwitch) && (co.Type == MeldState.Chii))
            {
                // Determine what tile we can't discard if we take this chii.
                TileType nNoDiscardTile = TileType.None;
                int tileCalledValue = co.TileA.GetValue();
                int tileAValue = co.TileB.GetValue();
                int tileBValue = co.TileC.GetValue();

                if ((tileCalledValue < tileAValue) && (tileCalledValue < tileBValue))
                {
                    // Tile is the lowest. Can't discard 3 higher.
                    // Note that if we go out of range, BuildTile will return NO_TILE.
                    nNoDiscardTile = TileHelpers.BuildTile(co.TileA.GetSuit(), tileCalledValue + 3);
                }
                else if ((tileCalledValue > tileAValue) && (tileCalledValue > tileBValue))
                {
                    // Tile is the highest. Can't discard 3 lower.
                    nNoDiscardTile = TileHelpers.BuildTile(co.TileA.GetSuit(), tileCalledValue - 3);
                }

                // Determine if we have any tile, besides the ones we're going to consume with the chii, that
                // aren't equal to nNoDiscardTile. This means skip looking at the tiles in slot co.SlotA/B/C.
                // We don't want to allow a chii where the only remaining discardable tile can't be discarded.
                fValid = false;
                for (int iActiveHandSlot = 0; !fValid && (iActiveHandSlot < sourceTiles.Length); ++iActiveHandSlot)
                {
                    if ((iActiveHandSlot != co.SlotA) && (iActiveHandSlot != co.SlotB) && (iActiveHandSlot != co.SlotC))
                    {
                        if (!sourceTiles[iActiveHandSlot].IsEqual(nNoDiscardTile))
                        {
                            fValid = true;
                            break;
                        }
                    }
                }
            }

            // Create the call list.
            List<CallOption> localCalls = calls;
            if (fValid)
            {
                if (localCalls == null)
                {
                    localCalls = new List<CallOption>();
                }
                localCalls.Add(co);
            }
            return localCalls;
        }

        private static int GetNextActiveHandTileTypeSlot(TileType[] sourceTiles, int slot)
        {
            for (int i = slot + 1; i < sourceTiles.Length; ++i)
            {
                if (!sourceTiles[slot].IsEqual(sourceTiles[i])) { return i; }
            }
            return -1;
        }

        private static int GetNextTileRedVersionSlot(TileType[] sourceTiles, int regularFiveSlot)
        {
            Global.Assert(regularFiveSlot >= 0);
            Global.Assert(regularFiveSlot < sourceTiles.Length);
            Global.Assert(sourceTiles[regularFiveSlot].GetValue() == 5);
            Global.Assert(!sourceTiles[regularFiveSlot].IsRedDora());

            Suit suit = sourceTiles[regularFiveSlot].GetSuit();
            for (int i = regularFiveSlot + 1; i < sourceTiles.Length; ++i)
            {
                TileType iTile = sourceTiles[i];
                if ((suit != iTile.GetSuit()) || (iTile.GetValue() != 5)) { break; }
                if (iTile.IsRedDora())                                    { return i; }
            }
            return -1;
        }
    }
}
