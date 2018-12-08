// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MahjongCore.Riichi.Helpers
{
    public class HandHelpers
    {
        public static void IterateTiles(IHand hand, Action<TileType> callback)      { for (int i = 0; i < hand.TileCount; ++i) { callback(hand.Tiles[i].Type); } }
        public static void IterateTiles(IHand hand, Action<TileType, int> callback) { for (int i = 0; i < hand.TileCount; ++i) { callback(hand.Tiles[i].Type, i); } }
        public static void IterateMelds(IHand hand, Action<IMeld> callback)         { foreach (IMeld meld in hand.Melds) { if (!meld.State.IsCalled()) { break; } callback(meld); } }

        public static bool IterateTilesAND(IHand hand, Func<TileType, bool> callback)
        {
            for (int i = 0; i < hand.TileCount; ++i)
            {
                if (!callback(hand.Tiles[i].Type)) { return false; }
            }
            return true;
        }

        public static bool IterateTilesOR(IHand hand, Func<TileType, bool> callback)
        {
            for (int i = 0; i < hand.TileCount; ++i)
            {
                if (callback(hand.Tiles[i].Type)) { return true; }
            }
            return false;
        }

        public static void IterateMelds(IHand hand, Action<IMeld, int> callback)
        {
            int i = 0;
            foreach (IMeld meld in hand.Melds)
            {
                if (!meld.State.IsCalled()) { break; }
                callback(meld, i++);
            }
        }

        public static bool IterateMeldsAND(IHand hand, Func<IMeld, bool> callback)
        {
            foreach (IMeld meld in hand.Melds)
            {
                if (!meld.State.IsCalled()) { break; }
                if (!callback(meld))        { return false; }
            }
            return true;
        }

        public static bool IterateMeldsOR(IHand hand, Func<IMeld, bool> callback)
        {
            foreach (IMeld meld in hand.Melds)
            {
                if (!meld.State.IsCalled()) { break; }
                if (callback(meld))         { return true; }
            }
            return false;
        }

        public static TileType[] GetSortedTiles(IHand hand)
        {
            TileType[] sortedTiles = new TileType[hand.TileCount];
            IterateTiles(hand, (TileType tile, int i) => { sortedTiles[i] = tile; });
            Array.Sort(sortedTiles);
            return sortedTiles;
        }

        internal static List<IMeld> GetCalls(HandImpl hand)
        {
            GameStateImpl state = hand.Parent as GameStateImpl;
            return GetCalls(hand.Player, state.Current, state.Settings, state.GetHand(state.Current).Discards.Last(), GetSortedTiles(hand));
        }

        private static List<IMeld> GetCalls(Player owner, Player target, IGameSettings settings, ITile calledTile, TileType[] sourceTiles)
        {
            List<IMeld> calls = null;
            int calledValue = calledTile.Type.GetValue();
            Suit calledSuit = calledTile.Type.GetSuit();

            // Go through and find chiis. Be mindful of encountering 5s along the way, because we'll need an alternate red 5 option if applicable.
            if (!calledTile.Type.IsHonor() && (owner.GetTargetPlayerDirection(target) == CalledDirection.Left))
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
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[redSlot], sourceTiles[middleTileSlot], redSlot, middleTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 4) && !sourceTiles[middleTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, middleTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[middleTileSlot], i, middleTileSlot));
                            }
                        }
                        else if ((sourceTileValue + 1 == calledValue) && (calledValue <= 8))
                        {
                            // Chii with the called tile at the middle. Need to see if the front or end tiles are 5s.
                            int lastTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, i);
                            if ((lastTileSlot >= 0) && sourceTiles[lastTileSlot].GetValue() == sourceTileValue) { lastTileSlot = GetNextActiveHandTileTypeSlot(sourceTiles, lastTileSlot); }

                            if ((lastTileSlot >= 0) && calledTile.Type.IsNext(sourceTiles[lastTileSlot]))
                            {
                                // See if the front or end tiles should vary.
                                if ((sourceTileValue == 5) && !sourceTile.IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, i);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[redSlot], sourceTiles[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 3) && !sourceTiles[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[lastTileSlot], i, lastTileSlot));
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
                                        calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[redSlot], sourceTiles[lastTileSlot], redSlot, lastTileSlot));
                                    }
                                }
                                else if ((sourceTileValue == 4) && !sourceTiles[lastTileSlot].IsRedDora())
                                {
                                    int redSlot = GetNextTileRedVersionSlot(sourceTiles, lastTileSlot);
                                    if (redSlot >= 0)
                                    {
                                        calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[redSlot], i, redSlot));
                                    }
                                }

                                // Make the chii.
                                calls = HandHelpers.AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildChii(target, calledTile, sourceTiles[i], sourceTiles[lastTileSlot], i, lastTileSlot));
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
                    if (calledTile.Type.IsRedDora())
                    {
                        calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type.GetNonRedDoraVersion(), calledTile.Type.GetNonRedDoraVersion(), sameTileStartSlot, sameTileStartSlot + 1));

                        // Actually! If callTile is a Red 5 Pin, and if we have 4 red dora, then we can make two different pons - one with two
                        // red fives, and one with just one. So check to see if we can make a call with two red five to show both of them.
                        // Again, the red five we had concealed is in slot tStartSlot+2.
                        if ((calledTile.Type == TileType.Circles5Red) && (settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type, calledTile.Type.GetNonRedDoraVersion(), sameTileStartSlot + 2, sameTileStartSlot + 1));
                        }
                    }
                    else
                    {
                        // Here, we make two calls with fives, because we have [5][5][R5] in our hand.
                        // We can also have [5][R5][R5] in our hand if we have 4 dora and this is the
                        // circle suit. Either way we can only make two pons:
                        // [ 5 ][5][5]  and [ 5 ][5][R5] if 3 dora OR
                        // [ 5 ][5][R5] and [ 5 ][R5][R5] if 4 dora and pinzu.
                        if ((calledTile.Type == TileType.Circles5) && (settings.GetSetting<RedDora>(GameOption.RedDoraOption) == RedDora.RedDora_4))
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type, calledTile.Type.GetRedDoraVersion(), sameTileStartSlot, sameTileStartSlot + 1));
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type.GetRedDoraVersion(), calledTile.Type.GetRedDoraVersion(), sameTileStartSlot + 1, sameTileStartSlot + 2));
                        }
                        else
                        {
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type, calledTile.Type, sameTileStartSlot, sameTileStartSlot + 1));
                            calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type, calledTile.Type.GetRedDoraVersion(), sameTileStartSlot + 1, sameTileStartSlot + 2));
                        }
                    }
                }
                else
                {
                    calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, calledTile.Type, calledTile.Type, sameTileStartSlot, sameTileStartSlot + 1));
                }

                // Make a regular old kan.
                calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildOpenKan(target, owner, calledTile, sourceTiles[sameTileStartSlot], sourceTiles[sameTileStartSlot + 1], sourceTiles[sameTileStartSlot + 2], sameTileStartSlot, sameTileStartSlot + 1, sameTileStartSlot + 2));
            }
            else if (sameTileCount == 2)
            {
                calls = AddCallToListAndCheckValid(settings, sourceTiles, calls, MeldFactory.BuildPon(target, owner, calledTile, sourceTiles[sameTileStartSlot], sourceTiles[sameTileStartSlot + 1], sameTileStartSlot, sameTileStartSlot + 1));
            }
            return calls;
        }


        private static List<IMeld> AddCallToListAndCheckValid(IGameSettings settings, TileType[] sourceTiles, List<IMeld> calls, IMeld meld)
        {
            // Check the CallOption for validity.
            bool valid = true;
            if (!settings.GetSetting<bool>(GameOption.SequenceSwitch) && (meld.State == MeldState.Chii))
            {
                // Determine what tile we can't discard if we take this chii.
                TileType kuikaeTile = TileType.None;
                int tileCalledValue = meld.CalledTile.Type.GetValue();
                int tileAValue = meld.Tiles[1].Type.GetValue();
                int tileBValue = meld.Tiles[2].Type.GetValue();

                if ((tileCalledValue < tileAValue) && (tileCalledValue < tileBValue))
                {
                    // Tile is the lowest. Can't discard 3 higher.
                    // Note that if we go out of range, BuildTile will return NO_TILE.
                    kuikaeTile = TileHelpers.BuildTile(meld.CalledTile.Type.GetSuit(), tileCalledValue + 3);
                }
                else if ((tileCalledValue > tileAValue) && (tileCalledValue > tileBValue))
                {
                    // Tile is the highest. Can't discard 3 lower.
                    kuikaeTile = TileHelpers.BuildTile(meld.CalledTile.Type.GetSuit(), tileCalledValue - 3);
                }

                // Determine if we have any tile, besides the ones we're going to consume with the chii, that
                // aren't equal to kuikaeTile. This means skip looking at the tiles in slot co.SlotA/B/C.
                // We don't want to allow a chii where the only remaining discardable tile can't be discarded.
                valid = false;
                for (int i = 0; i < sourceTiles.Length; ++i)
                {
                    if ((i != meld.Tiles[0].Slot) &&
                        (i != meld.Tiles[1].Slot) &&
                        (i != meld.Tiles[2].Slot) &&
                        !sourceTiles[i].IsEqual(kuikaeTile))
                    {
                        valid = true;
                        break;
                    }
                }
            }

            // Create the call list.
            List<IMeld> localCalls = calls;
            if (valid)
            {
                if (localCalls == null)
                {
                    localCalls = new List<IMeld>();
                }
                localCalls.Add(meld);
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
