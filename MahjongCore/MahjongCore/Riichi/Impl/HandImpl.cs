// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MahjongCore.Riichi.Impl
{
    public class HandImpl : IHand
    {
        // IHand
        public event Action<Player>                   Sorted;
        public event Action<Player, ITile[]>          TilesAdded;
        public event Action<Player, IMeld>            Called;
        public event Action<Player, ITile, ReachType> Reached;
        public event Action<Player, ITile>            Discarded;
        public event Action<Player, TileType>         DiscardUndone;

        public IGameState      Parent                { get { return _Parent; } }
        public Player          Player                { get; private set; }
        public Wind            Seat                  { get { return WindExtensionMethods.GetWind(Player, Parent.Dealer); } }
        public ITile[]         Tiles                 { get { return ActiveHandRaw; } }
        public IMeld[]         Melds                 { get { return MeldsRaw; } }
        public IList<ITile>    Discards              { get { return new List<ITile>(DiscardsRaw.Cast<ITile>()); } }
        public IList<TileType> Waits                 { get; internal set; } = new List<TileType>();
        public IList<ICommand> DrawsAndKans          { get; internal set; } = new List<ICommand>();
        public ReachType       Reach                 { get; internal set; } = ReachType.None;
        public int             Score                 { get; internal set; } = 0;
        public int             TileCount             { get; internal set; } = 0;
        public int             Streak                { get; internal set; } = 0;
        public int             MeldCount             { get { return (MeldsRaw[0].State.IsCalled() ? 1 : 0) + (MeldsRaw[1].State.IsCalled() ? 1 : 0) + (MeldsRaw[2].State.IsCalled() ? 1 : 0) + (MeldsRaw[3].State.IsCalled() ? 1 : 0); } }
        public int             MeldedTileCount       { get { return MeldsRaw[0].State.GetTileCount() + MeldsRaw[1].State.GetTileCount() + MeldsRaw[2].State.GetTileCount() + MeldsRaw[3].State.GetTileCount(); } }
        public int             KanCount              { get { return (MeldsRaw[0].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[1].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[2].State.GetMeldType() == MeldType.Kan ? 1 : 0) + (MeldsRaw[3].State.GetMeldType() == MeldType.Kan ? 1 : 0); } }
        public bool            Dealer                { get { return Player == Parent.Dealer; } }
        public bool            Open                  { get { return MeldsRaw[0].State.IsOpen() || MeldsRaw[1].State.IsOpen() || MeldsRaw[2].State.IsOpen() || MeldsRaw[3].State.IsOpen(); } }
        public bool            Closed                { get { return !Open; } }
        public bool            Tempai                { get; internal set; } = false;
        public bool            Furiten               { get; internal set; } = false;
        public bool            Yakitori              { get; internal set; } = true;  // Managed by GameStateImpl
        public bool            HasFullHand           { get { return ((MeldCount * 3) + TileCount) == TileHelpers.HAND_SIZE; } }
        public bool            CouldIppatsu          { get; internal set; } = false; // Managed by GameStateImpl
        public bool            CouldDoubleReach      { get; internal set; } = false;
        public bool            CouldKyuushuuKyuuhai  { get; internal set; } = false;
        public bool            CouldSuufurendan      { get; internal set; } = false;

        public IList<TileType> GetWaitsForDiscard(int slot) { return ActiveTileWaits[slot]; }

        public bool WouldMakeFuriten(int slot)
        {
            // Returns true if discarding the given tile would place the hand into furiten.
            CommonHelpers.Check(((slot > 0) && (slot < TileCount)), ("Specified slot out of range, found: " + slot + " TileCount: " + TileCount));
            List<TileType> waits = HandEvaluator.GetWaits(this, slot, null);
            return (waits != null) && waits.Contains(ActiveHandRaw[slot].Type.GetNonRedDoraVersion());
        }

        public int GetTileSlot(TileType tile, bool matchRed)
        {
            foreach (TileImpl handTile in ActiveHandRaw) { if (handTile.Type.IsEqual(tile, matchRed)) { return handTile.Slot; } }
            return -1;
        }

        public void MoveTileToEnd(TileType targetTile)
        {
            Global.Assert(targetTile != TileType.None);

            // Find where the target tile is in the hand. Don't need to do anything if it's already at the end.
            bool found = false;
            for (int i = 0; i < (TileCount - 1); ++i)
            {
                if (ActiveHandRaw[i].Type.IsEqual(targetTile, true))
                {
                    ActiveHandRaw[i].Type = TileType.None;
                    ActiveHandRaw[i].Ghost = true;
                    Sort(false);
                    ActiveHandRaw[TileCount - 1].Type = targetTile;
                    ActiveHandRaw[TileCount - 1].Ghost = false;

                    found = true;
                    break;
                }
            }
            Global.Assert(found);
        }

        public void ReplaceTiles(List<TileType> tilesRemove, List<TileType> tilesAdd)
        {
            CommonHelpers.Check((tilesRemove.Count == tilesAdd.Count), ("Count of tiles to add must add count of tiles to remove. Remove: " + tilesRemove.Count + " add: " + tilesAdd.Count));

            // Remove from the hand one of each tile in tilesRemove.
            foreach (TileType ripTile in tilesRemove)
            {
                bool found = false;
                for (int iHandTile = 0; iHandTile < TileCount; ++iHandTile)
                {
                    if (ripTile.IsEqual(ActiveHandRaw[iHandTile].Type, true))
                    {
                        ActiveHandRaw[iHandTile].Type = ActiveHandRaw[TileCount - 1].Type;
                        ActiveHandRaw[TileCount - 1].Type = TileType.None;
                        ActiveHandRaw[TileCount - 1].Ghost = true;
                        --TileCount;

                        found = true;
                        break;
                    }
                }
                Global.Assert(found);
            }

            // Add to the hand one of each tile in tilesAdd. We'll need to grab that tile from somewhere else on the board. Look
            // through the wall first (excluding flipped dora tiles), then other players' hands, then discards, and then the dora
            // tile. Replace the tile with a corresponding tile from tilesRemove.
            for (int i = 0; i < tilesAdd.Count; ++i)
            {
                AddTile(tilesAdd[i]);
                _Parent.ReplaceTile(tilesAdd[i], tilesRemove[i], Player);
                // TODO: Update DrawsAndKans as well...
            }
            Sort(true);
        }

        // HandImpl
        internal ICandidateHand   WinningHandCache                             { get; set; } = null;
        internal IMeld            CachedCall                                   { get; set; } = null;
        internal List<TileImpl>   DiscardsRaw                                  { get; private set; } = new List<TileImpl>();
        internal TileImpl[]       ActiveHandRaw                                { get; private set; } = new TileImpl[TileHelpers.HAND_SIZE];
        internal MeldImpl[]       MeldsRaw                                     { get; private set; } = new MeldImpl[] { new MeldImpl(), new MeldImpl(), new MeldImpl(), new MeldImpl() };
        internal bool             OverrideNoReachFlag                          { get; set; } = false; // Used for things like thirteen broken, which you can't reach for.
        internal List<TileType>[] ActiveTileWaits                              { get; private set; } = new List<TileType>[TileHelpers.HAND_SIZE];
        internal bool             HasTemporaryTile                             { get; set; } = false;
        internal TileType[]       ActiveRiichiKanTiles                         { get; set; }
        internal TileType[][]     RiichiKanTilesPerSlot                        { get; private set; } = new TileType[][]
                                                                                     { new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4],
                                                                                       new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4], new TileType[4] };
        internal bool             FourKans                                     { get { return KanCount == 4; } }
        internal List<TileType>   GetTileWaits(int slot)                       { return ActiveTileWaits[(slot == -1) ? (TileCount - 1) : slot]; }
        internal List<IMeld>      GetCalls()                                   { return HandHelpers.GetCalls(this); }
        internal ICommand         PeekLastDrawKan()                            { return DrawsAndKans.Peek(); }
        internal TileType         GetSuufurendanTile()                         { return CouldSuufurendan ? _Parent.GetHand(_Parent.Dealer).DiscardsRaw[0].Type : TileType.None; }
        internal ITile            AddTile(ITile wallTile, bool rewind = false) { return AddTile(wallTile.Type, rewind); }
        internal void             AddTileCompleted(ITile[] ts)                 { TilesAdded?.Invoke(Player, ts); }

        private GameStateImpl              _Parent;

        internal HandImpl(GameStateImpl parent, Player p, int score)
        {
            _Parent              = parent;
            Player               = p;
            Score                = score;
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];

            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i)
            {
                TileImpl tile = new TileImpl(TileType.None) { Location = Location.Hand, Ghost = true, Slot = i };
                ActiveHandRaw[i] = tile;
                ActiveTileWaits[i] = new List<TileType>();
            }

            foreach (MeldImpl meld in MeldsRaw) { meld.Owner = p; }
        }

        internal void Reset(bool resetForNextRound = false)
        {
            foreach (MeldImpl meld in MeldsRaw)                    { meld.Reset(true); }
            foreach (List<TileType> waits in ActiveTileWaits)      { waits.Clear(); }
            foreach (TileType[] kanTiles in RiichiKanTilesPerSlot) { for (int i = 0; i < kanTiles.Length; ++i) { kanTiles[i] = TileType.None; } }

            for (int i = 0; i < TileHelpers.HAND_SIZE; ++i)
            {
                TileImpl tile = ActiveHandRaw[i];
                tile.Type = TileType.None;
                tile.Ghost = true;
                tile.Slot = i;
            }

            DiscardsRaw.Clear();
            DrawsAndKans.Clear();
            Waits.Clear();

            WinningHandCache = null;
            OverrideNoReachFlag = false;
            TileCount = 0;
            CachedCall = null;
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];
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
            ActiveRiichiKanTiles = RiichiKanTilesPerSlot[TileHelpers.HAND_SIZE - 1];
            Waits = HandEvaluator.GetWaits(this, -1, ActiveRiichiKanTiles); // TODO: Redo to optionally take a list to fill vs. return.

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
                foreach (List<TileType> tileWaits in ActiveTileWaits)
                {
                    if ((tileWaits != null) && (tileWaits.Count > 0))
                    {
                        Tempai = true;
                        break;
                    }
                }
            }
            else
            {
                Tempai = Waits.Count > 0;
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
            CommonHelpers.Check(!HasTemporaryTile, "Hand already has temporary tile?");

            TileImpl tempTile = ActiveHandRaw[TileCount++];
            tempTile.Ghost = false;
            tempTile.Type = tile;
            HasTemporaryTile = true;
        }

        public void RemoveTemporaryTile()
        {
            CommonHelpers.Check(HasTemporaryTile, "Expecing to find temporary tile if removing temporary tile!");
            CommonHelpers.Check(HasFullHand, "Expecting to have a full hand when removing temporary tile!");

            TileImpl tempTile = ActiveHandRaw[--TileCount];
            tempTile.Ghost = true;
            tempTile.Type = TileType.None;
            HasTemporaryTile = false;
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
                                ((ActiveRiichiKanTiles[0].IsEqual(tile)) || (ActiveRiichiKanTiles[1].IsEqual(tile)) ||
                                 (ActiveRiichiKanTiles[2].IsEqual(tile)) || (ActiveRiichiKanTiles[3].IsEqual(tile))));
                }
                else
                {
                    // Ensure that we have four tiles in rHand with the value of Tile.
                    int count = 0;
                    for (int i = 0; i < TileCount; ++i)
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
                Global.Assert(TileCount == TileHelpers.HAND_SIZE);

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
                    Global.Assert(TileCount == TileHelpers.HAND_SIZE);
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
            for (int i = 0; i < TileCount; ++i) { if (ActiveHandRaw[i].Type.IsEqual(tile, fMatchRed)) { return i; } }
            return -1;
        }

        internal void Sort(bool fireEvent)
        {
            // Sort the tiles. Any unset tiles will end up at the end.
            Array.Sort(ActiveHandRaw);
            for (int i = 0; i < ActiveHandRaw.Length; ++i)
            {
                ActiveHandRaw[i].Slot = i;
            }

            // %%%%%%%%%%%%%%%%%%%%% REDO ALL THIS..

            if (fireEvent)
            {
                Sorted?.Invoke(Player);
            }
        }

        private ITile AddTile(TileType wallTile, bool skipAddToDrawsAndKans = false)
        {
            TileImpl handTile = ActiveHandRaw[TileCount++];
            handTile.Type = wallTile;
            handTile.Ghost = false;

            if (HasFullHand)
            {
                if (!Reach.IsReach())
                {
                    Furiten = false;

                    // Update our waits for every tile we can discard.
                    for (int i = 0; i < TileCount; ++i)
                    {
                        ActiveTileWaits[i] = HandEvaluator.GetWaits(this, i, RiichiKanTilesPerSlot[i]);
                    }
                }

                UpdateCouldDoubleReach();
                UpdateCouldKyuushuuKyuuhai();
                UpdateCouldSuufurendan();
                UpdateTempai();
            }

            // Skip in the case of rewind or manual tile manipulation.
            if (!skipAddToDrawsAndKans)
            {
                DrawsAndKans.Add(new CommandImpl(CommandType.Tile, (handTile.Clone() as ITile)));
                Global.LogExtra("Pushed onto drawsnkans! Player: " + Player + " tile: " + handTile.Type + " new drawsnkans count: " + DrawsAndKans.Count);
            }
            return handTile;
        }

        internal void RewindDiscardTile(TileImpl discardedTile)
        {
            // Add the tile at the end.
            AddTile(discardedTile, true);

            // Determine what the next tile to be placed into the wall should be.
            // TODO: Do something different if the player previously called.
            TileType prevDrawnTile = _Parent.WallRaw[_Parent.GetNextWallDrawSlot(-1)].Type;

            // If we have that tile in our hand, move it to the end.
            for (int i = 0; i < (TileCount - 1); ++i)
            {
                if (ActiveHandRaw[i].Type.IsEqual(prevDrawnTile, true))
                {
                    ActiveHandRaw[i].Type = ActiveHandRaw[TileCount - 1].Type;
                    ActiveHandRaw[TileCount - 1].Type = TileType.None;
                    ActiveHandRaw[TileCount - 1].Ghost = true;

                    Sort(false);

                    ActiveHandRaw[TileCount - 1].Type = prevDrawnTile;
                    ActiveHandRaw[TileCount - 1].Ghost = false;
                    break;
                }
            }

            DiscardUndone?.Invoke(Player, discardedTile.Type);
            Sorted?.Invoke(Player);
        }

        public bool RewindAddTile(TileType targetTile)
        {
            // Find the tile. Start backwards.
            for (int i = TileCount - 1; i >= 0; --i)
            {
                if (ActiveHandRaw[i].Type.IsEqual(targetTile, true))
                {
                    ActiveHandRaw[i].Type = TileType.None;
                    ActiveHandRaw[i].Ghost = true;
                    TileCount--;
                    Sort(false);

                    Global.Assert(targetTile == DrawsAndKans.Peek().Tile.Type);
                    DrawsAndKans.Pop();
                    return true;
                }
            }
            return false;
        }

        internal void PerformDiscard(ITile tile, ReachType reach)
        {
            CommonHelpers.Check((!Reach.IsReach() || !reach.IsReach()), "Attempting to reach a hand that is already in reach");
            CommonHelpers.Check(ActiveHandRaw[tile.Slot].Type.IsEqual(tile.Type, true), "Invalid tile parameter: Tile type: " + tile.Type + " tile slot: " + tile.Slot + " rawhand[slot] tile: " + ActiveHandRaw[tile.Slot].Type);
            CommonHelpers.Check(tile.Type.IsTile(), "Trying to discard a non-tile");

            ITile discardedHandTile = tile.Clone() as ITile;
            ActiveHandRaw[discardedHandTile.Slot].Type = TileType.None;
            ActiveHandRaw[discardedHandTile.Slot].Ghost = true;
            TileCount--;
            Sort(false);

            DiscardsRaw.Add(new TileImpl(discardedHandTile.Type) { Reach = reach, Slot = DiscardsRaw.Count, Location = Location.Discard });
            WinningHandCache = null;

            // Update our waits unless we're PREVIOUSLY in reach.
            if (!Reach.IsReach())
            {
                Waits = ActiveTileWaits[discardedHandTile.Slot];
                UpdateFuritenWithDiscards();
            }

            // Stash the available closed kan tiles if we're entering reach.
            if (reach.IsReach())
            {
                Reach = reach;
                ActiveRiichiKanTiles = RiichiKanTilesPerSlot[discardedHandTile.Slot];
            }

            if (reach.IsReach()) { Reached?.Invoke(Player, discardedHandTile, reach); }
            else                 { Discarded?.Invoke(Player, discardedHandTile); }
        }

        internal void PerformClosedKan(TileType tile)
        {
            MeldImpl meld = GetNextEmptyMeld();
            meld.State = MeldState.KanConcealed;

            int meldSlot = 0;
            for (int i = 0; i < TileCount; ++i)
            {
                if (ActiveHandRaw[i].Type.IsEqual(tile))
                {
                    meld.TilesRaw[meldSlot++].Type = ActiveHandRaw[i].Type;
                    ActiveHandRaw[i].Type = TileType.None;
                    ActiveHandRaw[i].Ghost = true;
                    TileCount--;
                }
            }
            CommonHelpers.Check((meldSlot == 4), ("Couldn't find four of tile: " + tile));

            meld.SortMeldTilesForClosedKan();
            DrawsAndKans.Add(new CommandImpl(CommandType.ClosedKan, tile.GetNonRedDoraVersion()));
            Sort(false);

            // Unless we're in reach, update our immediate waits because closed kan can break everything. Just recalculate from scratch.
            if (!Reach.IsReach())
            {
                Waits = HandEvaluator.GetWaits(this, -1, null);
            }

            Called?.Invoke(Player, meld);
        }

        internal void PerformPromotedKan(ITile tile)
        {
            CommonHelpers.Check(!Reach.IsReach(), "Attempting to promoted kan a hand that is in reach??");
            CommonHelpers.Check(ActiveHandRaw[tile.Slot].Type.IsEqual(tile.Type, true), "Invalid tile parameter");

            // Remove the tile from the hand.
            TileType discardType = tile.Type;
            int kanTileSlot = tile.Slot;
            ActiveHandRaw[kanTileSlot].Type = TileType.None;
            ActiveHandRaw[kanTileSlot].Ghost = true;
            --TileCount;
            Sort(false);

            // Promote the pon.
            MeldImpl meld = GetPonMeld(tile.Type);
            meld.State = MeldState.KanPromoted;
            meld.TilesRaw[3].Type = discardType;
            DrawsAndKans.Add(new CommandImpl(CommandType.PromotedKan, discardType.GetNonRedDoraVersion()));

            // Update waits, as we should not be in reach.
            Waits = ActiveTileWaits[kanTileSlot];
            UpdateFuritenWithDiscards();

            Called?.Invoke(Player, meld);
        }

        internal void PerformCachedCall()
        {
            IMeld cached = CachedCall;
            CachedCall = null;

            // Remove tiles from ActiveHand by just setting tiles to no tile.
            // We'll sort after. Manually delay the event until after we make the call.
            foreach (ITile meldTile in cached.Tiles)
            {
                if (meldTile.Type.IsTile() && !meldTile.Ghost)
                {
                    ActiveHandRaw[meldTile.Slot].Type = TileType.None;
                    ActiveHandRaw[meldTile.Slot].Ghost = true;
                }
            }
            TileCount -= (cached.State.GetTileCount() - 1);
            Sort(false);

            // Set the meld.
            MeldImpl nextMeld = GetNextEmptyMeld();
            nextMeld.Set(cached);

            Called?.Invoke(Player, nextMeld);
            Sorted?.Invoke(Player);
        }

        internal bool CanTsumo()
        {
            if (!_Parent.PreviousAction.IsOpenCall())
            {
                TileType pickedTile = ActiveHandRaw[TileCount - 1].Type;
                if (Waits != null)
                {
                    foreach (TileType waitTile in Waits)
                    {
                        // Check if the picked tile is equal to any one of our waits. If so,
                        // check if we have a winning hand (false if we don't have a yaku.)
                        if (pickedTile.IsEqual(waitTile))
                        {
                            WinningHandCache = HandEvaluator.GetWinningHand(this, false, false);
                            return (WinningHandCache != null);
                        }
                    }
                }
            }
            return false;
        }

        internal List<TileType> GetAvailablePromotedKans(List<TileType> existingArray)
        {
            List<TileType> promotedKanTiles = existingArray;
            if (promotedKanTiles == null) { promotedKanTiles = new List<TileType>(); }
            else                          { promotedKanTiles.Clear(); }

            // Check if we can do a promoted kan.
            // see if we have any tiles we can add to a pon. Then check to see if we have four of a given tile in our active hand.
            if (!_Parent.PreviousAction.IsOpenCall())
            {
                foreach (MeldImpl meld in MeldsRaw)
                {
                    if (meld.State == MeldState.Pon)
                    {
                        for (int i = 0; i < TileCount; ++i)
                        {
                            if (ActiveHandRaw[i].Type.IsEqual(meld.CalledTile.Type))
                            {
                                promotedKanTiles.Add(ActiveHandRaw[i].Type);
                                break;
                            }
                        }
                    }
                }
            }
            return promotedKanTiles;
        }

        internal List<TileType> GetAvailableClosedKans(List<TileType> existingArray)
        {
            List<TileType> closedKanTiles = existingArray;
            if (closedKanTiles == null) { closedKanTiles = new List<TileType>(); }
            else                        { closedKanTiles.Clear(); }

            if (Reach.IsReach())
            {
                // Check to see if we can make a concealed kan. This is can only be made if the kan will not change
                // the shape of the hand. Approved tiles have already been determined in _ActiveRiichiKanTiles.
                TileType pickedTile = ActiveHandRaw[TileCount - 1].Type;
                if (Parent.Settings.GetSetting<bool>(GameOption.KanAfterRiichi) &&
                    ((ActiveRiichiKanTiles[0].IsEqual(pickedTile)) || (ActiveRiichiKanTiles[1].IsEqual(pickedTile)) ||
                     (ActiveRiichiKanTiles[2].IsEqual(pickedTile)) || (ActiveRiichiKanTiles[3].IsEqual(pickedTile))))
                {
                    closedKanTiles.Add(pickedTile);
                }
            }
            else
            {
                // Check for a possible concealed kan. Now... the hand except for the last tile is sorted. So we can look for
                // either four of the same tile in our sorted active hand or three of the same tile + the picked tile. We can
                // also make sure we have enough tiles in our active hand. We can very well have just 2 (next highest should be 5.)
                if (!_Parent.PreviousAction.IsOpenCall() && (TileCount > 4))
                {
                    TileType pickedTile = ActiveHandRaw[TileCount - 1].Type;
                    TileType searchTile = ActiveHandRaw[0].Type;
                    int tileCount = 1;
                    for (int i = 1; i < (TileCount - 1); ++i)
                    {
                        TileType checkTile = ActiveHandRaw[i].Type;
                        if (checkTile.IsEqual(searchTile))
                        {
                            tileCount++;
                            if (((tileCount == 3) && pickedTile.IsEqual(searchTile)) || (tileCount == 4))
                            {
                                closedKanTiles.Add(checkTile.GetNonRedDoraVersion());
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
            return closedKanTiles;
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
            if (!Reach.IsReach() && (_Parent.PreviousAction == GameAction.Chii) && !_Parent.Settings.GetSetting<bool>(GameOption.SequenceSwitch))
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

        public bool CheckRon()
        {
            // If we can ron, store this in WinningHandCache and return true. Otherwise return false.
            TileType discardedTile = _Parent.NextActionTile;
            CommonHelpers.Check(((_Parent.NextAction == GameAction.Discard) ||
                                 (_Parent.NextAction == GameAction.RiichiDiscard) ||
                                 (_Parent.NextAction == GameAction.OpenRiichiDiscard) ||
                                 (_Parent.NextAction == GameAction.PromotedKan) ||
                                 (_Parent.NextAction == GameAction.ClosedKan)), ("Expected next action compatible with ron, found: " + _Parent.NextAction));
            CommonHelpers.Check(discardedTile.IsTile(), ("Discarded tile isn't a tile " + discardedTile));

            bool handAtozuke = false;
            WinningHandCache = null;

            if ((Waits != null) && (Waits.Count > 0))
            {
                foreach (TileType waitTile in Waits)
                {
                    // Check all of our wait tiles to check for atozuke. Make sure we have yaku.
                    AddTemporaryTile(waitTile);
                    ICandidateHand winningHandCheck = HandEvaluator.GetWinningHand(this, true, (_Parent.PreviousAction == GameAction.ClosedKan));
                    RemoveTemporaryTile();

                    if (winningHandCheck == null)
                    {
                        handAtozuke = true;
                    }

                    if (waitTile.IsEqual(discardedTile))
                    {
                        // They've discarded one of our wait tiles. We can put our best hand into WinningHandCache.
                        WinningHandCache = winningHandCheck;
                    }
                }
            }

            if (!_Parent.Settings.GetSetting<bool>(GameOption.Atozuke) && handAtozuke)
            {
                WinningHandCache = null;
            }
            return (WinningHandCache != null);
        }

        public bool CheckNagashiMangan()
        {
            Global.Assert(_Parent.TilesRemaining == 0);

            int han = Yaku.NagashiMangan.Evaluate(this, null, false);
            if (han != 0)
            {
                var winningHand = new CandidateHand();
                winningHand.Yaku.Add(Yaku.NagashiMangan);
                winningHand.Han = han;
                WinningHandCache = winningHand;
                return true;
            }
            return false;
        }

        internal void UpdateTemporaryFuriten(TileType tile)
        {
            Furiten |= ((Waits != null) && Waits.Contains(tile.GetNonRedDoraVersion()));
        }

        private void RebuildFuriten()
        {
            // Set furiten if any wait matches any discard.
            UpdateFuritenWithDiscards();

            if ((Waits.Count > 0) && !Furiten)
            {
                bool checkDiscards = false;
                _Parent.IterateDiscards((Player player, TileImpl tile) =>
                {
                    if (checkDiscards && Waits.Contains(tile.Type.GetNonRedDoraVersion()))
                    {
                        Furiten = true;
                        return false;
                    }

                    // Check all discards after this one if this is our last discard or we've reached.
                    checkDiscards |= ((player == Player) && ((tile.Slot == (DiscardsRaw.Count - 1)) || tile.Reach.IsReach()));
                    return true;
                });
            }
        }

        private void RebuildIppatsu()
        {
            _Parent.IterateDiscards((Player player, TileImpl tile) =>
            {
                // If ippatsu is active and we make another discard or another player makes a call, cancel ippatsu.
                // TODO: There's a bug here where closed/promoted kans wont cancel ippatsu.
                if (CouldIppatsu && ((player == Player) || tile.Called))
                {
                    CouldIppatsu = false;
                    return false;
                }

                // Set ippatsu if we reached but don't clear it otherwise.
                CouldIppatsu |= ((player == Player) && tile.Reach.IsReach());
                return true;
            });
        }

        private void UpdateFuritenWithDiscards()
        {
            // Set the furiten flag if any of our waits match one of our discards.
            Furiten = false;
            if ((Waits != null) && (Waits.Count > 0) && (DiscardsRaw != null) && (DiscardsRaw.Count > 0))
            {
                foreach (TileType waitTile in Waits)
                {
                    foreach (TileImpl discardTile in DiscardsRaw)
                    {
                        if (discardTile.Type.IsEqual(waitTile))
                        {
                            Furiten = true;
                            return;
                        }
                    }
                }
            }
        }

        internal void OverrideTileWaits(List<TileType> waitTiles)
        {
            CommonHelpers.Check((waitTiles != null), "Tile collection supplied to OverrideTileWaits is null!");
            Waits = waitTiles;
            UpdateFuritenWithDiscards();
        }

        internal GameAction PeekLastDrawKanType()
        {
            ICommand tc = (DrawsAndKans.Count > 0) ? DrawsAndKans.Peek() : null;
            Global.LogExtra("PeekLastDrawKanType! tc: " + tc + ((tc != null) ? " " + tc.Tile.Type : "") + " DrawsAndKansCount " + DrawsAndKans.Count);
            return (tc == null)                      ? GameAction.Nothing :
                   ((tc.Command == CommandType.Tile) ? GameAction.PickedFromWall :
                                                       GameAction.ReplacementTilePick);
        }

        internal string GetSummaryString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < TileCount; ++i)
            {
                ActiveHandRaw[i].Type.GetSummary(sb, (i > 0) ? new TileType?(ActiveHandRaw[i - 1].Type) : null);
            }
            return sb.ToString();
        }
    }
}
