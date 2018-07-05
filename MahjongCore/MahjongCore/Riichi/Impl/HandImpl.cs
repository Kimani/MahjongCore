// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MahjongCore.Riichi.Impl
{
    public class HandImpl : IHand
    {
        // IHand
        public IGameState      Parent                { get { return _Parent; } }
        public Player          Player                { get; private set; }
        public Wind            Seat                  { get { return WindExtensionMethods.GetWind(Player, Parent.Dealer); } }
        public ITile[]         ActiveHand            { get { return ActiveHandRaw; } }
        public IMeld[]         Melds                 { get { return MeldsRaw; } }
        public IList<ITile>    Discards              { get { return (IList<ITile>)DiscardsRaw; } }
        public IList<TileType> Waits                 { get; internal set; } = new List<TileType>();
        public IList<ICommand> DrawsAndKans          { get; internal set; } = new List<ICommand>();
        public ReachType       Reach                 { get; internal set; } = ReachType.None;
        public int             Score                 { get; internal set; } = 0;
        public int             ActiveTileCount       { get; internal set; } = 0;
        public int             Streak                { get; internal set; } = 0;
        public int             MeldCount             { get { return (MeldsRaw[0].State.IsCalled() ? 1 : 0) + (MeldsRaw[1].State.IsCalled() ? 1 : 0) + (MeldsRaw[2].State.IsCalled() ? 1 : 0) + (MeldsRaw[3].State.IsCalled() ? 1 : 0); } }
        public int             MeldedTileCount       { get { return MeldsRaw[0].State.GetTileCount() + MeldsRaw[1].State.GetTileCount() + MeldsRaw[2].State.GetTileCount() + MeldsRaw[3].State.GetTileCount(); } }
        public int             KanCount              { get { return (MeldsRaw[0].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[1].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[2].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[3].State.GetMeldType() == MeldType.Kan ? 1 : 0); } }
        public bool            Dealer                { get { return Player == Parent.Dealer; } }
        public bool            Open                  { get { return MeldsRaw[0].State.IsOpen() || MeldsRaw[1].State.IsOpen() || MeldsRaw[2].State.IsOpen() || MeldsRaw[3].State.IsOpen(); } }
        public bool            Closed                { get { return !Open; } }
        public bool            Tempai                { get; internal set; } = false;
        public bool            Furiten               { get; internal set; } = false;
        public bool            Yakitori              { get; internal set; } = true;
        public bool            HasFullHand           { get { return ((MeldCount * 3) + ActiveTileCount) == TileHelpers.HAND_SIZE; } }
        public bool            CouldIppatsu          { get; internal set; } = false;
        public bool            CouldDoubleReach      { get; internal set; } = false;
        public bool            CouldKyuushuuKyuuhai  { get; internal set; } = false;
        public bool            CouldSuufurendan      { get; internal set; } = false;

        public IList<TileType> GetWaitsForDiscard(int slot) { return _ActiveTileWaits[slot]; }

        public int GetTileSlot(TileType tile, bool matchRed)
        {
            foreach (TileImpl handTile in ActiveHandRaw) { if (handTile.Type.IsEqual(tile, matchRed)) { return handTile.Slot; } }
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
                        %%%% SORT OR reslot...
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

        // HandImpl
        internal ICandidateHand WinningHandCache               { get; set; } = null;
        internal IMeld          CachedCall                     { get; set; } = null;
        internal List<TileImpl> DiscardsRaw                    { get; set; } = new List<TileImpl>();
        internal TileImpl[]     ActiveHandRaw                  { get; set; } = new TileImpl[TileHelpers.HAND_SIZE];
        internal MeldImpl[]     MeldsRaw                       { get; set; } = new MeldImpl[] { new MeldImpl(), new MeldImpl(), new MeldImpl(), new MeldImpl() };
        internal bool           OverrideNoReachFlag            { get; set; } = false; // Used for things like thirteen broken, which you can't reach for.
        internal bool           FourKans                       { get { return KanCount == 4; } }
        internal List<TileType> GetTileWaits(int slot)         { return _ActiveTileWaits[(slot == -1) ? (ActiveTileCount - 1) : slot]; }
        internal List<IMeld>    GetCalls()                     { return RiichiHandHelpers.GetCalls(this, Parent); }
        internal ICommand       PeekLastDrawKan()              { return _DrawsAndKans.Peek(); }
        internal TileType       GetSuufurendanTile()           { return CouldSuufurendan ? _Parent.GetHand(_Parent.Dealer).DiscardsRaw[0].Type : TileType.None; }

        private GameStateImpl      _Parent;
        private List<TileType>[]   _ActiveTileWaits       = new List<TileType>[TileHelpers.HAND_SIZE];
        private Stack<CommandImpl> _DrawsAndKans          = new Stack<CommandImpl>();
        private bool               _HasTemporaryTile      = false;
        private List<TileType>     _WaitTiles             = new List<TileType>();
        private TileType[]         _ActiveRiichiKanTiles;
        private TileType[][]       _RiichiKanTilesPerSlot = new TileType[][] { new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                               new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                               new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                               new TileType[4], new TileType[4] };

        internal HandImpl(GameStateImpl parent, Player p, int score)
        {
            _Parent               = parent;
            Player                = p;
            Score                 = score;
            _ActiveRiichiKanTiles = _RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];

            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i)
            {
                TileImpl tile = new TileImpl(TileType.None);
                tile.Location = Location.Hand;
                tile.Ghost = true;
                tile.Slot = i;
                ActiveHandRaw[i] = tile;
                _ActiveTileWaits[i] = new List<TileType>();
            }
        }

        internal void Reset(bool resetForNextRound = false)
        {
            foreach (MeldImpl meld in MeldsRaw)                     { meld.Reset(); }
            foreach (List<TileType> waits in _ActiveTileWaits)      { waits.Clear(); }
            foreach (TileType[] kanTiles in _RiichiKanTilesPerSlot) { for (int i = 0; i < kanTiles.Length; ++i) { kanTiles[i] = TileType.None; } }

            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i)
            {
                TileImpl tile = ActiveHandRaw[i];
                tile.Type = TileType.None;
                tile.Ghost = true;
                tile.Slot = i;
            }

            DiscardsRaw.Clear();
            _DrawsAndKans.Clear();
            _WaitTiles.Clear();

            WinningHandCache = null;
            Furiten = false;
            CouldIppatsu = false;
            OverrideNoReachFlag = false;
            ActiveTileCount = 0;
            CachedCall = null;
            _ActiveRiichiKanTiles = null;
            Reach = ReachType.None;
            Tempai = false;
            Furiten = false;
            CouldIppatsu = false;
            CouldDoubleReach = false;
            CouldKyuushuuKyuuhai = false;
            CouldSuufurendan = false;

            // Don't reset these fields if we're just advancing rounds.
            if (!resetForNextRound)
            {
                Streak = 0;
                Score = 0;
                Yakitori = true;
            }
        }

        internal void Rebuild()
        {
            // Gets called after loading from a save state and fields like score, streak, yakitori, hands, calls, draws/kans,
            // and discards have been set. This method should reconstruct everything else like furiten, waits, ippatsu flags, etc.
            _ActiveRiichiKanTiles = _RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];
            _WaitTiles = HandEvaluator.GetWaits(this, -1, _ActiveRiichiKanTiles);

            foreach (TileImpl tile in DiscardsRaw)
            {
                if (tile.Reach.IsReach())
                {
                    Reach = tile.Reach;
                    break;
                }
            }

            RebuildFuriten();
            RebuildIppatsu();
        }

        private bool UpdateTempai()
        {
            Tempai = false;
            if (HasFullHand)
            {
                // We haven't populated WaitTiles yet. In this case, we're tempai if anything in ActiveTileWaits is non-null.
                foreach (List<TileType> tileWaits in _ActiveTileWaits)
                {
                    if (tileWaits.Count > 0)
                    {
                        Tempai = true;
                        break;
                    }
                }
            }
            else
            {
                Tempai = _WaitTiles.Count > 0;
            }
            return Tempai;
        }

        private MeldImpl GetPonMeld(TileType tile)
        {
            foreach (MeldImpl meld in MeldsRaw)
            {
                if ((meld.State == MeldState.Pon) && (meld.TilesRaw[0].Type.IsEqual(tile)))
                {
                    return meld;
                }
            }
            return null;
        }

        internal MeldImpl GetLatestMeld()
        {
            return (MeldsRaw[3].State != MeldState.None) ? MeldsRaw[3] :
                   (MeldsRaw[2].State != MeldState.None) ? MeldsRaw[2] :
                   (MeldsRaw[1].State != MeldState.None) ? MeldsRaw[1] :
                   (MeldsRaw[0].State != MeldState.None) ? MeldsRaw[0] : null;
        }

        private MeldImpl GetNextEmptyMeld()
        {
            return (MeldsRaw[0].State == MeldState.None) ? MeldsRaw[0] :
                   (MeldsRaw[1].State == MeldState.None) ? MeldsRaw[1] :
                   (MeldsRaw[2].State == MeldState.None) ? MeldsRaw[2] :
                   (MeldsRaw[3].State == MeldState.None) ? MeldsRaw[3] : null;
        }

        internal bool IsInDoubleReach()
        {
            bool isDoubleReach = false;
            if (Reach.IsReach())
            {
                isDoubleReach = true;

                // Check the players who went before the player we are looking at. If any calls have occured then we can't cant double reach.
                for (Player playerCheck = _Parent.Dealer; playerCheck != Player; playerCheck = playerCheck.GetNext())
                {
                    HandImpl checkHand = _Parent.GetHand(playerCheck);
                    if (((checkHand.DiscardsRaw.Count > 0) && checkHand.DiscardsRaw[0].Called) || checkHand.MeldsRaw[0].State.IsCalled())
                    {
                        isDoubleReach = false;
                        break;
                    }
                }
            }
            return isDoubleReach;
        }

        public void AddTemporaryTile(TileType tile)
        {
            CommonHelpers.Check(!_HasTemporaryTile, "Hand already has temporary tile?");

            TileImpl tempTile = ActiveHandRaw[ActiveTileCount++];
            tempTile.Ghost = false;
            tempTile.Type = tile;
            _HasTemporaryTile = true;
        }

        public void RemoveTemporaryTile()
        {
            CommonHelpers.Check(_HasTemporaryTile, "Expecing to find temporary tile if removing temporary tile!");
            CommonHelpers.Check(HasFullHand, "Expecting to have a full hand when removing temporary tile!");

            TileImpl tempTile = ActiveHandRaw[--ActiveTileCount];
            tempTile.Ghost = true;
            tempTile.Type = TileType.None;
            _HasTemporaryTile = false;
        }

        internal bool CanClosedKanWithTile(TileType tile)
        {
            bool closedKan = false;
            if (Parent.Settings.GetSetting<bool>(GameOption.FifthKanDraw) ||
                ((Parent.Player1Hand.KanCount + Parent.Player2Hand.KanCount + Parent.Player3Hand.KanCount + Parent.Player4Hand.KanCount) < 4))
            {
                if (Reach.IsReach())
                {
                    // If we're in reach, make sure the type is equal to one of the approved reach kan tiles.
                    closedKan = (Parent.Settings.GetSetting<bool>(GameOption.KanAfterRiichi) &&
                                ((_ActiveRiichiKanTiles[0].IsEqual(tile)) || (_ActiveRiichiKanTiles[1].IsEqual(tile)) ||
                                 (_ActiveRiichiKanTiles[2].IsEqual(tile)) || (_ActiveRiichiKanTiles[3].IsEqual(tile))));
                }
                else
                {
                    // Ensure that we have four tiles in rHand with the value of Tile.
                    int count = 0;
                    for (int i = 0; i < ActiveTileCount; ++i)
                    {
                        Global.Assert(!ActiveHandRaw[i].Ghost);
                        count += (ActiveHandRaw[i].Type.IsEqual(tile)) ? 1 : 0;
                    }
                    closedKan = (count == 4);
                }
            }
            return closedKan;
        }

        private void UpdateCouldDoubleReach()
        {
            CouldDoubleReach = false;
            if (DiscardsRaw.Count == 0)
            {
                CouldDoubleReach = true;
                for (Player playerCheck = _Parent.Dealer; playerCheck != Player; playerCheck = playerCheck.GetNext())
                {
                    HandImpl checkHand = _Parent.GetHand(playerCheck);
                    if (((checkHand.DiscardsRaw.Count > 0) && checkHand.DiscardsRaw[0].Called) || checkHand.MeldsRaw[0].State.IsCalled())
                    {
                        CouldDoubleReach = false;
                        break;
                    }
                }
            }
        }

        public void UpdateCouldKyuushuuKyuuhai()
        {
            CouldKyuushuuKyuuhai = false;
            if ((DiscardsRaw.Count == 0) &&
                (Parent.Player1Hand.MeldCount == 0) &&
                (Parent.Player2Hand.MeldCount == 0) &&
                (Parent.Player3Hand.MeldCount == 0) &&
                (Parent.Player4Hand.MeldCount == 0))
            {
                Global.Assert(ActiveTileCount == TileHelpers.HAND_SIZE);

                // Make sure we have 9 unique terminals and honors.
                bool circle1 = false;
                bool circle9 = false;
                bool character1 = false;
                bool character9 = false;
                bool bamboo1 = false;
                bool bamboo9 = false;
                bool north = false;
                bool west = false;
                bool south = false;
                bool east = false;
                bool haku = false;
                bool chun = false;
                bool hatsu = false;

                foreach (TileImpl tile in ActiveHandRaw)
                {
                    circle1    |= (tile.Type.IsEqual(TileType.Circles1));
                    circle9    |= (tile.Type.IsEqual(TileType.Circles9));
                    character1 |= (tile.Type.IsEqual(TileType.Characters1));
                    character9 |= (tile.Type.IsEqual(TileType.Characters9));
                    bamboo1    |= (tile.Type.IsEqual(TileType.Bamboo1));
                    bamboo9    |= (tile.Type.IsEqual(TileType.Bamboo9));
                    north      |= (tile.Type.IsEqual(TileType.North));
                    east       |= (tile.Type.IsEqual(TileType.East));
                    south      |= (tile.Type.IsEqual(TileType.South));
                    west       |= (tile.Type.IsEqual(TileType.West));
                    haku       |= (tile.Type.IsEqual(TileType.Haku));
                    chun       |= (tile.Type.IsEqual(TileType.Chun));
                    hatsu      |= (tile.Type.IsEqual(TileType.Hatsu));
                }

                int count = (circle1    ? 1 : 0) + (circle9    ? 1 : 0) +
                            (character1 ? 1 : 0) + (character9 ? 1 : 0) +
                            (bamboo1    ? 1 : 0) + (bamboo9    ? 1 : 0) +
                            (north      ? 1 : 0) + (west       ? 1 : 0) +
                            (south      ? 1 : 0) + (east       ? 1 : 0) +
                            (haku       ? 1 : 0) + (chun       ? 1 : 0) +
                            (hatsu      ? 1 : 0);
                CouldKyuushuuKyuuhai = (count >= 9);
            }
        }

        private void UpdateCouldSuufurendan()
        {
            Global.Assert(_Parent.Current == Player);

            CouldSuufurendan = false;
            if ((DiscardsRaw.Count == 0) && (Seat == Wind.North))
            {
                HandImpl handA = _Parent.GetHand(_Parent.Dealer);
                HandImpl handB = _Parent.GetHand(handA.Player.GetNext());
                HandImpl handC = _Parent.GetHand(handB.Player.GetNext());

                if ((handA.Discards.Count == 1) &&
                    handA.DiscardsRaw[0].Type.IsWind() &&
                    (handA.MeldCount == 0) &&
                    (handB.MeldCount == 0) &&
                    (handC.MeldCount == 0) &&
                    (handA.DiscardsRaw[0].Type.IsEqual(handB.DiscardsRaw[0].Type)) &&
                    (handA.DiscardsRaw[0].Type.IsEqual(handC.DiscardsRaw[0].Type)))
                {
                    Global.Assert(ActiveTileCount == TileHelpers.HAND_SIZE);
                    TileType suufurendanTile = handA.DiscardsRaw[0].Type;
                    foreach (TileImpl tile in ActiveHandRaw)
                    {
                        if (tile.Type.IsEqual(suufurendanTile))
                        {
                            CouldSuufurendan = true;
                            break;
                        }
                    }
                }
            }
        }
        
        public int GetSlot(TileType tile, bool fMatchRed)
        {
            for (int i = 0; i < ActiveTileCount; ++i) { if (ActiveHandRaw[i].Type.IsEqual(tile, fMatchRed)) { return i; } }
            return -1;
        }

        internal void Sort()
        {
            // Sort the tiles. Any unset tiles will end up at the end.
            Array.Sort(ActiveHand);
            // %%%%%%%%%%%%%%%%%%%%% REDO ALL THIS..
        }

        public ITile AddTile(ITile wallTile, bool rewind = false)
        {
            TileImpl handTile = ActiveHandRaw[ActiveTileCount++];
            handTile.Type = wallTile.Type;
            handTile.Ghost = false;

            if (HasFullHand)
            {
                // %%%%%%%%%%%%%%%%%%%%% CAN MERGE WITH STARTDISCARDSTATE?!?!

                UpdateCouldDoubleReach();
                UpdateCouldKyuushuuKyuuhai();
                UpdateCouldSuufurendan();
            }


            if (!rewind)
            {
                _DrawsAndKans.Push(new CommandImpl(CommandType.Tile, (handTile.Clone() as ITile)));
                Global.LogExtra("Pushed onto drawsnkans! Player: " + Player + " tile: " + handTile.Type + " new drawsnkans count: " + DrawsAndKans.Count);
            }

            return handTile;
        }

        public void RewindDiscardTile(TileImpl discardedTile)
        {
            // Add the tile at the end.
            // %%%%%%%%%%%%%% turn discardedtile into a real tile... figure out what this means against AddTile and StartDiscardState...
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
            Sort();
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


        public void PerformAbortiveDraw(ITile tile)
        {
            if (tile != null)
            {
                targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
                tile = ActiveHand[targetSlot];

                ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
                ActiveHand[ActiveTileCount - 1] = TileType.None;
                ActiveTileCount--;
                Sort(false);
            }
        }

        public void PerformDiscard(ITile tile)
        {
            int targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
            TileType tile = ActiveHand[targetSlot];

            ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            ActiveTileCount--;
            Sort(false);

            ADD TO DISCARDS

            WinningHandCache = null;

            // Update our waits unless we're in reach.
            if (!IsInReach())
            {
                UpdateWaitTiles(targetSlot);
            }
        }

        public void PerformReach(ITile tile, bool fOpenReach)
        {
            int targetSlot = (slot == -1) ? (ActiveTileCount - 1) : slot;
            TileType tile = ActiveHand[targetSlot];

            ActiveHand[targetSlot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            ActiveTileCount--;
            Sort(false);

            ADD TO DISCARDS

            UpdateWaitTiles(targetSlot);
            WinningHandCache = null;
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[targetSlot];
        }

        public void PerformClosedKan(ITile tile)
        {
            // Remove the tile from the hand.
            TileType tile = ActiveHand[slot];
            ActiveHand[slot] = ActiveHand[ActiveTileCount - 1];
            ActiveHand[ActiveTileCount - 1] = TileType.None;
            --ActiveTileCount;

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
                {
                    // Closed kan can break everything. Just recalculate from scratch.
                    WaitTiles = HandEvaluator.GetWaits(this, -1, null);
                }
            }
        }

        public void PerformPromotedKan(ITile tile)
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

            Sort(false);

            // Update our waits, unless we're in reach. This is so we can rinshan.
            if (!IsInReach())
            {
                if (fPromoted)
                {
                    UpdateWaitTiles(slot);
                }
            }
        }

        public IMeld PerformCachedCall()
        {
            IMeld cached = CachedCall;
            CachedCall = null;

            // Remove tiles from ActiveHand by just setting tiles to no tile. We'll sort after.
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                if ((i == StoredCallOption.SlotA) || (i == StoredCallOption.SlotB) || (i == StoredCallOption.SlotC))
                {
                    ActiveHand[i] = TileType.None;
                }
            }
            ActiveTileCount -= (StoredCallOption.Type.GetTileCount() - 1);
            Sort();

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


            return cached;
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

        internal List<TileType> GetAvailablePromotedKans(List<TileType> existingArray)
        {
                        bool fNoClosedKanPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                                        (Parent.PrevAction == GameAction.Pon) ||*
                                        (Parent.PrevAction == GameAction.OpenKan);


            bool fPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                 (Parent.PrevAction == GameAction.Pon) ||
                 (Parent.PrevAction == GameAction.OpenKan) ||
                 (Parent.PrevAction == GameAction.PromotedKan) ||
                 (Parent.PrevAction == GameAction.ClosedKan);

           
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


            }
            return options;
        }

        internal List<TileType> GetAvailableClosedKans(List<TileType> existingArray)
        {
            bool fNoClosedKanPrevCall = (Parent.PrevAction == GameAction.Chii) ||
                            (Parent.PrevAction == GameAction.Pon) || *
                            (Parent.PrevAction == GameAction.OpenKan);


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

        public void StartDiscardState()
        {
            OverrideNoReachFlag = false;

            if (!Reach.IsReach())
            {
                Furiten = false;

                // Update our waits for every tile we can discard.
                for (int i = 0; i < ActiveTileCount; ++i)
                {
                    _ActiveTileWaits[i] = HandEvaluator.GetWaits(this, i, _RiichiKanTilesPerSlot[i]);
                }
            }
        }

        public bool CanReach()
        {
            return Tempai &&
                   Closed &&
                   !Reach.IsReach() &&
                   !!Parent.ExtraSettings.DisableReach && (Score >= 1000 || !_Parent.Settings.GetSetting<bool>(GameOption.Buttobi)) &&
                   !OverrideNoReachFlag &&
                   (Parent.Settings.GetSetting<bool>(GameOption.Riichi) ||
                    Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) ||
                    (Parent.Settings.GetSetting<bool>(GameOption.DoubleRiichi) && CouldDoubleReach));
        }

        public TileType GetKuikaeTile()
        {
            TileType kuikaeTile = TileType.None;

            // If we've made a call previously and it is a chii, check to see which tile was called on and which
            // tile we can't discard (IE if we chii 3-4-5 on a 3, then can't discard 6)
            if (!Reach.IsReach() && (Parent.PrevAction == GameAction.Chii) && !Parent.Settings.GetSetting<bool>(GameOption.SequenceSwitch))
            {
                MeldImpl meld = GetLatestMeld();
                Global.Assert(meld.State == MeldState.Chii);

                int tileCalledValue = meld.Tiles[0].Type.GetValue();
                int tileAValue = meld.Tiles[1].Type.GetValue();
                int tileBValue = meld.Tiles[2].Type.GetValue();

                if ((tileCalledValue < tileAValue) && (tileCalledValue < tileBValue))
                {
                    // Tile is the lowest. Can't discard 3 higher.
                    // Note that if we go out of range, GetTile will return NO_TILE.
                    kuikaeTile = TileHelpers.BuildTile(meld.Tiles[0].Type.GetSuit(), (tileCalledValue + 3));
                }
                else if ((tileCalledValue > tileAValue) && (tileCalledValue > tileBValue))
                {
                    // Tile is the highest. Can't discard 3 lower.
                    kuikaeTile = TileHelpers.BuildTile(meld.Tiles[0].Type.GetSuit(), (tileCalledValue - 3));
                }
            }
            return kuikaeTile;
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

            int han = Yaku.NagashiMangan.Evaluate(Parent.Settings, this, null, false);
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
            for (int i = 0; i < ActiveTileCount; ++i)
            {
                ActiveHandRaw[i].Type.GetSummary(sb, (i > 0) ? new TileType?(ActiveHandRaw[i - 1].Type) : null);
            }
            return sb.ToString();
        }
    }
}
