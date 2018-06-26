// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Common;
using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace MahjongCore.Riichi
{
    public class HandImpl : IHand
    {
        // IHand
        public IGameState      Parent                { get; internal set; }
        public Player          Player                { get; internal set; }
        public Wind            Seat                  { get { return WindExtensionMethods.GetWind(Player, Parent.Dealer); } }
        public ITile[]         ActiveHand            { get { return ActiveHandRaw; } }
        public IMeld[]         Melds                 { get { return MeldsRaw; } }
        public IList<ITile>    Discards              { get { return (IList<ITile>)DiscardsImpl; } }
        public IList<TileType> Waits                 { get; internal set; }
        public IList<ICommand> DrawsAndKans          { get; internal set; }
        public IList<IMeld>    AvailableCalls        { get; internal set; }
        public int             Score                 { get; internal set; } = 0;
        public int             ActiveTileCount       { get; internal set; } = 0;
        public int             Streak                { get; internal set; } = 0;
        public int             MeldCount             { get { return (MeldsRaw[0].State.IsCalled() ? 1 : 0) + (MeldsRaw[1].State.IsCalled() ? 1 : 0) + (MeldsRaw[2].State.IsCalled() ? 1 : 0) + (MeldsRaw[3].State.IsCalled() ? 1 : 0); } }
        public int             MeldedTileCount       { get { return MeldsRaw[0].State.GetTileCount() + MeldsRaw[1].State.GetTileCount() + MeldsRaw[2].State.GetTileCount() + MeldsRaw[3].State.GetTileCount(); } }
        public int             KanCount              { get { return (MeldsRaw[0].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[1].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[2].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[3].State.GetMeldType() == MeldType.Kan ? 1 : 0); } }
        public bool            Dealer                { get { return Player == Parent.Dealer; } }
        public bool            Open                  { get { return MeldsRaw[0].State.IsOpen() || MeldsRaw[1].State.IsOpen() || MeldsRaw[2].State.IsOpen() || MeldsRaw[3].State.IsOpen(); } }
        public bool            Closed                { get { return !Open; } }
        public bool            Tempai                { get; internal set; }
        public bool            InReach               { get; internal set; }
        public bool            InDoubleReach         { get; internal set; }
        public bool            InOpenReach           { get; internal set; }
        public bool            Furiten               { get; internal set; } = false;
        public bool            Yakitori              { get; internal set; } = true;
        public bool            HasFullHand           { get; internal set; }
        public bool            CouldIppatsu          { get; internal set; }
        public bool            CouldDoubleReach      { get; internal set; }
        public bool            CouldKyuushuuKyuuhai  { get; internal set; }
        public bool            CouldSuufurendan      { get; internal set; }

        public int GetTileSlot(TileType tile, bool matchRed)
        {
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                if (ActiveHandRaw[i].Type.IsEqual(tile, matchRed)) { return i; }
            }
            return -1;
        }

        public void MoveTileToEnd(TileType targetTile)
        {

        }

        public void ReplaceTiles(List<TileType> tilesRemove, List<TileType> tilesAdd)
        {
            CommonHelpers.Check((tilesRemove.Count == tilesAdd.Count), ("Count of tiles to add must add count of tiles to remove. Remove: " + tilesRemove.Count + " add: " + tilesAdd.Count));

            // Remove from the hand one of each tile in tilesRemove.
            foreach (TileType ripTile in tilesRemove)
            {
                bool fFound = false;
                for (int iHandTile = 0; iHandTile < ActiveTileCount; ++iHandTile)
                {
                    if (ripTile.IsEqual(ActiveHandRaw[iHandTile].Type, true))
                    {
                        ActiveHand[iHandTile] = ActiveHand[ActiveTileCount - 1];
                        ActiveHand[ActiveTileCount - 1] = TileType.None;
                        --ActiveTileCount;

                        fFound = true;
                        break;
                    }
                }
                Global.Assert(fFound);
            }

            // Add to the hand one of each tile in tilesAdd. We'll need to grab that tile from somewhere else on the board. Look
            // through the wall first (excluding flipped dora tiles), then other players' hands, then discards, and then the dora
            // tile. Replace the tile with a corresponding tile from tilesRemove.
            foreach (TileType addTile in tilesAdd)
            {
                // Add the tile to the hand.
                ActiveHand[ActiveTileCount++] = addTile;
                // TODO: Find this tile somewhere else on the board and replace it with tilesRemove[iTileAdd].
                // TODO: maybe update DrawsAndKans as well...
            }

            // Sort the hand. This will also go into MahjongHand and update it's tiles there.
            Sort(false);
        }

        public IList<TileType> GetWaitsForDiscard(int slot)
        {
            return null;
        }

        // HandImpl
        internal ICandidateHand WinningHandCache    { get; set; } = null;
        internal IMeld          CachedCall          { get; set; } = null;
        internal List<TileImpl> DiscardsImpl        { get; set; } = new List<TileImpl>();
        internal TileImpl[]     ActiveHandRaw       { get; set; } = new TileImpl[TileHelpers.HAND_SIZE];
        internal MeldImpl[]     MeldsRaw            { get; set; } = new MeldImpl[] { new MeldImpl(), new MeldImpl(), new MeldImpl(), new MeldImpl() };
        internal bool           OverrideNoReachFlag { get; set; } = false;

        private List<TileType>[]   ActiveTileWaits       { get; private set; } = new List<TileType>[TileHelpers.HAND_SIZE];
        private Stack<TileCommand> DrawsAndKans          { get; private set; } = new Stack<TileCommand>();

        private bool               HasTemporaryTile      = false;
        private List<TileType>     WaitTiles             = new List<TileType>();
        private TileType[]         ActiveRiichiKanTiles  = null;
        private TileType[][]       RiichiKanTilesPerSlot = new TileType[][] { new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                              new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                              new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                              new TileType[4], new TileType[4] };

        public bool IsFullHand()                     { return ((GetCalledMeldCount() * 3) + ActiveTileCount) == TileHelpers.HAND_SIZE; }
        public bool IsFuriten()                      { return Parent.Settings.GetSetting<bool>(GameOption.Furiten) && Furiten; }
        public bool IsWaramePlayer()                 { return Player == Parent.WaremePlayer; }
        public bool IsInReach()                      { return Parent.IsInReach(Player); }
        public bool IsInDoubleReach()                { return Parent.IsInDoubleReach(Player); }
        public bool IsIppatsu()                      { return Parent.GetIppatsu(Player); }
        public bool IsFourKans()                     { return GetKanCount() == 4; }
        public List<TileType> GetTileWaits(int slot) { return ActiveTileWaits[(slot == -1) ? (ActiveTileCount - 1) : slot]; }
        public int GetClosedKanFlippedTileCount()    { return OpenMeld[0].State.GetFlippedTileCount() + OpenMeld[1].State.GetFlippedTileCount() + OpenMeld[2].State.GetFlippedTileCount() + OpenMeld[3].State.GetFlippedTileCount(); }
        public bool IsInOpenReach()                  { return Parent.IsInOpenReach(Player); }
        public List<TileType> GetWaits()             { return CommonHelpers.SafeCopyByValue(WaitTiles); } // Make a copy, so the caller can't mess with our waits.
        public List<IMeld> GetCalls()                { return RiichiHandHelpers.GetCalls(this, Parent); }
        public ICommand PeekLastDrawKan()            { return DrawsAndKans.Peek(); }

        internal HandImpl(IGameState parent, Player p, int score)
        {
            Parent               = parent;
            Player               = p;
            Score                = score;
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];

            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i)
            {
                ActiveHand[i] = TileType.None;
                ActiveTileWaits[i] = new List<TileType>();
            }
        }

        internal void Reset()
        {
            asdfasdf
            foreach (MeldImpl meld in hand.MeldsRaw) { meld.Reset(); }
        }

        internal void Rebuild()
        {
            // Gets called after loading from a save state and fields like score, streak, yakitori, hands, calls, and discards
            // have been set. This method should reconstruct everything else like furiten, waits, ippatsu flags, etc.

            CouldIppatsu = DetermineIppatsu(state, player, pickedTileCount);
            Furiten = DetermineFuriten(hand);


            ActiveRiichiKanTiles = new TileType[4];
            WaitTiles = HandEvaluator.GetWaits(this, -1, ActiveRiichiKanTiles);

            DrawsAndKans.Clear();
            StringBuilder sb = new StringBuilder();
            sb.Append("Loading from state, players d&ks, p: " + Player + " list: ");
            foreach (TileCommand tc in values.DrawsAndKans)
            {
                sb.Append(tc.TilePrimary.Tile + " ");
                DrawsAndKans.Push(tc);
            }
            Global.Log(sb.ToString());

            RebuildFuriten();
            RebuildIppatsu();
        }

        public bool IsTempai()
        {
            bool fTempai = false;
            if (IsFullHand())
            {
                // We haven't populated WaitTiles yet. In this case, we're tempai if anything in ActiveTileWaits is non-null.
                if (ActiveTileWaits != null)
                {
                    for (int i = 0; i < ActiveTileWaits.Length; ++i)
                    {
                        if (ActiveTileWaits[i] != null)
                        {
                            fTempai = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                fTempai = (WaitTiles != null) && (WaitTiles.Count > 0);
            }
            return fTempai;
        }

        public IMeld GetPonMeld(TileType tile)
        {
            foreach (Meld meld in OpenMeld)
            {
                if ((meld.State == MeldState.Pon) && (meld.Tiles[0].Tile.IsEqual(tile)))
                {
                    return meld;
                }
            }
            return null;
        }

        internal MeldImpl GetLatestMeld()
        {
            return (OpenMeld[3].State != MeldState.None) ? OpenMeld[3] :
                   (OpenMeld[2].State != MeldState.None) ? OpenMeld[2] :
                   (OpenMeld[1].State != MeldState.None) ? OpenMeld[1] :
                   (OpenMeld[0].State != MeldState.None) ? OpenMeld[0] : null;
        }

        public Meld GetNextEmptyMeld()
        {
            return (OpenMeld[0].State == MeldState.None) ? OpenMeld[0] :
                   (OpenMeld[1].State == MeldState.None) ? OpenMeld[1] :
                   (OpenMeld[2].State == MeldState.None) ? OpenMeld[2] :
                   (OpenMeld[3].State == MeldState.None) ? OpenMeld[3] : null;
        }

        public void AddTemporaryTile(TileType tile)
        {
            Global.Assert(!HasTemporaryTile);

            ActiveHand[ActiveTileCount] = tile;
            ActiveTileCount++;
            HasTemporaryTile = true;
        }

        public void RemoveTemporaryTile()
        {
            Global.Assert(HasTemporaryTile);
            Global.Assert(IsFullHand());

            ActiveTileCount--;
            ActiveHand[ActiveTileCount] = TileType.None;
            HasTemporaryTile = false;
        }

        public void Clear()
        {
            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i) { ActiveHand[i] = TileType.None; }
            for (int i = 0; i < 4; ++i)                     { OpenMeld[i].Reset(); }

            ActiveTileCount = 0;
            WaitTiles = null;
            StoredCallOption = null;
            Parent.HandCleared(Player);
            DrawsAndKans.Clear();
        }

        public bool CanClosedKanWithTile(TileType tile)
        {
            // Ensure that we have four tiles in rHand with the value of Tile.
            int nTileCount = 0;
            foreach (TileType handTile in ActiveHand)
            {
                nTileCount += (handTile.IsEqual(tile)) ? 1 : 0;
            }
            bool fClosedKan = (nTileCount == 4);

            // If we're in reach, make sure the type is equal to one of the approved reach kan tiles.
            if (IsInReach())
            {
                fClosedKan = (Parent.Settings.GetSetting<bool>(GameOption.KanAfterRiichi) &&
                              ((ActiveRiichiKanTiles[0].IsEqual(tile)) || (ActiveRiichiKanTiles[1].IsEqual(tile)) ||
                               (ActiveRiichiKanTiles[2].IsEqual(tile)) || (ActiveRiichiKanTiles[3].IsEqual(tile))));
            }
            return fClosedKan;
        }

        public bool CanDoubleReach()
        {
            // Check to make sure this is our first discard and that no other discards have been made that are calls. Just... check all of the discard piles.
            bool fCanDoubleReach = true;
            foreach (Player p in PlayerExtensionMethods.Players)
            {
                List<ExtendedTile> discards = Parent.GetDiscards(p);
                if ((discards.Count == 0) || ((p != Player) && (discards.Count == 1) && !discards[0].Called))
                {
                    fCanDoubleReach = true;
                    break;
                }
            }
            return fCanDoubleReach;
        }

        public bool CanKyuushuuKyuuhai()
        {
            // Make sure we haven't discarded anything yet.
            bool fCanKyuushuuKyuuhai = (Discards.Count == 0);

            // Make sure noone has made a call.
            for (int i = 0; fCanKyuushuuKyuuhai && (i < 4); ++i)
            {
                fCanKyuushuuKyuuhai = (Parent.GetHand(PlayerExtensionMethods.Players[i]).OpenMeld[0].State == MeldState.None);
            }

            // Make sure we have 9 unique terminals and honors.
            if (fCanKyuushuuKyuuhai)
            {
                bool fCircle1 = false;
                bool fCircle9 = false;
                bool fCharacter1 = false;
                bool fCharacter9 = false;
                bool fBamboo1 = false;
                bool fBamboo9 = false;
                bool fNorth = false;
                bool fWest = false;
                bool fSouth = false;
                bool fEast = false;
                bool fHaku = false;
                bool fChun = false;
                bool fHatsu = false;

                foreach (TileType tile in ActiveHand)
                {
                    fCircle1    |= (tile.IsEqual(TileType.Circles1));
                    fCircle9    |= (tile.IsEqual(TileType.Circles9));
                    fCharacter1 |= (tile.IsEqual(TileType.Characters1));
                    fCharacter9 |= (tile.IsEqual(TileType.Characters9));
                    fBamboo1    |= (tile.IsEqual(TileType.Bamboo1));
                    fBamboo9    |= (tile.IsEqual(TileType.Bamboo9));
                    fNorth      |= (tile.IsEqual(TileType.North));
                    fEast       |= (tile.IsEqual(TileType.East));
                    fSouth      |= (tile.IsEqual(TileType.South));
                    fWest       |= (tile.IsEqual(TileType.West));
                    fHaku       |= (tile.IsEqual(TileType.Haku));
                    fChun       |= (tile.IsEqual(TileType.Chun));
                    fHatsu      |= (tile.IsEqual(TileType.Hatsu));
                }

                int nCount = (fCircle1    ? 1 : 0) + (fCircle9    ? 1 : 0) +
                             (fCharacter1 ? 1 : 0) + (fCharacter9 ? 1 : 0) +
                             (fBamboo1    ? 1 : 0) + (fBamboo9    ? 1 : 0) +
                             (fNorth      ? 1 : 0) + (fWest       ? 1 : 0) +
                             (fSouth      ? 1 : 0) + (fEast       ? 1 : 0) +
                             (fHaku       ? 1 : 0) + (fChun       ? 1 : 0) +
                             (fHatsu      ? 1 : 0);
                fCanKyuushuuKyuuhai = (nCount >= 9);
            }
            return fCanKyuushuuKyuuhai;
        }

        public TileType GetSuufurendanTile()
        {
            // Make sure the other three players have all discarded once and have made no calls. Gather the first discard they have all made.
            bool fSuufurendan = true;
            List<ExtendedTile> firstTiles = new List<ExtendedTile>();
            for (int i = 0; fSuufurendan && (i < 4); ++i)
            {
                Player p = PlayerExtensionMethods.Players[i];
                Hand hand = Parent.GetHand(p);
                List<ExtendedTile> playerDiscards = hand.Discards;

                if (hand.Player != Player)
                {
                    fSuufurendan = (playerDiscards.Count == 1) && (hand.OpenMeld[0].State == MeldState.None);
                    if (fSuufurendan)
                    {
                        firstTiles.Add(playerDiscards[0]);
                    }
                }
                else
                {
                    fSuufurendan = (playerDiscards.Count == 0) && (hand.OpenMeld[0].State == MeldState.None);
                }
            }

            // Make sure all the tiles the other three have discarded are all the same tile.
            fSuufurendan &= (firstTiles.Count == 3) && firstTiles[0].Tile.IsEqual(firstTiles[1].Tile) && firstTiles[1].Tile.IsEqual(firstTiles[2].Tile);

            // Make sure we have the tile in question. If so, return that.
            TileType suufurendanTile = TileType.None;
            if (fSuufurendan)
            {
                for (int i = 0; i < ActiveTileCount; ++i)
                {
                    if (ActiveHand[i].IsEqual(firstTiles[0].Tile))
                    {
                        suufurendanTile = ActiveHand[i];
                        break;
                    }
                }
            }
            return suufurendanTile;
        }

        public int GetSlot(TileType tile, bool fMatchRed)
        {
            int slot = -1;
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                if (ActiveHand[i].IsEqual(tile) && (!fMatchRed || (tile.IsRedDora() == ActiveHand[i].IsRedDora())))
                {
                    slot = i;
                    break;
                }
            }
            return slot;
        }

        public void Sort(bool fAnimation)
        {
            // Sort the tiles. Any unset tiles will end up at the end.
            Array.Sort(ActiveHand);

            // Sort the tile view.
            Parent.HandSort(Player, fAnimation);
        }

        public void AddTile(TileType tile, bool rewind = false)
        {
            ActiveHand[ActiveTileCount++] = tile;

            if (!rewind)
            {
                DrawsAndKans.Push(new TileCommand(TileCommand.Type.Tile, new ExtendedTile(tile)));
                Global.Log("Pushed onto drawsnkans! Player: " + Player + " tile: " + tile + " new drawsnkans count: " + DrawsAndKans.Count);
            }
        }

        public void RewindDiscardTile(TileType discardedTile)
        {
            // Add the tile at the end.
            AddTile(discardedTile, true);

            // Determine what the next tile to be placed into the wall should be.
            // TODO: Do something different if the player previously called.
            TileType prevDrawnTile = Parent.Wall[TileHelpers.ClampTile(Parent.Offset + (122 - (Parent.TilesRemaining + 1)))];

            // If we have that tile in our hand, move it to the end.
            for (int i = 0; i < (ActiveTileCount - 1); ++i) 
            {
                if (ActiveHand[i].IsEqual(prevDrawnTile) && (ActiveHand[i].IsRedDora() == prevDrawnTile.IsRedDora()))
                {
                    ActiveHand[i] = ActiveHand[ActiveTileCount - 1];
                    ActiveHand[ActiveTileCount - 1] = prevDrawnTile;
                    break;
                }
            }

            // Now sync our hand so it doesn't look like we picked up our discard, but rather we picked up our draw.
            Parent.HandSort(Player, false);
        }

        public bool RewindAddTile(TileType targetTile)
        {
            // Find the tile. Start backwards.
            for (int i = ActiveTileCount - 1; i >= 0; --i)
            {
                if (ActiveHand[i].IsEqual(targetTile) && (ActiveHand[i].IsRedDora() == targetTile.IsRedDora()))
                {
                    ActiveHand[i] = TileType.None;
                    ActiveTileCount--;
                    Sort(false);

                    Global.Assert(targetTile == DrawsAndKans.Peek().TilePrimary.Tile);
                    DrawsAndKans.Pop();
                    return true;
                }
            }
            return false;
        }

        public void ForceSetLastTile(TileType targetTile)
        {
            Global.Assert(targetTile != TileType.None);

            // Find where the target tile is in the hand. Don't need to do anything if it's already at the end.
            bool fFound = false;
            for (int i = 0; i < (ActiveTileCount - 1); ++i)
            {
                if (ActiveHand[i].IsEqual(targetTile) && (ActiveHand[i].IsRedDora() == targetTile.IsRedDora()))
                {
                    ActiveHand[i] = TileType.None;
                    Array.Sort(ActiveHand);
                    ActiveHand[ActiveTileCount - 1] = targetTile;

                    fFound = true;
                    break;
                }
            }
            Global.Assert(fFound);

            // Don't sort the hand, but sync the visual representation of it.
            Parent.HandSort(Player, false);
        }


        public void AbortiveDraw(int slot, bool fDiscard)
        {
            int targetSlot = slot;
            TileType tile = TileType.None;

            if (fDiscard)
            {
                targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
                tile = ActiveHand[targetSlot];

                ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
                ActiveHand[ActiveTileCount - 1] = TileType.None;
                ActiveTileCount--;
                Sort(false);
            }
            Parent.HandPerformedAbortiveDraw(Player, tile, targetSlot);
        }

        public void Discard(int slot)
        {
            int targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
            TileType tile = ActiveHand[targetSlot];

            ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            ActiveTileCount--;
            Sort(false);

            WinningHandCache = null;

            // Update our waits unless we're in reach.
            if (!IsInReach())
            {
                UpdateWaitTiles(targetSlot);
            }

            Parent.HandPerformedDiscard(tile, targetSlot);
        }

        public void Reach(int slot, bool fOpenReach)
        {
            int targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
            TileType tile = ActiveHand[targetSlot];

            ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            ActiveTileCount--;
            Sort(false);

            UpdateWaitTiles(targetSlot);
            WinningHandCache = null;
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[targetSlot];

            Parent.HandPerformedReach(tile, targetSlot, fOpenReach);
        }

        public void Kan(int slot, bool fPromoted)
        {
            // Remove the tile from the hand.
            TileType tile = ActiveHand[slot];
            ActiveHand[slot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            --ActiveTileCount;

            if (fPromoted)
            {
                // Update a meld to a promoted kan.
                Meld meld = GetPonMeld(tile);
                meld.State = MeldState.KanPromoted;
                meld.Tiles[3].Tile = tile;

                DrawsAndKans.Push(new TileCommand(TileCommand.Type.PromotedKan, new ExtendedTile(tile.GetNonRedDoraVersion())));
            }
            else
            {
                Meld meld = GetNextEmptyMeld();
                meld.State = MeldState.KanConcealed;
                meld.Tiles[3].Tile = tile;

                int tileInsert = 0;
                for (int i = 0; i < ActiveTileCount; ++i)
                {
                    if (ActiveHand[i].IsEqual(tile))
                    {
                        meld.Tiles[tileInsert++].Tile = ActiveHand[i];
                        ActiveHand[i] = ActiveHand[ActiveTileCount - 1];
                        ActiveHand[ActiveTileCount - 1] = TileType.None;
                        ActiveTileCount--;

                        // Need to reevaluate the slot we just evaluated in the case that it
                        // was one of the tiles we wanted. Otherwise, we'll skip over it.
                        i--;
                    }
                }

                meld.SortMeldTilesForClosedKan();
                DrawsAndKans.Push(new TileCommand(TileCommand.Type.ClosedKan, new ExtendedTile(tile.GetNonRedDoraVersion())));
            }

            Sort(false);

            // Update our waits, unless we're in reach. This is so we can rinshan.
            if (!IsInReach())
            {
                if (fPromoted)
                {
                    UpdateWaitTiles(slot);
                }
                else
                {
                    // Closed kan can break everything. Just recalculate from scratch.
                    WaitTiles = HandEvaluator.GetWaits(this, -1, null);
                }
            }

            Parent.HandPerformedKan(Player, slot, tile, (fPromoted ? KanType.Promoted : KanType.Concealed));
        }

        public void PerformStoredCall()
        {
            // Remove tiles from ActiveHand by just setting tiles to no tile. We'll sort after.
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                if ((i == StoredCallOption.SlotA) || (i == StoredCallOption.SlotB) || (i == StoredCallOption.SlotC))
                {
                    ActiveHand[i] = TileType.None;
                }
            }
            ActiveTileCount -= (StoredCallOption.Type.GetTileCount() - 1);
            Sort(false);

            // Create the meld.
            Meld meld = GetNextEmptyMeld();
            meld.State = StoredCallOption.Type;
            meld.Tiles[0].Tile = StoredCallOption.TileA;
            meld.Tiles[1].Tile = StoredCallOption.TileB;
            meld.Tiles[2].Tile = StoredCallOption.TileC;
            meld.Tiles[3].Tile = (StoredCallOption.Type == MeldState.KanOpen) ? StoredCallOption.TileD : TileType.None;

            CalledDirection direction = Player.GetTargetPlayerDirection(Parent.CurrentPlayer);
            Global.Assert(direction != CalledDirection.None);
            if (direction == CalledDirection.Left)        { meld.Tiles[0].Called = true; }
            else if (direction == CalledDirection.Across) { meld.Tiles[1].Called = true; }
            else if (direction == CalledDirection.Right)  { meld.Tiles[meld.State.GetTileCount() - 1].Called = true; }

            StoredCallOption = null;
        }

        public bool CanTsumo()
        {
            bool fPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                             (Parent.PrevAction == GameAction.Pon) ||
                             (Parent.PrevAction == GameAction.OpenKan) ||
                             (Parent.PrevAction == GameAction.PromotedKan) ||
                             (Parent.PrevAction == GameAction.ClosedKan);

            TileType pickedTile = ActiveHand[ActiveTileCount - 1];
            bool fTsumo = false;
            bool waitFound = false;

            if ((WaitTiles != null) && !fPrevCall)
            {
                foreach (TileType waitTile in WaitTiles)
                {
                    if (pickedTile.IsEqual(waitTile))
                    {
                        waitFound = true;
                        break;
                    }
                }
            }

            // Make sure we have a yaku if we do this. Determine our winning hand if we were to tsumo at this juncture.
            if (waitFound)
            {
                WinningHandCache = HandEvaluator.GetWinningHand(this, false, false);
                fTsumo = WinningHandCache != null;
            }
            return fTsumo;
        }

        public KanOptions GetKanOptions()
        {
            TileType pickedTile = ActiveHand[ActiveTileCount - 1];
            KanOptions options = new KanOptions();

            bool fNoClosedKanPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                                        (Parent.PrevAction == GameAction.Pon) ||
                                        (Parent.PrevAction == GameAction.OpenKan);

            bool fPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                             (Parent.PrevAction == GameAction.Pon) ||
                             (Parent.PrevAction == GameAction.OpenKan) ||
                             (Parent.PrevAction == GameAction.PromotedKan) ||
                             (Parent.PrevAction == GameAction.ClosedKan);

            if (IsInReach())
            {
                // Check to see if we can make a concealed kan. This is can only be made if the kan will not change the shape of the hand.
                // Lets only kan if the three tiles in your hand is being considered to make a concealed kan is a three of a kind.
                // This is determined earlier so we only need to look in RiichiKanTiles to see if we can kan.
                if (Parent.Settings.GetSetting<bool>(GameOption.KanAfterRiichi) &&
                    (ActiveRiichiKanTiles[0].IsEqual(pickedTile) ||
                     ActiveRiichiKanTiles[1].IsEqual(pickedTile) ||
                     ActiveRiichiKanTiles[2].IsEqual(pickedTile) ||
                     ActiveRiichiKanTiles[3].IsEqual(pickedTile)))
                {
                    options.Closed = true;
                }
            }
            else
            {
                // Check if we can do a promoted kan.
                // see if we have any tiles we can add to a pon. Then check to see if we have four of a given tile in our active hand.
                if (!fPrevCall)
                {
                    foreach (Meld meld in OpenMeld)
                    {
                        if (meld.State == MeldState.Pon)
                        {
                            for (int i = 0; (!options.Promoted && !options.Closed && (i < ActiveTileCount)); ++i)
                            {
                                if (ActiveHand[i].IsEqual(meld.Tiles[0].Tile))
                                {
                                    options.Promoted = true;
                                }
                            }
                        }
                    }
                }

                // Check for a possible concealed kan. Now... the hand except for the last tile is sorted. So we can look for
                // either four of the same tile in our sorted active hand or three of the same tile + the picked tile. We can
                // also make sure we have enough tiles in our active hand. We can very well have just 2 (next highest should be 5.)
                if (!fNoClosedKanPrevCall && (ActiveTileCount > 4))
                {
                    TileType searchTile = ActiveHand[0];
                    int tileCount = 1;
                    for (int i = 1; i < (ActiveTileCount - 1); ++i)
                    {
                        TileType checkTile = ActiveHand[i];
                        if (checkTile.IsEqual(searchTile))
                        {
                            tileCount++;
                            if (((tileCount == 3) && pickedTile.IsEqual(searchTile)) || (tileCount == 4))
                            {
                                options.Closed = true;
                                break;
                            }
                        }
                        else
                        {
                            searchTile = checkTile;
                            tileCount = 1;
                        }
                    }
                }
            }
            return options;
        }

        public void StartDiscardState()
        {
            OverrideNoReachFlag = false;

            if (!IsInReach())
            {
                Furiten = false;

                // Update our waits for every tile we can discard.
                for (int i = 0; i < ActiveTileCount; ++i)
                {
                    ActiveTileWaits[i] = HandEvaluator.GetWaits(this, i, RiichiKanTilesPerSlot[i]);
                }
            }
        }

        public bool CanReach()
        {
            bool fRiichi = false;
            bool fRiichiDisabled = Parent.IsTutorial && Parent.TutorialSettings.DisableReach;

            if (!IsInReach() && !fRiichiDisabled)
            {
                // Get waits.
                bool fHasWaits = false;
                for (int i = 0; (i < ActiveTileCount) && !fHasWaits; ++i)
                {
                    fHasWaits = (ActiveTileWaits[i] != null) && (ActiveTileWaits[i].Count > 0);
                }

                // Determine if we can reach.
                fRiichi = (fHasWaits && IsClosed() && !OverrideNoReachFlag && (Score > 1000) &&
                           (Parent.Settings.GetSetting<bool>(GameOption.Riichi) ||
                            Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) ||
                            (Parent.Settings.GetSetting<bool>(GameOption.DoubleRiichi) && CanDoubleReach())));
            }
            return fRiichi;
        }

        public TileType GetNoDiscardTile()
        {
            TileType noDiscardTile = TileType.None;

            // If we've made a call previously and it is a chii, check to see which tile was called on and which
            // tile we can't discard (IE if we chii 3-4-5 on a 3, then can't discard 6)
            if (!IsInReach() && (Parent.PrevAction == GameAction.Chii) && !Parent.Settings.GetSetting<bool>(GameOption.SequenceSwitch))
            {
                Meld meld = GetLatestMeld();
                Global.Assert(meld.State == MeldState.Chii);

                int tileCalledValue = meld.Tiles[0].Tile.GetValue();
                int tileAValue = meld.Tiles[1].Tile.GetValue();
                int tileBValue = meld.Tiles[2].Tile.GetValue();

                if ((tileCalledValue < tileAValue) && (tileCalledValue < tileBValue))
                {
                    // Tile is the lowest. Can't discard 3 higher.
                    // Note that if we go out of range, GetTile will return NO_TILE.
                    noDiscardTile = TileHelpers.BuildTile(meld.Tiles[0].Tile.GetSuit(), (tileCalledValue + 3));
                }
                else if ((tileCalledValue > tileAValue) && (tileCalledValue > tileBValue))
                {
                    // Tile is the highest. Can't discard 3 lower.
                    noDiscardTile = TileHelpers.BuildTile(meld.Tiles[0].Tile.GetSuit(), (tileCalledValue - 3));
                }
            }
            return noDiscardTile;
        }

        /**
         * If we can ron, store this in WinningHandCache and return true. Otherwise return false.
         */
        public bool CheckRon(bool fKokushiOnly)
        {
            TileType discardedTile = Parent.NextActionTile;
            Global.Assert((Parent.NextAction == GameAction.Discard) ||
                                (Parent.NextAction == GameAction.RiichiDiscard) ||
                                (Parent.NextAction == GameAction.OpenRiichiDiscard) ||
                                (Parent.NextAction == GameAction.PromotedKan) ||
                                (Parent.NextAction == GameAction.ClosedKan), "NextAction is " + Parent.NextAction);
            Global.Assert(discardedTile.IsTile(), "Discarded tile isn't a tile " + discardedTile);

            bool fHandAtozuke = false;
            WinningHandCache = null;
            if (WaitTiles != null)
            {
                foreach (TileType waitTile in WaitTiles)
                {
                    // Check all of our wait tiles to check for atozuke. Make sure we have yaku.
                    AddTemporaryTile(waitTile);
                    CandidateHand winningHandCheck = HandEvaluator.GetWinningHand(this, true, fKokushiOnly); // True because this is a check for ron.
                    RemoveTemporaryTile();

                    if (winningHandCheck == null)
                    {
                        fHandAtozuke = true;
                    }

                    if (waitTile.IsEqual(discardedTile))
                    {
                        // They've discarded one of our wait tiles. We can put our best hand into WinningHandCache.
                        WinningHandCache = winningHandCheck;
                    }
                }
            }

            if (!Parent.Settings.GetSetting<bool>(GameOption.Atozuke) && fHandAtozuke)
            {
                WinningHandCache = null;
            }
            return (WinningHandCache != null);
        }

        public bool CheckNagashiMangan()
        {
            Global.Assert(Parent.TilesRemaining == 0);

            int han = Yaku.NagashiMangan.Evaluate(this, null, false);
            if (han != 0)
            {
                WinningHandCache = new CandidateHand();
                WinningHandCache.Yaku.Add(Yaku.NagashiMangan);
                WinningHandCache.Han = han;
                return true;
            }
            return false;
        }

        public void UpdateFuriten(TileType tile)
        {
            if ((WaitTiles != null) && (WaitTiles.Count > 0))
            {
                Furiten = false;
                for (int iWaitTile = 0; !Furiten && (iWaitTile < WaitTiles.Count); ++iWaitTile)
                {
                    Furiten = tile.IsEqual(WaitTiles[iWaitTile]);
                }
            }
        }

        private bool RebuildFuriten()
        {
            // Get the list of waits.
            bool anyCalls = (Players[0].Melds.Count + Players[1].Melds.Count + Players[2].Melds.Count + Players[3].Melds.Count) > 0;
            bool overrideNoReachDummy;
            List<TileType> waits = HandEvaluator.GetWaits(activeHand, null, discards, anyCalls, out overrideNoReachDummy);

            bool furiten = false;
            if ((waits != null) && (waits.Count > 0))
            {
                // If any of our discards match one of the waits, then we're furiten!
                foreach (ExtendedTile et in discards)
                {
                    foreach (TileType tv in waits)
                    {
                        if (tv == et.Tile)
                        {
                            furiten = true;
                            break;
                        }

                        if (furiten == true)
                        {
                            break;
                        }
                    }
                }

                // Check if we've passed up a wait since our last discard. Reset on our discard unless we've reached.
                if (!furiten)
                {
                    // TODO: this
                }
            }
            return furiten;
        }

        private bool RebuildIppatsu()
        {
            int[] slots = new int[] { 0, 0, 0, 0 };
            bool ippatsu = false;

            Player currentPlayer = dealer;
            for (int iTile = 0; iTile < tilesPicked; ++iTile)
            {
                int currPlayerIndex = currentPlayer.GetZeroIndex();

                ITile et = players[currPlayerIndex].Discards[slots[currPlayerIndex]];
                slots[currPlayerIndex]++;

                if (currentPlayer == p)
                {
                    ippatsu = et.Reach || et.OpenReach;
                }

                if (et.Called)
                {
                    ippatsu = false;
                    currentPlayer = et.Caller;

                    // Roll back because the next discard wont count as a picked.
                    iTile--;
                }
                else
                {
                    currentPlayer = currentPlayer.GetNext();
                }
            }
            return ippatsu;
        }

        /**
         * Returns true if discarding the given tile would place the hand into furiten. This
         * returns true/false regardless if the player owns the specified tile (IE it returns correct
         * information if this is called during GetDiscardDecision or GetPostDiscardDecision). You can
         * use this to see if passing up ronning on a tile would put you into furiten (it will.)
         */
        bool WouldMakeFuriten(TileType discardTile)
        {
            // See if we're already in furiten.
            bool wouldMakeFuriten = Furiten;

            // See if we have any waits if we discard the tile (ignored if we don't have a full hand)
            // and then check discardTile against our waits. Also check the waits against the discard pile
            // if we have a full hand. If we're already furiten we don't need to do this. IsInFuriten should
            // handle the case of furiten reach and temporary furiten (IE we don't need to look into other people's
            // discards here. That work is already taken care of.
            if (!wouldMakeFuriten)
            {
                List<TileType> waits = HandEvaluator.GetWaits(this, GetSlot(discardTile, false), null);
                if ((waits != null) && (waits.Count > 0))
                {
                    // Check against discardTile.
                    for (int iWait = 0; (iWait < waits.Count) && !wouldMakeFuriten; ++iWait)
                    {
                        wouldMakeFuriten = waits[iWait].IsEqual(discardTile);
                    }

                    // Check against discards if we have a full hand.
                    if (!wouldMakeFuriten && IsFullHand())
                    {
                        List<ExtendedTile> discards = Discards;
                        foreach (ExtendedTile et in discards)
                        {
                            foreach (TileType tt in waits)
                            {
                                if (et.Tile.IsEqual(tt))
                                {
                                    wouldMakeFuriten = true;
                                    break;
                                }
                            }

                            if (wouldMakeFuriten)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return wouldMakeFuriten;
        }

        private void UpdateWaitTiles(int slot)
        {
            WaitTiles = ActiveTileWaits[slot];

            // Set the furiten flag if any of our waits match one of our discards.
            List<ExtendedTile> discards = Discards;
            if ((WaitTiles != null) && (WaitTiles.Count > 0))
            {
                Furiten = false;
                for (int iDiscardTile = 0; !Furiten && (iDiscardTile < discards.Count); ++iDiscardTile)
                {
                    for (int iWaitTile = 0; !Furiten && (iWaitTile < WaitTiles.Count); ++iWaitTile)
                    {
                        Furiten = discards[iDiscardTile].Tile.IsEqual(WaitTiles[iWaitTile]);
                    }
                }
            }
        }

        public void OverrideTileWaits(List<TileType> waitTiles)
        {
            WaitTiles = waitTiles;

            // Update furiten.
            List<ExtendedTile> discards = Parent.GetDiscards(Player);
            if ((WaitTiles != null) && (WaitTiles.Count > 0))
            {
                Furiten = false;
                for (int iDiscardTile = 0; !Furiten && (iDiscardTile < discards.Count); ++iDiscardTile)
                {
                    for (int iWaitTile = 0; !Furiten && (iWaitTile < WaitTiles.Count); ++iWaitTile)
                    {
                        Furiten = discards[iDiscardTile].Tile.IsEqual(WaitTiles[iWaitTile]);
                    }
                }
            }
        }

        public GameAction PeekLastDrawKanType()
        {
            TileCommand tc = (DrawsAndKans.Count > 0) ? DrawsAndKans.Peek() : null;
            Global.Log("PeekLastDrawKanType! tc: " + tc + ((tc != null) ? " " + tc.TilePrimary.Tile : "") + " draws&kanscount " + DrawsAndKans.Count);
            return (tc == null) ? GameAction.Nothing :
                   (tc.CommandType == TileCommand.Type.Tile) ? GameAction.PickedFromWall :
                                                               GameAction.ReplacementTilePick;
        }

        public string GetSummaryString()
        {
            StringBuilder sb = new StringBuilder();
            Suit lastSeenSuit = Suit.None;
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                TileType nextTile = ActiveHand[i];
                if (nextTile.IsHonor())
                {
                    sb.Append((nextTile == TileType.North) ? "n" :
                              (nextTile == TileType.East)  ? "e" :
                              (nextTile == TileType.South) ? "s" :
                              (nextTile == TileType.West)  ? "w" :
                              (nextTile == TileType.Chun)  ? "c" :
                              (nextTile == TileType.Haku)  ? "h" :
                                                             "g");
                    lastSeenSuit = Suit.None;
                }
                else
                {
                    if (nextTile.GetSuit() != lastSeenSuit)
                    {
                        lastSeenSuit = nextTile.GetSuit();
                        sb.Append((lastSeenSuit == Suit.Bamboo)     ? "b" :
                                  (lastSeenSuit == Suit.Characters) ? "m" :
                                                                      "p");
                    }
                    sb.Append(nextTile.GetValue());
                    if (nextTile.IsRedDora())
                    {
                        sb.Append("r");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
