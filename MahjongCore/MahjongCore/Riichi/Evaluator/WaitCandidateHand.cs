// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    // This class represents a hand that could be made, with one wildcard having been used. This can be used to determine the waits of our current hand.
    internal class WaitCandidateHand : CandidateHand
    {
        // Members
        private bool           Wildcard;              // If true, we still have a wildcard we can use.
        private TileType       WaitA = TileType.None; // The tile that this candidate hand has shown us to be waiting on.
        private TileType       WaitB = TileType.None; // The other tile this candidate hand has shown us to be waiting on. Can happen if we have, say, a 2-3 and we use the wildcard on that meld.
        private int            MeldCount;             // Amount of melds that this candidate hand has already accounted for. The pair is implicitly counted for. Tiles[] should then be of length 14-meldCount*3-2 (the -2 is for the pair)
        private TileType[]     Tiles;                  // List of tiles that have not been assigned yet.
        private List<TileType> ThreeOfAKindTiles = new List<TileType>();
        private List<TileType> ThreeInARowTiles  = new List<TileType>();

        public WaitCandidateHand(TileType[] tiles, int meldCount, bool wildcard)
        {
            Tiles = tiles;
            MeldCount = meldCount;
            Wildcard = wildcard;
        }

        public WaitCandidateHand(TileType[] tiles, int meldCount, bool wildcard, List<TileType> threeKind, List<TileType> threeRow)
        {
            Tiles = tiles;
            MeldCount = meldCount;
            Wildcard = wildcard;
            ThreeOfAKindTiles.AddRange(threeKind);
            ThreeInARowTiles.AddRange(threeRow);
        }

        /**
         * Branch by taking up a three of a kind, starting at the slot. Based on some of our heuristics, we know that if this is an option, they
         * will all be next to each other in the array. Increase the meld count in the branch, NO_TILE out the slots, and copy over waits and wildcard.
         */
        public WaitCandidateHand Branch(int slotStart)
        {
            Global.Assert(Tiles.Length >= 3);
            Global.Assert(MeldCount < 4);
            Global.Assert(slotStart + 2 < Tiles.Length);
            Global.Assert(Tiles[slotStart].IsEqual(Tiles[slotStart + 1]));
            Global.Assert(Tiles[slotStart].IsEqual(Tiles[slotStart + 2]));
            Global.Assert(Tiles[slotStart].IsTile());

            TileType[] subTiles = (TileType[])Tiles.Clone();
            TileType tile = Tiles[slotStart];
            subTiles[slotStart + 0] = TileType.None;
            subTiles[slotStart + 1] = TileType.None;
            subTiles[slotStart + 2] = TileType.None;

            WaitCandidateHand bHand = new WaitCandidateHand(subTiles, (MeldCount + 1), Wildcard, ThreeOfAKindTiles, ThreeInARowTiles);
            bHand.WaitA = WaitA;
            bHand.WaitB = WaitB;

            // Tile is of a three of a kind, all in the hand. So we can potentially kan with it.
            bHand.ThreeOfAKindTiles.Add(tile);
            return bHand;
        }

        /**
         * Branch and make use of the wildcard. This sets the two tiles we use for this to NO_TILE and counts them in the meld. It will also set
         * WaitA and WaitB by looking at the types of tiles at slotA and slotB. They should have already been checked to make sure a meld is viable.
         */
        public WaitCandidateHand Branch(int slotA, int slotB)
        {
            Global.Assert(Wildcard);
            Global.Assert(Tiles.Length >= 2);
            Global.Assert(MeldCount < 4);

            TileType[] subTiles = (TileType[])Tiles.Clone();
            subTiles[slotA] = TileType.None;
            subTiles[slotB] = TileType.None;
            WaitCandidateHand bHand = new WaitCandidateHand(subTiles, (MeldCount + 1), false, ThreeOfAKindTiles, ThreeInARowTiles);

            // Figure out the waits.
            TileType tileA = Tiles[slotA];
            TileType tileB = Tiles[slotB];
            if (tileA.IsEqual(tileB))
            {
                bHand.WaitA = tileA.GetNonRedDoraVersion();
                // Don't add to ThreeOfAKind here because they couldn't participate in a closed reach kan anyway.
            }
            else
            {
                Global.Assert(tileA.GetSuit() == tileB.GetSuit());
                if (tileA.IsNext(tileB))
                {
                    // Two sided wait. See if either works.
                    int lValue = tileA.GetValue();
                    if (lValue > 1)
                    {
                        bHand.WaitA = tileA.GetPrev();
                    }

                    if (lValue < 8)
                    {
                        if (bHand.WaitA == TileType.None)
                        {
                            bHand.WaitA = tileB.GetNext();
                        }
                        else
                        {
                            bHand.WaitB = tileB.GetNext();
                        }
                    }
                }
                else
                {
                    // Center wait.
                    Global.Assert(tileB.GetValue() == (tileA.GetValue() + 2));
                    bHand.WaitA = tileA.GetNext();
                }

                // Add to ThreeInARow, since these tiles can't be used in a closed kan now.
                bHand.ThreeInARowTiles.Add(tileA);
                bHand.ThreeInARowTiles.Add(tileB);
            }

            // Return the branched hand.
            return bHand;
        }
    }
}
