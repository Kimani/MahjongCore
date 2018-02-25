// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Evaluator
{
    public class HandEvaluator
    {
        private static int BITFIELD_KOKUSHI_1_CHAR = 0x0001;
        private static int BITFIELD_KOKUSHI_9_CHAR = 0x0002;
        private static int BITFIELD_KOKUSHI_1_BAM  = 0x0004;
        private static int BITFIELD_KOKUSHI_9_BAM  = 0x0008;
        private static int BITFIELD_KOKUSHI_1_CIRC = 0x0010;
        private static int BITFIELD_KOKUSHI_9_CIRC = 0x0020;
        private static int BITFIELD_KOKUSHI_NORTH  = 0x0040;
        private static int BITFIELD_KOKUSHI_SOUTH  = 0x0080;
        private static int BITFIELD_KOKUSHI_EAST   = 0x0100;
        private static int BITFIELD_KOKUSHI_WEST   = 0x0200;
        private static int BITFIELD_KOKUSHI_HATSU  = 0x0400;
        private static int BITFIELD_KOKUSHI_HAKU   = 0x0800;
        private static int BITFIELD_KOKUSHI_CHUN   = 0x1000;
        private static int BITFIELD_KOKUSHI_ALL    = 0x1fff;

        /**
         * Returns an array with the waits this hand has. Note that this will return regular 5 and never RED_5. This does NOT take into account
         * if the hand has any yaku. Wouldn't be good to do so anyway - would be tricky to see if it's a Haitei or something like that...
         * @param hand The RiichiHand that should be processed.
         * @param ignoreTileSlot  If you pass in -1 then this assumes that we don't have a full hand here, ie it's not our turn and we haven't picked up a tile.
         *                        If you pass in >= 0 then we will get our waits ignoring the given tile - getting our waits as if we discarded the given tile.
         */
        public static List<TileType> GetWaits(Hand hand, int ignoreTileSlot, TileType[] approvedReachKanTiles)
        {
            // Get the sorted waiting hand without ignoreSlot.
            int activeTileCount = hand.ActiveTileCount;
            TileType[] activeHand = hand.ActiveHand;
            TileType[] sublist;
            if (ignoreTileSlot == -1)
            {
                sublist = new TileType[13];
                for (int i = 0; i < 13; ++i)
                {
                    sublist[i] = activeHand[i];
                }
            }
            else
            {
                RiichiGlobal.Assert(hand.IsFullHand(), "We don't have a full hand?? ActiveTileCount: " + activeTileCount);
                sublist = new TileType[activeTileCount - 1];
                {
                    int target = 0;
                    for (int i = 0; i < activeTileCount; ++i)
                    {
                        if (i == ignoreTileSlot)
                        {
                            continue;
                        }
                        sublist[target++] = activeHand[i];
                    }
                    Array.Sort(sublist);
                }
            }

            GameState state = hand.Parent;
            bool anyCalls = (state.Player1Hand.GetCalledMeldCount() > 0) ||
                            (state.Player2Hand.GetCalledMeldCount() > 0) ||
                            (state.Player3Hand.GetCalledMeldCount() > 0) ||
                            (state.Player4Hand.GetCalledMeldCount() > 0);

            // Get the waits and set the overrideNoReachFlag.
            bool overrideNoReachFlag;
            List<TileType> waits = GetWaits(sublist, approvedReachKanTiles, state.GetDiscards(hand.Player), anyCalls, out overrideNoReachFlag);
            hand.OverrideNoReachFlag = overrideNoReachFlag;
            return waits;
        }

        private static TileType[] g_stashedSortedWaitingHand;
        public static List<TileType> GetWaits(TileType[] sortedWaitingHand, TileType[] approvedReachKanTiles, List<ExtendedTile> discards, bool anyCalls, out bool overrideNoReachFlag)
        {
            overrideNoReachFlag = false;

            // Get the activeTileCount. If it's not of an expected value, we don't have any waits to speak of.
            List<TileType> waitList = new List<TileType>();
            int activeTileCount = 0;
            for (int i = 0; i < sortedWaitingHand.Length; ++i)
            {
                if (sortedWaitingHand[i].IsTile())
                {
                    activeTileCount++;
                }
                else
                {
                    break;
                }
            }

            if ((activeTileCount != 13) &&
                (activeTileCount != 10) &&
                (activeTileCount != 7) &&
                (activeTileCount != 4) &&
                (activeTileCount != 1))
            {
                return waitList;
            }

            g_stashedSortedWaitingHand = sortedWaitingHand;

            // Check to see if we're in a wait for thirteen or fourteen broken. Check to make sure we do not have any tiles
            // that are meldable and we have at most one pair.
            try
            {
                if ((activeTileCount == 13) && !anyCalls && (discards.Count == 0))
                {
                    int nPairCount = 0;
                    for (int i = 0; i < 12; ++i)
                    {
                        TileType nTileA = sortedWaitingHand[i];
                        TileType nTileB = sortedWaitingHand[i + 1];

                        if (nTileA.GetSuit() == nTileB.GetSuit())
                        {
                            int nValueA = nTileA.GetValue();
                            int nValueB = nTileB.GetValue();
                            if (nValueA == nValueB)
                            {
                                nPairCount++;
                                if (nPairCount > 1)
                                {
                                    break;
                                }
                            }
                            else if (Math.Abs(nValueA - nValueB) < 3)
                            {
                                nPairCount = 10; // To indicate failure.
                                break;
                            }
                        }
                    }

                    if (nPairCount <= 1)
                    {
                        // Alright. Made an array with ALL tiles as waits. Then remove ones that would be meldable with each tile.
                        // If we have no pair at all then keep the tile itself.
                        List<TileType> brokenWaitTiles = new List<TileType>();
                        foreach (TileType tt in Enum.GetValues(typeof(TileType)))
                        {
                            if (tt.IsTile() && !tt.IsRedDora())
                            {
                                brokenWaitTiles.Add(tt);
                            }
                        }

                        for (int i = 0; i < 13; ++i)
                        {
                            if (sortedWaitingHand[i].IsHonor())
                            {
                                if (nPairCount > 0)
                                {
                                    brokenWaitTiles.Remove(sortedWaitingHand[i]);
                                }
                            }
                            else
                            {
                                Suit tileSuit = sortedWaitingHand[i].GetSuit();
                                int tileValue = sortedWaitingHand[i].GetValue();
                                TileType tilePlus1 = TileHelpers.BuildTile(tileSuit, (tileValue + 1));
                                TileType tilePlus2 = TileHelpers.BuildTile(tileSuit, (tileValue + 2));
                                TileType tileMinus1 = TileHelpers.BuildTile(tileSuit, (tileValue - 1));
                                TileType tileMinus2 = TileHelpers.BuildTile(tileSuit, (tileValue - 2));

                                if (tilePlus1 != TileType.None) { brokenWaitTiles.Remove(tilePlus1); }
                                if (tilePlus2 != TileType.None) { brokenWaitTiles.Remove(tilePlus2); }
                                if (tileMinus1 != TileType.None) { brokenWaitTiles.Remove(tileMinus1); }
                                if (tileMinus2 != TileType.None) { brokenWaitTiles.Remove(tileMinus2); }
                                if (nPairCount > 0) { brokenWaitTiles.Remove(sortedWaitingHand[i]); }
                            }
                        }

                        // Add the tiles to the wait list. Set OverrideNoReachFlag because we can't reach with this.
                        overrideNoReachFlag = true;
                        waitList.AddRange(brokenWaitTiles);
                    }
                }
            }
            catch (Exception e)
            {
                RiichiGlobal.Assert(false, e.StackTrace);
            }

            // Check to see if we're in a wait for thirteen orphans. Handle both the single and 13 sided wait scenarios.
            // Start by checking if sublist is of length 13. If so then we have no called melds, so we can go ahead with the check.
            if (activeTileCount == 13)
            {
                int bitFieldHonors = 0;
                int honorCount = 0;

                for (int i = activeTileCount - 1; i >= 0; --i)
                {
                    TileType tile = sortedWaitingHand[i];
                    if (!tile.IsTerminalOrHonor())
                    {
                        honorCount = 0;
                        break;
                    }

                    int mask = 0;
                    if (tile.IsEqual(TileType.Bamboo1))          { mask = BITFIELD_KOKUSHI_1_BAM; }
                    else if (tile.IsEqual(TileType.Bamboo9))     { mask = BITFIELD_KOKUSHI_9_BAM; }
                    else if (tile.IsEqual(TileType.Characters1)) { mask = BITFIELD_KOKUSHI_1_CHAR; }
                    else if (tile.IsEqual(TileType.Characters9)) { mask = BITFIELD_KOKUSHI_9_CHAR; }
                    else if (tile.IsEqual(TileType.Circles1))    { mask = BITFIELD_KOKUSHI_1_CIRC; }
                    else if (tile.IsEqual(TileType.Circles9))    { mask = BITFIELD_KOKUSHI_9_CIRC; }
                    else if (tile.IsEqual(TileType.North))       { mask = BITFIELD_KOKUSHI_NORTH; }
                    else if (tile.IsEqual(TileType.South))       { mask = BITFIELD_KOKUSHI_SOUTH; }
                    else if (tile.IsEqual(TileType.East))        { mask = BITFIELD_KOKUSHI_EAST; }
                    else if (tile.IsEqual(TileType.West))        { mask = BITFIELD_KOKUSHI_WEST; }
                    else if (tile.IsEqual(TileType.Hatsu))       { mask = BITFIELD_KOKUSHI_HATSU; }
                    else if (tile.IsEqual(TileType.Haku))        { mask = BITFIELD_KOKUSHI_HAKU; }
                    else                                         { mask = BITFIELD_KOKUSHI_CHUN; }

                    if ((bitFieldHonors & mask) == 0)
                    {
                        honorCount++;
                        bitFieldHonors |= mask;
                    }
                }

                if (honorCount == 13)
                {
                    waitList.Add(TileType.Bamboo1);
                    waitList.Add(TileType.Bamboo9);
                    waitList.Add(TileType.Characters1);
                    waitList.Add(TileType.Characters9);
                    waitList.Add(TileType.Circles1);
                    waitList.Add(TileType.Circles9);
                    waitList.Add(TileType.East);
                    waitList.Add(TileType.North);
                    waitList.Add(TileType.South);
                    waitList.Add(TileType.West);
                    waitList.Add(TileType.Chun);
                    waitList.Add(TileType.Haku);
                    waitList.Add(TileType.Hatsu);
                }
                else if (honorCount == 12)
                {
                    int bitFieldTile = ~bitFieldHonors & BITFIELD_KOKUSHI_ALL;
                    if      (bitFieldTile == BITFIELD_KOKUSHI_1_BAM)  { waitList.Add(TileType.Bamboo1); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_9_BAM)  { waitList.Add(TileType.Bamboo9); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_1_CHAR) { waitList.Add(TileType.Characters1); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_9_CHAR) { waitList.Add(TileType.Characters9); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_1_CIRC) { waitList.Add(TileType.Circles1); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_9_CIRC) { waitList.Add(TileType.Circles9); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_NORTH)  { waitList.Add(TileType.North); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_SOUTH)  { waitList.Add(TileType.South); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_EAST)   { waitList.Add(TileType.East); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_WEST)   { waitList.Add(TileType.West); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_HATSU)  { waitList.Add(TileType.Hatsu); }
                    else if (bitFieldTile == BITFIELD_KOKUSHI_HAKU)   { waitList.Add(TileType.Haku); }
                    else                                              { waitList.Add(TileType.Chun); }
                }
            }

            // Check to see if we're in a wait for a seven pairs hand. Make sure we don't have doubles in our pairs.
            if (activeTileCount == 13)
            {
                TileType singleTile = TileType.None;
                int tileCount = 1;
                TileType recentTile = sortedWaitingHand[0];
                bool fFound = true;

                for (int i = 1; i < 13; ++i)
                {
                    TileType tTile = sortedWaitingHand[i];
                    if (recentTile.IsEqual(tTile))
                    {
                        tileCount++;
                        if (tileCount > 2)
                        {
                            // We have 3+ tiles. No seven pairs.
                            singleTile = TileType.None;
                            fFound = false;
                            break;
                        }
                    }
                    else
                    {
                        if (tileCount == 1)
                        {
                            if (singleTile != TileType.None)
                            {
                                // We already encountered a single tile. No seven pairs.
                                singleTile = TileType.None;
                                fFound = false;
                                break;
                            }
                            else
                            {
                                singleTile = recentTile;
                            }
                        }
                        else
                        {
                            // We must have 2. Otherwise we would have had 2+, which would have exited this loop.
                            tileCount = 1;
                        }
                        recentTile = tTile;
                    }
                }

                if (singleTile != TileType.None)
                {
                    waitList.Add(singleTile);
                }
                else if (fFound)
                {
                    // If we didn't get an opportunity to find the single tile in the loop above, it's because it was the last tile in the list.
                    waitList.Add(sortedWaitingHand[12]);
                }
            }

            // Run our algorithm over the existing hand. We want to do this by first extracting out the pair. Do so by first branching for each
            // existing pair - these branches will have a wildcard tile. For all tiles that don't have a pair, branch with the wildcard used up.
            // There's no need to do a branch of tiles that have a pair with a branch that uses a wildcard for the pair.
            List<TileType> threeOfAKindTiles = new List<TileType>();
            List<TileType> threeInARowInHandTiles = new List<TileType>();
            TileType lastTileLookedAt = sortedWaitingHand[0];
            int lastTileLookedCount = 1;
            int calledMeldCount = (activeTileCount == 13) ? 0 :
                                  (activeTileCount == 10) ? 1 :
                                  (activeTileCount == 7)  ? 2 :
                                  (activeTileCount == 4)  ? 3 :
                                  (activeTileCount == 1)  ? 4 : -1;

            for (int i = 1; i < activeTileCount; ++i)
            {
                if (lastTileLookedAt.IsEqual(sortedWaitingHand[i]))
                {
                    lastTileLookedCount++;
                    if (lastTileLookedCount == 2)
                    {
                        TileType[] waitSubHand = GetSubhand(sortedWaitingHand, i, (i - 1));
                        GetWaitsBranch(waitList, new WaitCandidateHand(waitSubHand, calledMeldCount, true), threeOfAKindTiles, threeInARowInHandTiles);
                    }
                }
                else
                {
                    // If lastTileLookedCount is 1, then we just went past the only tile of this type. Branch.
                    if (lastTileLookedCount == 1)
                    {
                        TileType[] waitSubHand = GetSubhand(sortedWaitingHand, (i - 1), -1);
                        WaitCandidateHand wch = new WaitCandidateHand(waitSubHand, calledMeldCount, false);
                        wch.WaitA = lastTileLookedAt;
                        GetWaitsBranch(waitList, wch, threeOfAKindTiles, threeInARowInHandTiles);
                    }

                    lastTileLookedAt = sortedWaitingHand[i];
                    lastTileLookedCount = 1;
                }
            }

            if (lastTileLookedCount == 1)
            {
                // Branch using this tile as the wait tile!
                TileType[] waitSubHand = GetSubhand(sortedWaitingHand, (activeTileCount - 1), -1);
                WaitCandidateHand wch = new WaitCandidateHand(waitSubHand, calledMeldCount, false);
                wch.WaitA = lastTileLookedAt;
                GetWaitsBranch(waitList, wch, threeOfAKindTiles, threeInARowInHandTiles);
            }

            if (approvedReachKanTiles != null)
            {
                approvedReachKanTiles[0] = TileType.None;
                approvedReachKanTiles[1] = TileType.None;
                approvedReachKanTiles[2] = TileType.None;
                approvedReachKanTiles[3] = TileType.None;
            }

            // Done! If we don't have any waits then return null. Otherwise we need to go through and remove doubles.
            if (waitList.Count == 0)
            {
                waitList = null;
            }
            else
            {
                SortAndRemoveDoubles(waitList);
                SortAndRemoveDoubles(threeOfAKindTiles);
                SortAndRemoveDoubles(threeInARowInHandTiles);

                // Go through and add threeOfAKindTiles to approvedReachKanTiles IFF that tile isn't in threeInARowInHandTiles.
                if (approvedReachKanTiles != null)
                {
                    // Remove all tiles in threeInARowInHandTiles from threeOfAKindTiles.
                    for (int i = threeOfAKindTiles.Count - 1; i >= 0; --i)
                    {
                        TileType tile = threeOfAKindTiles[i];
                        foreach (TileType rowTile in threeInARowInHandTiles)
                        {
                            if (tile.IsEqual(rowTile))
                            {
                                threeOfAKindTiles.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    // Fill approvedReachKanTiles.
                    RiichiGlobal.Assert(threeOfAKindTiles.Count <= 4);
                    if (threeOfAKindTiles.Count > 0)
                    {
                        int reachKanSlot = 0;
                        foreach (TileType kanTile in threeOfAKindTiles)
                        {
                            if (!approvedReachKanTiles[reachKanSlot].IsEqual(kanTile))
                            {
                                approvedReachKanTiles[reachKanSlot++] = kanTile;
                            }
                        }
                    }
                }
            }

            // Done!
            g_stashedSortedWaitingHand = null;
            return waitList;
        }

        /**
         * Returns a complete hand regardless of ability to win on it or not. Just return the first result.
         */
        public static CandidateHand GetCompleteHand(Hand hand)
        {
            RiichiGlobal.Assert(hand.IsFullHand());

            List<CandidateHand> chBucket = new List<CandidateHand>();
            GetWinningHandCandidates(hand, chBucket, false);
            return (chBucket.Count == 0) ? null : chBucket[0];
        }

        /**
         * If we have a winning hand, then return the best winning hand. Otherwise, return null.
         * In the case of ron, the tile should be temporarily added to the hand in order to determine if it's
         * a winner. Should call the
         */
        public static CandidateHand GetWinningHand(Hand hand, bool fRon, bool fKokushiOnly)
        {
            RiichiGlobal.Assert(hand.IsFullHand());

            // Make sure we're not in furiten.
            if (fRon && hand.IsFuriten())
            {
                return null;
            }

            // Enumerate all the candidate hands!
            List<CandidateHand> chBucket = new List<CandidateHand>();
            GetWinningHandCandidates(hand, chBucket, fKokushiOnly);
            if (chBucket.Count == 0)
            {
                return null;
            }

            // Go through all the candidate hands and pick the one with the best score.
            // Note that at this step, we aren't taking into account riichi sticks or homba in our score evaluation.
            CandidateHand bestHand = null;
            int bestScore = 0;
            TsumoScore ts = fRon ? null : new TsumoScore();

            foreach (CandidateHand cHand in chBucket)
            {
                if (!cHand.Evaluate(hand, fRon))
                {
                    // Hand has no yaku.
                    continue;
                }

                int score = 0;
                bool limitDummy;
                if (fRon)
                {
                    score = RiichiScoring.GetScoreRon(hand.Parent.Settings, cHand.Han, cHand.Fu, hand.IsDealer(), out limitDummy);
                }
                else
                {
                    RiichiScoring.GetScoreTsumo(hand.Parent.Settings, cHand.Han, cHand.Fu, hand.IsDealer(), ts, out limitDummy);
                    score = ts.ScoreHi + (ts.ScoreLo * 2);
                }

                if (score > bestScore)
                {
                    bestHand = cHand;
                    bestScore = score;
                }
            }

            // Done!
            return bestHand;
        }

        private static void SortAndRemoveDoubles(List<TileType> list)
        {
            if (list != null)
            {
                list.Sort();
                for (int i = list.Count() - 1; i > 0; --i) // Iterate through size-1 to 1. Don't do slot 0.
                {
                    if (list[i].IsEqual(list[i - 1]))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }

        private static TileType[] GetSubhand(TileType[] baseHand, int slotA, int slotB)
        {
            TileType[] subHand = new TileType[baseHand.Length - ((slotA >= 0) ? 1 : 0) - ((slotB >= 0) ? 1 : 0)];
            int targetSlot = 0;
            for (int i = 0; i < baseHand.Length; ++i)
            {
                if ((i != slotA) && (i != slotB))
                {
                    subHand[targetSlot++] = baseHand[i];
                }
            }
            return subHand;
        }

        private static void GetWaitsBranch(List<TileType> resultsBox, WaitCandidateHand wHand, List<TileType> threeOfAKindTiles, List<TileType> threeInARowInHandTiles)
        {
            while (wHand.MeldCount < 4)
            {
                // Advance to the next tile.
                int slot = 0;
                while (wHand.Tiles[slot] == TileType.None)
                {
                    slot++;
                }

                // Are the first three tiles the same? If so, branch by taking that as the meld. Otherwise, are the first two
                // tiles the same and do we have a wildcard? If so, branch using the wildcard to make that a three of a kind.
                // Note that we don't want to do both, since that won't give us any new information.
                if (wHand.Tiles[slot].IsEqual(wHand.Tiles[slot + 1]))
                {
                    if (((slot + 2) < wHand.Tiles.Length) && wHand.Tiles[slot].IsEqual(wHand.Tiles[slot + 2]))
                    {
                        // Branch for a three of a kind.
                        GetWaitsBranch(resultsBox, wHand.Branch(slot), threeOfAKindTiles, threeInARowInHandTiles);
                    }
                    else if (wHand.Wildcard)
                    {
                        // Well, at least the first two tiles match. Branch using the wildcard.
                        // Cant add to threeOfAKindTiles here because that wouldn't ever be approved for kanning with.
                        GetWaitsBranch(resultsBox, wHand.Branch(slot, slot + 1), threeOfAKindTiles, threeInARowInHandTiles);
                    }
                }

                // Find the next three unique tiles. Can we make a run with the first two? Branch with that. Can we make
                // a run with all three? Update wHand and loop again.
                int slotB = -1;
                for (int i = slot + 1; i < wHand.Tiles.Length; ++i)
                {
                    if ((wHand.Tiles[i] != TileType.None) && !wHand.Tiles[i].IsEqual(wHand.Tiles[slot])) { slotB = i; break; }
                }

                int slotC = -1;
                if (slotB != -1)
                {
                    for (int i = slotB + 1; i < wHand.Tiles.Length; ++i)
                    {
                        if ((wHand.Tiles[i] != TileType.None) && !wHand.Tiles[i].IsEqual(wHand.Tiles[slotB])) { slotC = i; break; }
                    }
                }

                TileType tileA = wHand.Tiles[slot];
                TileType tileB = (slotB != -1) ? wHand.Tiles[slotB] : TileType.None;
                TileType tileC = (slotC != -1) ? wHand.Tiles[slotC] : TileType.None;

                if (tileA.GetSuit() == tileB.GetSuit())
                {
                    // If tileA and tileB are within two of each other (for a two-sided wait or a center wait) then branch with the wildcard.
                    if (wHand.Wildcard && (tileB.GetValue() - tileA.GetValue() <= 2))
                    {
                        GetWaitsBranch(resultsBox, wHand.Branch(slot, slotB), threeOfAKindTiles, threeInARowInHandTiles);
                    }

                    // If all the tiles are the same suit and they're next to each other, then continue on. Otherwise we want to
                    // break out of this test with an incomplete WaitCandidateHand to indicate failure - this hand is not in tempai.
                    if ((tileA.GetSuit() == tileC.GetSuit()) &&
                        tileA.IsNext(tileB) && tileB.IsNext(tileC))
                    {
                        wHand.ThreeInARowTiles.Add(wHand.Tiles[slot]);
                        wHand.ThreeInARowTiles.Add(wHand.Tiles[slotB]);
                        wHand.ThreeInARowTiles.Add(wHand.Tiles[slotC]);

                        wHand.Tiles[slot] = TileType.None;
                        wHand.Tiles[slotB] = TileType.None;
                        wHand.Tiles[slotC] = TileType.None;
                        wHand.MeldCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // If we find all four melds, add the results to the resultsBox. The results are the waits that the wildcard could have been.
            if (wHand.MeldCount == 4)
            {
                RiichiGlobal.Assert(!wHand.Wildcard, "Wait hand found but didn't use wildcard? 3ofKindTiles: " + wHand.ThreeInARowTiles.Count + " 3inRowTiles: " + wHand.ThreeInARowTiles.Count +
                                                     " WaitA: " + wHand.WaitA + " WaitB: " + wHand.WaitB + " Hand: " + OutputStashedHand());

                threeOfAKindTiles.AddRange(wHand.ThreeOfAKindTiles);
                threeInARowInHandTiles.AddRange(wHand.ThreeInARowTiles);
                resultsBox.Add(wHand.WaitA);

                if (wHand.WaitB != TileType.None)
                {
                    resultsBox.Add(wHand.WaitB);
                }
            }
        }

        private static string OutputStashedHand()
        {
            if (g_stashedSortedWaitingHand == null)
            {
                return "no hand?";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < g_stashedSortedWaitingHand.Length; ++i)
            {
                sb.Append(g_stashedSortedWaitingHand[i].ToString());
            }
            return sb.ToString();
        }

        private static void GetWinningHandCandidates_PairSubhand(Hand hand, List<CandidateHand> chBucket, TileType pairTile, TileType[] subHand)
        {
            // Check if the numbers for each suit are a multiple of three.
            bool fPass = true;
            int tileCount = hand.ActiveTileCount - 2;
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                if (suit.IsSuit())
                {
                    int suitCount = 0;
                    for (int i = 0; i < tileCount; ++i)
                    {
                        if (subHand[i].GetSuit() == suit)
                        {
                            suitCount++;
                        }
                    }
                    if ((suitCount % 3) != 0)
                    {
                        fPass = false;
                        break;
                    }
                }
            }

            // If we pass the previous test, go ahead and process the candidate.
            if (fPass)
            {
                GetWinningHandCandidates_ProcessCandidate(hand, chBucket, new StandardCandidateHand(pairTile, false), subHand, 0);
            }
        }

        /**
         * Returns a list of all the potential winning hands.
         * We'll just return an array of all the candidate hands, but yeah.
         * Parameter is a bucket that will have all the candidate hands put into it.
         */
        private static void GetWinningHandCandidates(Hand hand, List<CandidateHand> chBucket, bool fKokushiOnly)
        {
            RiichiGlobal.Assert(hand.IsFullHand());

            // Get a copy of the active hand and sort it.
            int activeTileCount = hand.ActiveTileCount;
            TileType[] sourceActiveHand = hand.ActiveHand;
            TileType[] handCopy = new TileType[activeTileCount];
            for (int i = activeTileCount - 1; i >= 0; --i)
            {
                handCopy[i] = sourceActiveHand[i];
            }
            Array.Sort(handCopy);

            // See if this is a Thirteen Orphans or Thirteen Broken hand.
            if (Yaku.KokushiMusou.Evaluate(hand, null, false) != 0)
            {
                chBucket.Add(new ThirteenHand(Yaku.KokushiMusou));
                return; // Don't need to look at anything else.
            }

            // See if this is a Thirteen Broken hand.
            if (!fKokushiOnly && (Yaku.ShiisanBudou.Evaluate(hand, null, false) != 0))
            {
                chBucket.Add(new ThirteenHand(Yaku.ShiisanBudou));
                return; // Don't need to look at anything else.
            }

            if (Yaku.ShiisuuPuuta.Evaluate(hand, null, false) != 0)
            {
                chBucket.Add(new FourteenHand());
                return; // Don't need to look at anything else.
            }

            // First, determine if this hand can be a seven pairs hand.
            // If so, add a candidate hand for the seven pairs case.
            if (!fKokushiOnly && (hand.GetCalledMeldCount() == 0))
            {
                // Go through and make sure we have a bunch of pairs, and that we don't have the same pair twice.
                bool validHand = true;
                bool fDisallowMultiPair = !hand.Parent.Settings.GetSetting<bool>(GameOption.SameTileChiitoi);

                for (int i = 0; i < 7; ++i)
                {
                    // If i*2+0 and i*2+1 differ, then we don't have a pair here. Pairs will align.
                    // Also if we have two pairs of the same type, then this won't work either.
                    if ((!handCopy[i * 2 + 0].IsEqual(handCopy[i * 2 + 1])) ||
                        (fDisallowMultiPair && ((i > 0) && handCopy[i * 2].IsEqual(handCopy[(i - 1) * 2]))))
                    {
                        validHand = false;
                        break;
                    }
                }

                if (validHand)
                {
                    SevenPairsCandidateHand chHand = new SevenPairsCandidateHand();
                    for (int i = 0; i < 7; ++i)
                    {
                        chHand.PairTiles[i].Tile = handCopy[i * 2];
                    }
                    chHand.ExpandAndInsert(chBucket, hand);
                }
            }

            // Evalulate all possible standard hands.
            // Find all pairs in the hand, and then spawn off candidate detection for each pair.
            if (!fKokushiOnly)
            {
                TileType lastPairTile = TileType.None;
                for (int i = 1; i < activeTileCount; ++i)
                {
                    if ((lastPairTile != handCopy[i]) && handCopy[i].IsEqual(handCopy[i - 1]))
                    {
                        TileType[] unPairHand = GetSubhand(handCopy, i, i - 1);
                        lastPairTile = handCopy[i];
                        GetWinningHandCandidates_PairSubhand(hand, chBucket, handCopy[i], unPairHand);
                        ++i; // Add 1 to i here so that for the next step it's essentially i += 2.
                    }
                }
            }
        }

        private static void GetWinningHandCandidates_ProcessCandidate(Hand hand, List<CandidateHand> chBucket, StandardCandidateHand chHand, TileType[] subHand, int startMeld)
        {
            int tileCount = hand.ActiveTileCount - 2; // Discluding the pair.
            int meld = startMeld;
            bool fSuccess = true;

            while ((meld * 3) < tileCount)
            {
                // Find the place to start from. Grab the first tile.
                int slot = 0;
                while (subHand[slot] == TileType.None)
                {
                    ++slot;
                }

                // Check to see if we can make a triplet. Are the next two tiles the same tile? If so,
                // split off into another candidate. Note that we don't need to check for array bounds
                // overflow because are guaranteed to have 3 tiles still in there.
                if ((subHand[slot].IsEqual(subHand[slot + 1]) && subHand[slot].IsEqual(subHand[slot + 2])))
                {
                    StandardCandidateHand chHandDup = (StandardCandidateHand)chHand.Clone();
                    TileType[] dupHand = (TileType[])subHand.Clone();
                    chHandDup.Melds[meld].Tiles[0].Tile = dupHand[slot];
                    chHandDup.Melds[meld].Tiles[1].Tile = dupHand[slot];
                    chHandDup.Melds[meld].Tiles[2].Tile = dupHand[slot];
                    chHandDup.Melds[meld].State = MeldState.Pon;
                    dupHand[slot]     = TileType.None;
                    dupHand[slot + 1] = TileType.None;
                    dupHand[slot + 2] = TileType.None;
                    GetWinningHandCandidates_ProcessCandidate(hand, chBucket, chHandDup, dupHand, meld + 1);
                }

                // Try to find a sequence. First we'll use the tile at "slot". Then we need to find the next non-empty tile, and check if it's the next one.
                chHand.Melds[meld].Tiles[0].Tile = subHand[slot];
                subHand[slot] = TileType.None;
                slot++;

                // Get the next tile.
                while ((slot < subHand.Length) && ((subHand[slot] == TileType.None) || subHand[slot].IsEqual(chHand.Melds[meld].Tiles[0].Tile)))
                {
                    ++slot;
                }
                if ((slot < subHand.Length) && chHand.Melds[meld].Tiles[0].Tile.IsNext(subHand[slot]))
                {
                    chHand.Melds[meld].Tiles[1].Tile = subHand[slot];
                    subHand[slot] = TileType.None;
                    slot++;
                }
                else
                {
                    // Failed.
                    fSuccess = false;
                    break;
                }

                // Get the last tile.
                while ((slot < subHand.Length) && ((subHand[slot] == TileType.None) || subHand[slot].IsEqual(chHand.Melds[meld].Tiles[1].Tile)))
                {
                    ++slot;
                }
                if ((slot < subHand.Length) && chHand.Melds[meld].Tiles[1].Tile.IsNext(subHand[slot]))
                {
                    chHand.Melds[meld].Tiles[2].Tile = subHand[slot];
                    subHand[slot] = TileType.None;
                }
                else
                {
                    // Failed.
                    fSuccess = false;
                    break;
                }

                // Succeeded! Note the meld state and advance to the next meld.
                chHand.Melds[meld].State = MeldState.Chii;
                ++meld;
            }

            // Add this to the list of candidate hands!
            if (fSuccess)
            {
                chHand.ExpandAndInsert(chBucket, hand);
            }
        }
    }
}
