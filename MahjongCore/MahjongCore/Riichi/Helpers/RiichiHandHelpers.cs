// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Helpers
{
    public class RiichiHandHelpers
    {
        public static List<CallOption> AddCallToListAndCheckValid(Hand hand, List<CallOption> calls, CallOption co)
        {
            // Check the CallOption for validity.
            bool fValid = true;
            if (co.Type == MeldState.Chii)
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
                fValid = false;
                int activeTileCount = hand.ActiveTileCount;
                TileType[] activeHand = hand.ActiveHand;

                for (int iActiveHandSlot = 0; !fValid && (iActiveHandSlot < activeTileCount); ++iActiveHandSlot)
                {
                    if ((iActiveHandSlot != co.SlotA) && (iActiveHandSlot != co.SlotB) && (iActiveHandSlot != co.SlotC))
                    {
                        if (!activeHand[iActiveHandSlot].IsEqual(nNoDiscardTile))
                        {
                            fValid = true;
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

        public static List<CallOption> GetCalls(Hand hand, GameState state)
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

            List<CallOption> calls = null;

            TileType callTile = state.NextActionTile;
            int tValue = callTile.GetValue();
            Suit tSuit = callTile.GetSuit();

            // Go through and find chiis. Be mindful of encountering 5s along the way, because we'll need an alternate red 5 option if applicable.
            int activeTileCount = hand.ActiveTileCount;
            TileType[] activeHand = hand.ActiveHand;
            CalledDirection direction = (state.CurrentPlayer.GetNext() == hand.Player)           ? CalledDirection.Left :
                                        (state.CurrentPlayer.GetNext().GetNext() == hand.Player) ? CalledDirection.Across :
                                                                                                   CalledDirection.Right;
            if (!callTile.IsHonor() && (direction == CalledDirection.Left))
            {
                bool suitFound = false;
                int lastValue = -1;
                Suit lastSuit = Suit.None;
                for (int i = 0; i < activeTileCount; ++i)
                {
                    TileType iTile = activeHand[i];
                    int iValue = iTile.GetValue();
                    Suit iSuit = iTile.GetSuit();

                    if (iSuit == tSuit)
                    {
                        // Make sure we're hitting a unique value. Ex: Don't collect two (2-3-4) chii's if we have 334 in the hand.
                        suitFound = true;
                        if ((iValue == lastValue) && (iSuit == lastSuit)) { continue; }

                        // Do different things if the value is -2 from tValue, -1, and +1.
                        if ((iValue + 2) == tValue)
                        {
                            // Chii with the called tile at the end. Need to see if the front or middle tiles are 5s.
                            // Ensure that we have the iValue + 1 tile in our hand. If so, perform a Chii.
                            int middleTileSlot = GetNextActiveHandTileTypeSlot(hand, i);
                            if ((middleTileSlot >= 0) && iTile.IsNext(activeHand[middleTileSlot]))
                            {
                                // See if the front or middle tiles should vary. If we found a tile that's a five, it's guaranteed to be
                                // the non-red if we do indeed have both. So search for the red version from the next slot onward
                                // (if it's not already red)
                                if ((iValue == 5) && !iTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[redSlot], activeHand[middleTileSlot], redSlot, middleTileSlot));
                                    }
                                }
                                else if ((iValue == 4) && !activeHand[middleTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, middleTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[redSlot],  i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[middleTileSlot],  i, middleTileSlot));
                            }
                        }
                        else if ((iValue + 1 == tValue) && (tValue <= 8))
                        {
                            // Chii with the called tile at the middle. Need to see if the front or end tiles are 5s.
                            int lastTileSlot = GetNextActiveHandTileTypeSlot(hand, i);
                            if ((lastTileSlot >= 0) && activeHand[lastTileSlot].GetValue() == tValue) { lastTileSlot = GetNextActiveHandTileTypeSlot(hand, lastTileSlot); }

                            if ((lastTileSlot >= 0) && callTile.IsNext(activeHand[lastTileSlot]))
                            {
                                // See if the front or end tiles should vary.
                                if ((iValue == 5) && !iTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[redSlot], activeHand[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((iValue == 3) && !activeHand[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[lastTileSlot], i, lastTileSlot));
                            }
                        }
                        else if ((iValue - 1 == tValue) && (tValue <= 7))
                        {
                            // Chii with the called tile at the front. Need to see if the middle or end tiles are 5s.
                            int lastTileSlot = GetNextActiveHandTileTypeSlot(hand, i);

                            if ((lastTileSlot >= 0) && iTile.IsNext(activeHand[lastTileSlot]))
                            {
                                // See if the middle or end tiles should vary.
                                if ((iValue == 5) && !iTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[redSlot], activeHand[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((iValue == 4) && !activeHand[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(hand, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = RiichiHandHelpers.AddCallToListAndCheckValid(hand, calls, CallOption.GetChii(callTile, activeHand[i], activeHand[lastTileSlot], i, lastTileSlot));
                            }
                        }
                    }
                    else if (suitFound)
                    {
                        break;
                    }

                    lastValue = iValue;
                    lastSuit = iSuit;
                }
            }

            // Go through and find pons and kans. Go over the active tiles and count the number of tiles
            // of the same suit and value. Once we go past those types of tiles, we can break.
            int tCount = 0;
            int tStartSlot = -1;
            for (int i = 0; i < activeTileCount; ++i)
            {
                if ((activeHand[i].GetValue() == tValue) && (activeHand[i].GetSuit() == tSuit))
                {
                    if (tStartSlot == -1)
                    {
                        tStartSlot = i;
                    }
                    tCount++;
                }
                else if (tCount > 0)
                {
                    break;
                }
            }

            if (tCount == 3)
            {
                if ((tValue == 5) && (hand.Parent.Settings.GetSetting<RedDora>(GameOption.RedDoraOption) != RedDora.RedDora_0))
                {
                    // These are fives and there are red fives. We should figure out how to spread this out. Should figure out how many red dora there are.
                    // If the tile being called on is the red one, then it's easy. Otherwise get one with the red one and one without.
                    // We know that the red one is the one at the end IE tStartSlot+2.
                    if (callTile.IsRedDora())
                    {
                        calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile - 1, callTile - 1, tStartSlot, tStartSlot + 1));

                        // Actually! If callTile is a Red 5 Pin, and if we have 4 red dora, then we can make two different pons - one with two
                        // red fives, and one with just one. So check to see if we can make a call with two red five to show both of them.
                        // Again, the red five we had concealed is in slot tStartSlot+2.
                        if ((callTile == TileType.Circles5Red) && (hand.Parent.Settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile, callTile - 1, tStartSlot + 2, tStartSlot + 1));
                        }
                    }
                    else
                    {
                        // Here, we make two calls with fives, because we have [5][5][R5] in our hand.
                        // We can also have [5][R5][R5] in our hand if we have 4 dora and this is the
                        // circle suit. Either way we can only make two pons:
                        // [ 5 ][5][5]  and [ 5 ][5][R5] if 3 dora OR
                        // [ 5 ][5][R5] and [ 5 ][R5][R5] if 4 dora and pinzu.
                        if ((callTile == TileType.Circles5) && (hand.Parent.Settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile, callTile + 1, tStartSlot, tStartSlot + 1));
                            calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile + 1, callTile + 1, tStartSlot + 1, tStartSlot + 2));
                        }
                        else
                        {
                            calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile, callTile, tStartSlot, tStartSlot + 1));
                            calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile, callTile + 1, tStartSlot + 1, tStartSlot + 2));
                        }
                    }
                }
                else
                {
                    calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, callTile, callTile, tStartSlot, tStartSlot + 1));
                }

                // Make a regular old kan.
                calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetKan(direction, callTile, activeHand[tStartSlot], activeHand[tStartSlot + 1], activeHand[tStartSlot + 2], tStartSlot, tStartSlot + 1, tStartSlot + 2));

                // Make sure the kan is in the bottom right most slot. Slots go:
                //
                // 2 3 5
                // 0 1 4 6
                //
                // Which means: If the kan is in slot 2 or 5, move it down one.
                // If the kan is in slot 3, move down 2.
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
            else if (tCount == 2)
            {
                calls = AddCallToListAndCheckValid(hand, calls, CallOption.GetPon(direction, callTile, activeHand[tStartSlot], activeHand[tStartSlot + 1], tStartSlot, tStartSlot + 1));
            }

            // Return the list of calls.
            return calls;
        }

        public static int GetNextActiveHandTileTypeSlot(Hand hand, int slot)
        {
            int tileSlot = -1;
            TileType[] activeHand = hand.ActiveHand;

            for (int i = slot + 1; i < hand.ActiveTileCount; ++i)
            {
                if (!activeHand[slot].IsEqual(activeHand[i]))
                {
                    tileSlot = i;
                    break;
                }
            }
            return tileSlot;
        }

        public static int GetNextTileRedVersionSlot(Hand hand, int regularFiveSlot)
        {
            int activeTileCount = hand.ActiveTileCount;
            TileType[] activeHand = hand.ActiveHand;

            RiichiGlobal.Assert(regularFiveSlot >= 0);
            RiichiGlobal.Assert(regularFiveSlot < activeTileCount);
            RiichiGlobal.Assert(activeHand[regularFiveSlot].GetValue() == 5);
            RiichiGlobal.Assert(!activeHand[regularFiveSlot].IsRedDora());

            Suit suit = activeHand[regularFiveSlot].GetSuit();
            int slot = -1;
            for (int i = regularFiveSlot + 1; i < activeTileCount; ++i)
            {
                TileType iTile = activeHand[i];
                if ((suit != activeHand[i].GetSuit()) || (activeHand[i].GetValue() != 5))
                {
                    break;
                }

                if (iTile.IsRedDora())
                {
                    slot = i;
                    break;
                }
            }
            return slot;
        }
    }
}
