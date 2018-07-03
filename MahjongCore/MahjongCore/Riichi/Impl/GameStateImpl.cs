// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Riichi;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MahjongCore.Riichi.Impl
{
    #region GameAction
        internal enum GameAction
        {
            [SkyValue(0),  WinValue(WinType.None)]  Nothing,
            [SkyValue(1),  WinValue(WinType.None)]  Discard,
            [SkyValue(2),  WinValue(WinType.None)]  Chii,                // Sorting Chii, Pon, OpenKan and Ron
            [SkyValue(3),  WinValue(WinType.None)]  Pon,                 // in ascending order on purpose so
            [SkyValue(4),  WinValue(WinType.None)]  OpenKan,             // you can just sort in order to
            [SkyValue(5),  WinValue(WinType.Ron)]   Ron,                 // determine what should be done.
            [SkyValue(6),  WinValue(WinType.Tsumo)] Tsumo,
            [SkyValue(7),  WinValue(WinType.None)]  ClosedKan,
            [SkyValue(8),  WinValue(WinType.None)]  PromotedKan,
            [SkyValue(9),  WinValue(WinType.None)]  RiichiDiscard,
            [SkyValue(10), WinValue(WinType.None)]  DecisionPending,
            [SkyValue(11), WinValue(WinType.None)]  PickedFromWall,
            [SkyValue(12), WinValue(WinType.None)]  ReplacementTilePick, // If set, we want to pick our tile from the dead wall instead of the regular wall.
            [SkyValue(13), WinValue(WinType.None)]  AbortiveDraw,        // If Kyuushuukyuuhai or too many kans.
            [SkyValue(14), WinValue(WinType.None)]  OpenRiichiDiscard,   // Will need to update marshalling if this goes over 0xf
        }

        internal static class GameActionExtentionMethods
        {
            public static WinType GetWinType(this GameAction ga)                   { return EnumAttributes.GetAttributeValue<WinValue, WinType>(ga); }
            public static int     GetSkyValue(this GameAction ga)                  { return EnumAttributes.GetAttributeValue<SkyValue, int>(ga); }
            public static bool    TryGetGameAction(string text, out GameAction ga) { return EnumHelper.TryGetEnumByCode<GameAction, SkyValue>(text, out ga); }
            public static bool    IsAgari(this GameAction ga)                      { return (ga == GameAction.Ron) || (ga == GameAction.Tsumo); }

            public static GameAction GetGameAction(string text)
            {
                GameAction ga;
                if (!EnumHelper.TryGetEnumByCode<GameAction, SkyValue>(text, out ga))
                {
                throw new Exception("Failed to parse GameAction: " + text);
                }
                return ga;
            }
        }
    #endregion

    internal enum AdvanceAction
    {
        Done,
        Advance,
        KanChosenTile,
        HandEnd,
        GameEnd,
        ReplacementPickTile,
        DecidePostCallMove,
        RandomizingBreak
    }

    internal class GameStateImpl : IGameState
    {
        // IGameState
        public event EventHandler<HandSortedEventArgs>      HandSorted;
        public event EventHandler<HandPickingTileArgs>      HandPickingTile;
        public event EventHandler<HandTileAddedArgs>        HandTileAdded;
        public event EventHandler<HandDiscardArgs>          HandDiscard;
        public event EventHandler<HandReachArgs>            HandReach;
        public event EventHandler<HandKanArgs>              HandKan;
        public event EventHandler<HandCallArgs>             HandCall;
        public event EventHandler<HandRonArgs>              HandRon;
        public event EventHandler<HandTsumoArgs>            HandTsumo;
        public event EventHandler<DiscardRequestedArgs>     DiscardRequested;
        public event EventHandler<PostDiscardRequstedArgs>  PostDiscardRequested;
        public event EventHandler<MultiWinArgs>             MultiWin;
        public event EventHandler<ExhaustiveDrawArgs>       ExhaustiveDraw;
        public event EventHandler<AbortiveDrawArgs>         AbortiveDraw;
        public event EventHandler<GameCompleteArgs>         GameComplete;
        public event EventHandler<PlayerChomboArgs>         Chombo;
        public event EventHandler<PostDiscardRequstedArgs>  PostKanRequested;
        public event EventHandler<WallTilesPicked>          WallTilesPicked;
        public event EventHandler                           DiceRolled;
        public event EventHandler                           DeadWallMoved;
        public event EventHandler<DoraIndicatorFlippedArgs> DoraIndicatorFlipped;
        public event EventHandler                           PreCheckAdvance;
        public event EventHandler                           TableCleared;
        public event EventHandler                           PreCheckRewind;
        public event EventHandler<DiscardUndoneArgs>        DiscardUndone;
        public event EventHandler<WinUndoneArgs>            WinUndone;
        public event EventHandler<TilePickUndoneArgs>       TilePickUndone;
        public event EventHandler<DecisionCancelledArgs>    DecisionCancelled;

        public ITile[]          Wall               { get { return WallRaw; } }
        public ITile[]          DoraIndicators     { get { return DoraIndicatorsRaw; } }
        public ITile[]          UraDoraIndicators  { get { return UraDoraIndicatorsRaw; } }
        public Round            Round              { get; internal set; } = Round.East1;
        public Player           FirstDealer        { get; internal set; } = Player.None;
        public Player           Dealer             { get; internal set; } = Player.None;
        public Player           Current            { get; internal set; } = Player.None;
        public Player           Wareme             { get; internal set; } = Player.None;
        public PlayState        State              { get; internal set; } = PlayState.PreGame;
        public IGameSettings    Settings           { get; internal set; }
        public IExtraSettings   ExtraSettings      { get; internal set; }
        public IHand            Player1Hand        { get { return Player1HandRaw; } }
        public IHand            Player2Hand        { get { return Player2HandRaw; } }
        public IHand            Player3Hand        { get { return Player3HandRaw; } }
        public IHand            Player4Hand        { get { return Player4HandRaw; } }
        public IPlayerAI        Player1AI          { get; set; }
        public IPlayerAI        Player2AI          { get; set; }
        public IPlayerAI        Player3AI          { get; set; }
        public IPlayerAI        Player4AI          { get; set; }
        public bool             Lapped             { get; internal set; } = false;
        public int              Offset             { get; internal set; } = 0;
        public int              TilesRemaining     { get; internal set; } = 0;
        public int              Bonus              { get; internal set; } = 0;
        public int              Pool               { get; internal set; } = 0;
        public int              DoraCount          { get; internal set; } = 0;
        public int              Roll               { get; internal set; } = 0;

        public ISaveState Save() { return new SaveStateImpl(this); }

        public void Advance()
        {
            CommonHelpers.Check(_CanAdvance, "Advance reentry detected! Cannot call advance at this time.");
            Advance(State.GetNext(), EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(State.GetNext()), false);
        }

        public void Resume()
        {
            CommonHelpers.Check(_CanResume, "Cannot resume, as this hasn't been paused.");
            Advance(State, false, true);
        }

        public void Rewind()
        {
            if (!CheckForPause(PreCheckRewind))
            {
                CommonHelpers.Check(_RewindPreHandlers.ContainsKey(State), "Cannot rewind from here!");
                _RewindPreHandlers[State].Invoke();   // Use the rewind mode handler to rewind the mode to the correct previous mode. This must exist.
                _RewindPostHandlers[State]?.Invoke(); // Now that we're at a previous mode, execute a handler. This might not exist, that is fine.
            }
            else
            {
                // TODO: "Setup state so that it's how it should be at the beginning of this mode." <- whatever this means.
            }
        }

        public void Pause()
        {
            CommonHelpers.Check(_ExpectingPause, "Not expecting pause at this time. Can only pause during PreCheckAdvance or PreCheckRewind handlers.");
            _ShouldPause = true;
            _CanResume = true;
        }

        public void SubmitDiscard(IDiscardDecision decision)
        {
            CommonHelpers.Check(_ExpectingDiscard, "Not expecting discard decision at this time!");
            _ExpectingDiscard = false;

            // If we're currently processing DecideMove (evidenced by the _CanAdvance reentry flag being cleared)
            // then we want to setup _AdvanceAction. Otherwise we want to pump state advancement at this time.
            _AdvanceAction = ProcessDiscardDecision(decision);
            if (_CanAdvance)
            {
                PlayState state;
                bool advancePlayer;
                DetermineAdvanceState(_AdvanceAction, out state, out advancePlayer);
                Advance(state, advancePlayer, false);
            }
        }

        public void SubmitPostDiscard(IPostDiscardDecision decision)
        {
            CommonHelpers.Check(((GetNextAction(decision.Player) == GameAction.DecisionPending) && (State == PlayState.GatherDecisions)),
                                "Not expecting post discard decision at this time.");

            // If we're currently processing DecideMove (evidenced by the _CanAdvance reentry flag being cleared)
            // then we just want to leave _AdvanceAction set. Otherwise we want to pump state advancement at this time.
            _AdvanceAction = ProcessPostDiscardDecision(decision);
            if (_CanAdvance && (_AdvanceAction == AdvanceAction.Advance))
            {
                Advance();
            }
        }

        // GameStateImpl
        internal TileImpl[]    WallRaw                  { get; private set; } = new TileImpl[TileHelpers.MAIN_WALL_TILE_COUNT];
        internal TileImpl[]    DoraIndicatorsRaw        { get; private set; } = new TileImpl[5];
        internal TileImpl[]    UraDoraIndicatorsRaw     { get; private set; } = new TileImpl[5];
        internal HandImpl      Player1HandRaw           { get; private set; }
        internal HandImpl      Player2HandRaw           { get; private set; }
        internal HandImpl      Player3HandRaw           { get; private set; }
        internal HandImpl      Player4HandRaw           { get; private set; }
        internal Stack<Player> DiscardPlayerList        { get; private set; } = new Stack<Player>();
        internal GameAction    PrevAction               { get; private set; } = GameAction.Nothing;
        internal GameAction    NextAction               { get; private set; } = GameAction.Nothing;
        internal Player        PlayerRecentOpenKan      { get; private set; } = Player.None;
        internal Player        NextActionPlayer         { get; private set; } = Player.None;
        internal TileType      NextActionTile           { get; private set; } = TileType.None;
        internal bool          PlayerDeadWallPick       { get; private set; } = false;
        internal bool          FlipDoraAfterNextDiscard { get; private set; } = false;
        internal bool          ChankanFlag              { get; private set; } = false;
        internal bool          KanburiFlag              { get; private set; } = false;

        private Dictionary<PlayState, Action> _PreBreakStateHandlers  = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _PostBreakStateHandlers = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindPreHandlers      = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindPostHandlers     = new Dictionary<PlayState, Action>();
        private WinResultsImpl                _WinResultCache         = new WinResultsImpl();
        private DiscardInfoImpl               _DiscardInfoCache       = new DiscardInfoImpl();
        private PostDiscardInfoImpl           _PostDiscardInfoCache   = new PostDiscardInfoImpl();
        private PostDiscardDecisionImpl       _CachedPostDiscardPass  = new PostDiscardDecisionImpl();
        private Player                        _NextActionPlayerTarget = Player.None;
        private GameAction                    _NextAction1            = GameAction.Nothing;
        private GameAction                    _NextAction2            = GameAction.Nothing;
        private GameAction                    _NextAction3            = GameAction.Nothing;
        private GameAction                    _NextAction4            = GameAction.Nothing;
        private GameAction                    _RewindAction           = GameAction.Nothing;
        private AdvanceAction                 _AdvanceAction          = AdvanceAction.Done;
        private AbortiveDrawType              _NextAbortiveDrawType   = AbortiveDrawType.Other;
        private bool                          _SkipAdvancePlayer      = false;
        private bool                          _HasExtraSettings       = false;
        private int                           _NextActionSlot         = -1;
        private bool                          _ExpectingPause         = false;
        private bool                          _ShouldPause            = false;
        private bool                          _CanAdvance             = true;
        private bool                          _CanResume              = false;
        private bool                          _ExpectingDiscard       = false;
        private bool                          _NagashiWin             = false;

        internal     GameStateImpl()                                       { InitializeCommon(null, null, true); }
        internal     GameStateImpl(IGameSettings settings)                 { Initialize(settings, null); }
        internal     GameStateImpl(IExtraSettings extra)                   { Initialize(null, extra); }
        internal     GameStateImpl(ISaveState state)                       { InitializeFromState(state, null); }
        internal     GameStateImpl(ISaveState state, IExtraSettings extra) { InitializeFromState(state, extra); }
        public void  ExecuteRewindModeChange_HandEnd()                     { PerformRoundEndRewindStep(); }
        public void  ExecuteRewindModeChange_TableCleanup()                { PerformRoundEndRewindStep(); }
        public void  ExecuteRewindModeChange_GameEnd()                     { PerformRoundEndRewindStep(); }
        public void  ExecuteRewindModeChange_GatherDecisions()             { State = PlayState.DecideMove; }
        public void  ExecuteRewindModeChange_KanChosenTile()               { State = PlayState.DecideMove; }
        public void  ExecuteRewindModeChange_NextTurn()                    { State = PlayState.GatherDecisions; }
        public void  ExecuteRewindModeChange_PerformDecision()             { State = PlayState.NextTurn; }
        public void  ExecuteRewindModeChange_PickTile()                    { State = PlayState.NextTurn; }
        public void  ExecutePostBreak_PreTilePick1()                       { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick2()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick3()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick4()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick5()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick6()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick7()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick8()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick9()                    { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick10()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick11()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick12()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 4)); }
        public void  ExecutePostBreak_PreTilePick13()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 1)); }
        public void  ExecutePostBreak_PreTilePick14()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 1)); }
        public void  ExecutePostBreak_PreTilePick15()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 1)); }
        public void  ExecutePostBreak_PreTilePick16()                   { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 1)); }
        public void  ExecutePostBreak_PrePickTile()                     { HandPickingTile?.Invoke(this, new HandPickingTileArgsImpl(Current, 1)); } // Sink.PerformSave();
        public void  ExecutePostBreak_TilePick1()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick2()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick3()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick4()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick5()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick6()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick7()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick8()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick9()                       { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick10()                      { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick11()                      { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick12()                      { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick13()                      { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick14()                      { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick15()                      { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick16()                      { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_PickTile()                        { PickIntoPlayerHand(Current, 1); }
        private void FlipDora()                                         { DoraIndicatorFlipped?.Invoke(this, new DoraIndicatorFlippedArgsImpl(DoraIndicators[DoraCount++])); }
        private void ApplyWinResultsToPlayerScores(WinResultsImpl win)  { foreach (Player p in PlayerHelpers.Players) { GetHand(p).Score += win.GetPlayerDelta(p) + win.GetPlayerPoolDelta(p); } }

        internal void Reset()
        {
            Settings.Reset();
            ExtraSettings.Reset();
            Player1HandRaw.Reset();
            Player2HandRaw.Reset();
            Player3HandRaw.Reset();
            Player4HandRaw.Reset();
            DiscardPlayerList.Clear();
            _WinResultCache.Reset();
            _DiscardInfoCache.Reset();
            _PostDiscardInfoCache.Reset();
            _CachedPostDiscardPass.Reset();

            foreach (TileImpl tile in WallRaw)                       { tile.Reset(); }
            for (int i = 0; i < DoraIndicatorsRaw.Length; ++i)       { DoraIndicatorsRaw[i] = null; }
            for (int i = 0; i < UraDoraIndicatorsRaw.Length; ++i)    { UraDoraIndicatorsRaw[i] = null; }

            Round = Round.East1;
            FirstDealer = Player.None;
            Dealer = Player.None;
            Current = Player.None;
            Wareme  = Player.None;
            State = PlayState.PreGame;
            Lapped = false;
            Offset  = 0;
            TilesRemaining = 0;
            Bonus = 0;
            Pool = 0;
            DoraCount = 0;
            Roll = 0;
            PrevAction = GameAction.Nothing;
            NextAction = GameAction.Nothing;
            PlayerRecentOpenKan = Player.None;
            NextActionPlayer = Player.None;
            NextActionTile = TileType.None;
            PlayerDeadWallPick = false;
            FlipDoraAfterNextDiscard = false;
            ChankanFlag = false;
            KanburiFlag = false;
            _NextActionPlayerTarget = Player.None;
            _NextAction1 = GameAction.Nothing;
            _NextAction2 = GameAction.Nothing;
            _NextAction3 = GameAction.Nothing;
            _NextAction4 = GameAction.Nothing;
            _RewindAction = GameAction.Nothing;
            _AdvanceAction = AdvanceAction.Done;
            _SkipAdvancePlayer = false;
            _HasExtraSettings = false;
            _NextActionSlot = -1;
            _ExpectingPause = false;
            _ShouldPause = false;
            _CanAdvance = true;
            _CanResume = false;
            _ExpectingDiscard = false;
        }

        private void Initialize(IGameSettings settings, IExtraSettings extra)
        {
            InitializeCommon(settings, extra);

            FirstDealer = PlayerHelpers.GetRandom();
            Dealer = FirstDealer;
            Current = FirstDealer;
        }

        private void InitializeFromState(ISaveState state, IExtraSettings extra)
        {
            InitializeCommon(state.Settings, extra);

            CommonHelpers.Check((state is SaveStateImpl), "SaveState not from MahjongCore, external save states not supported at this time.");
            (state as SaveStateImpl).PopulateState(this, null);
        }

        private void InitializeCommon(IGameSettings settings, IExtraSettings extra, bool skipHandlers = false)
        {
            for (int i = 0; i < WallRaw.Length; ++i)
            {
                WallRaw[i] = new TileImpl();
                WallRaw[i].Slot = i;
                WallRaw[i].Location = Location.Wall;
            }

            // Set settings and determine if we're in tutorial mode if ts is null or not.
            Settings          = (settings != null) ? settings : new GameSettingsImpl();
            _HasExtraSettings = (extra != null);
            ExtraSettings     = (extra != null) ? extra : new ExtraSettingsImpl();

            int score = Settings.GetSetting<int>(GameOption.StartingPoints);
            Player1HandRaw = new HandImpl(this, Player.Player1, score);
            Player2HandRaw = new HandImpl(this, Player.Player2, score);
            Player3HandRaw = new HandImpl(this, Player.Player3, score);
            Player4HandRaw = new HandImpl(this, Player.Player4, score);

            if (skipHandlers)
            {
                foreach (PlayState ps in Enum.GetValues(typeof(PlayState)))
                {
                    TryAddStateHandlerFunction(ps, _PreBreakStateHandlers,  "ExecutePreBreak_");
                    TryAddStateHandlerFunction(ps, _PostBreakStateHandlers, "ExecutePostBreak_");
                    TryAddStateHandlerFunction(ps, _RewindPreHandlers,      "ExecuteRewindModeChange_");
                    TryAddStateHandlerFunction(ps, _RewindPostHandlers,     "ExecuteRewindPostModeChange_");
                }
            }
        }

        private void Advance(PlayState nextMode, bool advancePlayer, bool skipCheck)
        {
            _RewindAction = GameAction.Nothing;
            _CanAdvance = false;
            bool queuedSkipCheck = skipCheck;
            bool queuedAdvancePlayer = advancePlayer;
            PlayState queuedMode = nextMode;

            do
            {
                _AdvanceAction = AdvanceAction.Done;

                if (!queuedSkipCheck)
                {
                    // Advance player, set flags/game mode/etc.
                    State = queuedMode;
                    queuedSkipCheck = false;

                    if (_SkipAdvancePlayer)
                    {
                        _SkipAdvancePlayer = false;
                    }
                    else if (queuedAdvancePlayer)
                    {
                        Current = Current.GetNext();
                    }

                    // Execute a pre-break handler which will make the new state valid if we end up pausing here.
                    // Skip this if we previously paused because we already did it.
                    _PreBreakStateHandlers[queuedMode]?.Invoke();
                    if (CheckForPause(PreCheckAdvance))
                    {
                        _AdvanceAction = AdvanceAction.Done;
                        break;
                    }
                }

                // Execute the bulk of the game mode.
                _PostBreakStateHandlers[queuedMode]?.Invoke();

                // Determine what the next step will be.
                DetermineAdvanceState(_AdvanceAction, out queuedMode, out queuedAdvancePlayer);
            } while (_AdvanceAction != AdvanceAction.Done);
            _CanAdvance = true;
        }

        private void DetermineAdvanceState(AdvanceAction action, out PlayState state, out bool advancePlayer)
        {
            advancePlayer = false;

            switch (_AdvanceAction)
            {
                case AdvanceAction.Advance: state = State.GetNext();
                                            advancePlayer = EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(State.GetNext());
                                            break;

                default:                    throw new Exception("Unexpected AdvanceAction");
            }
        }

        private AdvanceAction ProcessPostDiscardDecision(IPostDiscardDecision decision)
        {
            CommonHelpers.Check((!(decision is PostDiscardDecisionImpl) || ((PostDiscardDecisionImpl)decision).Validate()), "Post discard decision failed validation.");
            GetHand(decision.Player).CachedCall = decision.Call;
            SetNextAction(decision.Player, (decision.Decision == PostDiscardDecisionType.Ron)     ? GameAction.Ron :
                                           (decision.Decision == PostDiscardDecisionType.Nothing) ? GameAction.Nothing :
                                           (decision.Call.State == MeldState.Chii)                ? GameAction.Chii :
                                           (decision.Call.State == MeldState.Pon)                 ? GameAction.Pon :
                                                                                                    GameAction.OpenKan);

            // Advance if all decisions are accounted for.
            AdvanceAction nextAction = (_NextAction1 != GameAction.DecisionPending) &&
                                       (_NextAction2 != GameAction.DecisionPending) &&
                                       (_NextAction3 != GameAction.DecisionPending) &&
                                       (_NextAction4 != GameAction.DecisionPending) ? AdvanceAction.Advance : AdvanceAction.Done;

            if (nextAction == AdvanceAction.Advance)
            {
                // Set up the next action that is to be taken.
                NextAction = GameAction.Nothing;
                foreach (Player p in PlayerHelpers.Players)
                {
                    GameAction nextPlayerAction = GetNextAction(p);
                    if (nextPlayerAction.GetSkyValue() > NextAction.GetSkyValue())
                    {
                        NextAction = nextPlayerAction;
                        NextActionPlayer = p;
                    }
                }

                // Check to see if multiple people have ronned.
                // TODO: Implement head bump here. Be sure to invoke DecisionCancelled with a null call.
                if ((NextAction == GameAction.Ron) &&
                    (((_NextAction1.IsAgari() ? 1 : 0) + (_NextAction2.IsAgari() ? 1 : 0) + (_NextAction3.IsAgari() ? 1 : 0) + (_NextAction4.IsAgari() ? 1 : 0)) > 1))
                {
                    NextActionPlayer = Player.Multiple;
                }

                // Clear out stored call options on anyone that didn't win the decision.
                foreach (Player p in PlayerHelpers.Players)
                {
                    if ((NextActionPlayer != p) && (GetHand(p).CachedCall != null))
                    {
                        DecisionCancelled?.Invoke(this, new DecisionCancelledArgsImpl(p, GetHand(p).CachedCall));
                        GetHand(p).CachedCall = null;
                    }
                }
            }
            return nextAction;
        }

        private AdvanceAction ProcessDiscardDecision(IDiscardDecision decision)
        {
            _ExpectingDiscard = false;
            HandImpl hand = GetHand(Current);
            CommonHelpers.Check((!(decision is DiscardDecisionImpl) || ((DiscardDecisionImpl)decision).Validate(hand)), "Post discard decision failed validation.");

            AdvanceAction action = AdvanceAction.Advance;
            switch (decision.Decision)
            {
                case DiscardDecisionType.Discard:           hand.PerformDiscard(decision.Tile);
                                                            PerformDiscardState(decision.Tile, GameAction.Discard);
                                                            HandDiscard?.Invoke(this, new HandDiscardArgsImpl(decision.Tile));
                                                            break;

                case DiscardDecisionType.OpenRiichiDiscard:
                case DiscardDecisionType.RiichiDiscard:     bool openReach = (decision.Decision == DiscardDecisionType.OpenRiichiDiscard);
                                                            hand.PerformReach(decision.Tile, openReach);
                                                            PerformDiscardState(decision.Tile, (openReach ? GameAction.OpenRiichiDiscard : GameAction.RiichiDiscard));
                                                            hand.Score -= 1000;
                                                            hand.CouldIppatsu = true;
                                                            HandReach?.Invoke(this, new HandReachArgsImpl(decision.Tile));
                                                            break;

                case DiscardDecisionType.Tsumo:             NextAction = GameAction.Tsumo;
                                                            NextActionPlayer = Current;
                                                            action = AdvanceAction.HandEnd;
                                                            break;

                case DiscardDecisionType.ClosedKan:         hand.PerformClosedKan(decision.Tile);
                                                            NextActionTile = decision.Tile.Type;
                                                            NextAction = GameAction.ClosedKan;
                                                            _NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            break;

                case DiscardDecisionType.PromotedKan:       hand.PerformPromotedKan(decision.Tile);
                                                            NextActionTile = decision.Tile.Type;
                                                            NextAction = GameAction.PromotedKan;
                                                            _NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            break;

                case DiscardDecisionType.AbortiveDraw:      hand.PerformAbortiveDraw(decision.Tile);
                                                            NextActionPlayer = Current;
                                                            NextAction = GameAction.AbortiveDraw;
                                                            if (decision.Tile != null)
                                                            {
                                                                hand.PerformDiscard(decision.Tile);
                                                                PerformDiscardState(decision.Tile, GameAction.AbortiveDraw);
                                                                HandDiscard?.Invoke(this, new HandDiscardArgsImpl(decision.Tile));
                                                            }
                                                            _NextAbortiveDrawType = (decision.Tile == null) ? AbortiveDrawType.KyuushuuKyuuhai : AbortiveDrawType.Suufuurendan;
                                                            break;

                default:                                    throw new Exception("Unexpected discard decision.");
            }
            return action;
        }

        private void TryAddStateHandlerFunction(PlayState state, Dictionary<PlayState, Action> basket, string prefix)
        {
            // https://www.roelvanlisdonk.nl/?p=3790
            MethodInfo info = typeof(GameStateImpl).GetMethod(prefix + state.ToString());
            if (info != null)
            {
                var thisParameter = Expression.Constant(this);
                MethodCallExpression methodCall = Expression.Call(thisParameter, info);
                Expression<Action> lambda = Expression.Lambda<Action>(methodCall);
                basket.Add(state, lambda.Compile());
            }
        }

        private bool CheckForPause(EventHandler handler)
        {
            Exception stashedException = null;
            bool pause = false;
            try
            {
                _ExpectingPause = true;
                _ShouldPause = false;
                handler?.Invoke(this, null);
                pause = _ShouldPause;
            }
            catch (Exception e)
            {
                stashedException = e;
            }
            finally
            {
                _ExpectingPause = false;
                _ShouldPause = false;
                if (stashedException != null)
                {
                    throw stashedException;
                }
            }
            return pause;
        }

        public void ExecutePreBreak_DeadWallMove()
        {
            Player1HandRaw.Sort();
            Player2HandRaw.Sort();
            Player3HandRaw.Sort();
            Player4HandRaw.Sort();

            HandSorted?.Invoke(this, new HandSortedEventArgsImpl(Player.Player1));
            HandSorted?.Invoke(this, new HandSortedEventArgsImpl(Player.Player2));
            HandSorted?.Invoke(this, new HandSortedEventArgsImpl(Player.Player3));
            HandSorted?.Invoke(this, new HandSortedEventArgsImpl(Player.Player4));
        }

        public void ExecutePostBreak_RandomizingBreak()
        {
            Roll                     = (ExtraSettings.OverrideDiceRoll != null) ? ExtraSettings.OverrideDiceRoll.Value : (Global.RandomRange(1, 7) + Global.RandomRange(1, 7));
            Offset                   = GameStateHelpers.GetOffset(Dealer, Roll);
            TilesRemaining           = 122;
            DoraCount                = 0;
            Wareme                   = Settings.GetSetting<bool>(GameOption.Wareme) ? Dealer.AddOffset(Roll - 1) : Player.None;
            PlayerRecentOpenKan      = Player.None;
            PlayerDeadWallPick       = false;
            FlipDoraAfterNextDiscard = false;

            if (ExtraSettings.OverrideDiceRoll == null)
            {
                TileHelpers.GetRandomBoard(WallRaw, Settings.GetSetting<RedDora>(GameOption.RedDoraOption));
            }

            DiceRolled?.Invoke(this, null);
            PopulateDoraIndicators();
        }

        public void ExecutePostBreak_DeadWallMove()
        {
            FlipDora();
            DeadWallMoved?.Invoke(this, null);
        }

        public void ExecutePostBreak_DecideMove()
        {
            HandImpl hand = GetHand(Current);
            _DiscardInfoCache.Populate(hand);
            hand.StartDiscardState();

            // Query the AI for the discard decision, or raise the event requesting for the discard decision. If we 
            // can query the AI or the discard decision is supplied immediately during the event, then _AdvanceAction
            // will get set. Otherwise, _AdvanceAction will remain "Done" and our caller can supply discard info later.
            IPlayerAI ai = GetAI(Current);
            _ExpectingDiscard = true;
            if (ai != null)
            {
                SubmitDiscard(ai.GetDiscardDecision(_DiscardInfoCache));
            }
            else
            {
                DiscardRequested?.Invoke(this, new DiscardRequestedArgsImpl(_DiscardInfoCache));
            }
        }

        private void QueryPostDiscardDecision(GameAction currAction, HandImpl hand, IPlayerAI ai)
        {
            if (currAction == GameAction.DecisionPending)
            {
                // Determine if a post discard decision can even be made. Otherwise just set nothing.
                _PostDiscardInfoCache.Populate(hand);
                if (_PostDiscardInfoCache.CanRon || (_PostDiscardInfoCache.CallsRaw.Count > 0))
                {
                    if (ai != null)
                    {
                        SubmitPostDiscard(ai.GetPostDiscardDecision(_PostDiscardInfoCache));
                    }
                    else
                    {
                        PostDiscardRequested?.Invoke(this, new PostDiscardRequstedArgsImpl(_PostDiscardInfoCache));
                    }
                }
                else
                {
                    _CachedPostDiscardPass.Reset();
                    _CachedPostDiscardPass.Player = hand.Player;
                    SubmitPostDiscard(_CachedPostDiscardPass);
                }
            }
        }

        public void ExecutePostBreak_GatherDecisions()
        {
            // Set actions as pending except for current player which will make no decision.
            foreach (Player p in PlayerHelpers.Players) { SetNextAction(p, ((Current == p) ? GameAction.Nothing : GameAction.DecisionPending)); }
            NextActionPlayer = Current;

            // Poll decisions from AI, or query decisions from caller. If we populate all actions then _AdvanceAction
            // will get set. Otherwise a later call by the caller to SubmitPostDiscard will pump advancements.
            _AdvanceAction = AdvanceAction.Done;
            foreach (Player p in PlayerHelpers.Players) { QueryPostDiscardDecision(GetNextAction(p), GetHand(p), GetAI(p)); }
        }

        public void ExecutePostBreak_PerformDecision()
        {
            PlayerDeadWallPick = false; // Can clear this flag at this juncture - Rinshan has already been evaluated.

            bool updateFuriten = false;
            Player furitenDiscardingPlayer = Current;
            TileType furitenDiscardedTile = NextActionTile;

            // Flip the dora if we should now. You don't get it on a ron after discarding after a kan.
            if ((NextAction != GameAction.Ron) && FlipDoraAfterNextDiscard)
            {
                FlipDoraAfterNextDiscard = false;
                if (Settings.GetSetting<bool>(GameOption.KanDora)) { FlipDora(); }
            }

            if (Settings.GetSetting<bool>(GameOption.FourKanDraw) &&
                (PrevAction == GameAction.ReplacementTilePick) &&
                (NextAction != GameAction.Ron) &&
                (DoraCount > 4) &&
                !Player1HandRaw.FourKans && !Player2HandRaw.FourKans && !Player3HandRaw.FourKans && !Player4HandRaw.FourKans)
            {
                // Four kans, no one player has all four kans, and noone ronned on the fourth kan discard. Draw.
                KanburiFlag = false;
                NextAction = GameAction.AbortiveDraw;
                NextActionTile = TileType.None;
                _NextActionSlot = -1;
                _AdvanceAction = AdvanceAction.HandEnd;
                _NextAbortiveDrawType = AbortiveDrawType.FourKans;
            }
            else if (NextAction == GameAction.Nothing)
            {
                KanburiFlag = false;
                if (Settings.GetSetting<bool>(GameOption.FourReachDraw) &&
                    Player1Hand.Reach.IsReach() && Player2Hand.Reach.IsReach() && Player3Hand.Reach.IsReach() && Player4Hand.Reach.IsReach())
                {
                    // Four reaches! Draw game!
                    NextAction = GameAction.AbortiveDraw;
                    NextActionTile = TileType.None;
                    _NextActionSlot = -1;
                    _AdvanceAction = AdvanceAction.HandEnd;
                    _NextAbortiveDrawType = AbortiveDrawType.FourReach;
                }
                else
                {
                    updateFuriten = true;
                    PrevAction = GameAction.Discard;
                    _AdvanceAction = (TilesRemaining == 0) ? AdvanceAction.HandEnd : AdvanceAction.Advance;
                }
            }
            else if ((NextAction == GameAction.Chii) || (NextAction == GameAction.Pon) || (NextAction == GameAction.OpenKan))
            {
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).CouldIppatsu = false; }
                KanburiFlag = false;

                // Make the discarded tile have the Called flag set and also who called it.
                List<TileImpl> targetDiscards = GetHand(Current).DiscardsImpl;
                var targetTile = targetDiscards[targetDiscards.Count - 1];
                targetTile.Called = true;
                targetTile.Ancillary = NextActionPlayer;

                // Perform the stored call.
                HandImpl winningHand = GetHand(NextActionPlayer);
                HandCall?.Invoke(this, new HandCallArgsImpl(winningHand.PerformCachedCall()));
                HandSorted?.Invoke(this, new HandSortedEventArgsImpl(NextActionPlayer));

                // Take action.
                if ((NextAction == GameAction.OpenKan) && (DoraCount == 5))
                {
                    // This is the 5th kan. Immediately abort.
                    NextActionTile = TileType.None;
                    NextActionPlayer = NextActionPlayer;
                    NextAction = GameAction.AbortiveDraw;
                    _NextActionSlot = -1;
                    _AdvanceAction = AdvanceAction.HandEnd;
                    _NextAbortiveDrawType = AbortiveDrawType.FiveKans;
                }
                else
                {
                    // Set PrevAction to the call type to show that we're discarding from a call.
                    PrevAction = NextAction; // Becomes GameAction.Chii/Pon/OpenKan
                    if (NextAction == GameAction.OpenKan)
                    {
                        PlayerRecentOpenKan = Current; // For Sekinin Barai
                        NextAction = GameAction.ReplacementTilePick;
                        _AdvanceAction = AdvanceAction.ReplacementPickTile;
                    }
                    else
                    {
                        _AdvanceAction = AdvanceAction.DecidePostCallMove;
                    }

                    updateFuriten = true;
                    Current = NextActionPlayer;
                }
            }
            else if (NextAction == GameAction.Ron)
            {
                _NextActionPlayerTarget = Current;
                _AdvanceAction = AdvanceAction.HandEnd;
            }
            else
            {
                throw new Exception("Unexpected NextAction for PerformDecision: " + NextAction);
            }

            // Set the furiten flags on all the players who didn't just move.
            if (updateFuriten)
            {
                foreach (Player p in PlayerHelpers.Players)
                {
                    if (furitenDiscardingPlayer != p) { GetHand(p).UpdateFuriten(furitenDiscardedTile); }
                }
            }
        }

        public void ExecutePostBreak_HandEnd()
        {
            // Process Nagashi Mangan.
            _NagashiWin = false;
            if ((TilesRemaining == 0) && (NextAction == GameAction.Nothing))
            {
                bool player1Nagashi = Player1HandRaw.CheckNagashiMangan();
                bool player2Nagashi = Player2HandRaw.CheckNagashiMangan();
                bool player3Nagashi = Player3HandRaw.CheckNagashiMangan();
                bool player4Nagashi = Player4HandRaw.CheckNagashiMangan();

                int nagashiCount = (player1Nagashi ? 1 : 0) + (player2Nagashi ? 1 : 0) + (player3Nagashi ? 1 : 0) + (player4Nagashi ? 1 : 0);
                _NagashiWin = (nagashiCount != 0);

                if (nagashiCount > 1)
                {
                    NextActionPlayer = Player.Multiple;
                    _NextAction1 = player1Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction2 = player2Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction3 = player3Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction4 = player4Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextActionPlayerTarget = Dealer.GetPrevious(); // Start at the player before the dealer so that the dealer gets resolved first.
                }
                else if (_NagashiWin)
                {
                    NextAction = GameAction.Tsumo;
                    NextActionPlayer = player1Nagashi ? Player.Player1 :
                                       player2Nagashi ? Player.Player2 :
                                       player3Nagashi ? Player.Player3 :
                                                        Player.Player4;
                }
            }

            // Determine our state delta.
            WinResultsImpl[] multiWinResults = null;
            bool consumePool = !_NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiConsumesPool);
            int bonus = (!_NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiUsesBonus)) ? Bonus : 0;

            if (NextActionPlayer == Player.Multiple)
            {
                int winCount = 0;
                foreach (Player p in PlayerHelpers.Players) { if (GetNextAction(p).IsAgari()) { ++winCount; } }
                multiWinResults = new WinResultsImpl[winCount];

                Player processPlayer = _NagashiWin ? Dealer : _NextActionPlayerTarget;
                int resultSlot = 0;
                for (int i = 0; i < 4; ++i, processPlayer = processPlayer.GetNext())
                {
                    HandImpl hand = GetHand(processPlayer);
                    WinType winType = GetNextAction(processPlayer).GetWinType();
                    if ((winType == WinType.Ron) || (winType == WinType.Tsumo))
                    {
                        hand.Streak++;
                        hand.Yakitori = false;
                        multiWinResults[resultSlot++] = new WinResultsImpl(this,
                                                                           processPlayer,
                                                                           ((winType == WinType.Tsumo) ? Player.Multiple : _NextActionPlayerTarget),
                                                                           PlayerRecentOpenKan,
                                                                           winType,
                                                                           bonus,
                                                                           (consumePool ? ConsumePool() : 0));
                    }
                    else
                    {
                        hand.Streak = 0;
                    }
                }
            }
            else if (NextAction.IsAgari())
            {
                _WinResultCache.Populate(this,
                                         NextActionPlayer,
                                         ((NextAction == GameAction.Ron) ? _NextActionPlayerTarget : Player.Multiple),
                                         PlayerRecentOpenKan,
                                         NextAction.GetWinType(),
                                         bonus,
                                         (consumePool ? ConsumePool() : 0));

                HandImpl winner = GetHand(NextActionPlayer);
                winner.Streak++;
                winner.Yakitori = false;
                foreach (Player p in PlayerHelpers.Players) { if (p != NextActionPlayer) { GetHand(p).Streak = 0; } }
            }
            else if (NextAction == GameAction.Nothing)
            {
                _WinResultCache.Reset();
                _WinResultCache.WinningPlayer = Player.Multiple;
                _WinResultCache.Action = WinType.Draw;

                // Draw game! Determine score outputs. No warame in this situation because the math doesn't work.
                bool p1Tempai = Player1Hand.Tempai;
                bool p2Tempai = Player2Hand.Tempai;
                bool p3Tempai = Player3Hand.Tempai;
                bool p4Tempai = Player4Hand.Tempai;
                int tempaiCount = (p1Tempai ? 1 : 0) + (p2Tempai ? 1 : 0) + (p3Tempai ? 1 : 0) + (p4Tempai ? 1 : 0);

                if ((tempaiCount == 1) || (tempaiCount == 2) || (tempaiCount == 3))
                {
                    int deltaGainPoints = 3000 / tempaiCount;
                    int deltaLosePoints = -(3000 / (4 - tempaiCount));

                    _WinResultCache.Player1Delta = (p1Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player2Delta = (p2Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player3Delta = (p3Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player4Delta = (p4Tempai ? deltaGainPoints : deltaLosePoints);
                }

                foreach (Player p in PlayerHelpers.Players) { GetHand(p).Streak = 0; }
            }

            // Apply deltas to the game state.
            if (NextActionPlayer == Player.Multiple) { foreach (WinResultsImpl result in multiWinResults) { ApplyWinResultsToPlayerScores(result); } }
            else                                     { ApplyWinResultsToPlayerScores(_WinResultCache); }

            // Notify the sink.
            if      (NextActionPlayer == Player.Multiple)   { MultiWin?.Invoke(this, new MultiWinArgsImpl(multiWinResults)); }
            else if (NextAction == GameAction.Tsumo)        { HandTsumo?.Invoke(this, new HandTsumoArgsImpl(_WinResultCache)); }
            else if (NextAction == GameAction.Ron)          { HandRon?.Invoke(this, new HandRonArgsImpl(_WinResultCache)); }
            else if (NextAction == GameAction.Nothing)      { ExhaustiveDraw?.Invoke(this, new ExhaustiveDrawArgsImpl(_WinResultCache); }
            else if (NextAction == GameAction.AbortiveDraw) { AbortiveDraw?.Invoke(this, new AbortiveDrawArgsImpl(_NextAbortiveDrawType)); }
            else                                            { throw new Exception("Unexpected hand end state!"); }
        }

        public void ExecutePostBreak_TableCleanup()
        {
            // If the game is done, then goto GameEnd. Otherwise reset for the next round.
            Round endRound      = Settings.GetSetting<bool>(GameOption.Tonpussen) ? Round.East4 : Round.South4;
            int dealerPoints    = GetHand(Dealer).Score;
            int checkScore      = Settings.GetSetting<int>(GameOption.VictoryPoints);
            bool knockedOut     = Settings.GetSetting<bool>(GameOption.Buttobi) && ((Player1Hand.Score < 0) || (Player2Hand.Score < 0) || (Player3Hand.Score < 0) || (Player4Hand.Score < 0));
            bool dealerInLead   = ((Dealer == Player.Player1) || (dealerPoints > Player1Hand.Score)) &&
                                  ((Dealer == Player.Player2) || (dealerPoints > Player2Hand.Score)) &&
                                  ((Dealer == Player.Player3) || (dealerPoints > Player3Hand.Score)) &&
                                  ((Dealer == Player.Player4) || (dealerPoints > Player4Hand.Score));
            bool dealerWon      = ((NextActionPlayer == Player.Multiple) && GetNextAction(Dealer).IsAgari()) ||
                                  ((NextActionPlayer == Dealer) && NextAction.IsAgari());
            bool endGameRenchan = (Round == endRound) &&
                                  dealerWon &&
                                  (!dealerInLead || !Settings.GetSetting<bool>(GameOption.EndgameDealerFinish));

            if (knockedOut
                ||
                ((Round >= endRound) || Lapped) &&
                 !endGameRenchan &&
                 ((Player1Hand.Score >= checkScore) || (Player2Hand.Score >= checkScore) || (Player3Hand.Score >= checkScore) || (Player4Hand.Score >= checkScore)))
            {
                _AdvanceAction = AdvanceAction.GameEnd;
            }
            else
            {
                // Clean up the wall, discards, hands, etc.
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).Reset(); }
                DiscardPlayerList.Clear();

                for (int i = 0; i < DoraIndicators.Length; ++i)        { DoraIndicatorsRaw[i] = null; }
                for (int i = 0; i < UraDoraIndicators.Length; ++i)     { UraDoraIndicatorsRaw[i] = null; }
                foreach (TileImpl tile in WallRaw)                     { tile.Type = TileType.None;
                                                                         tile.Ghost = false; }

                // Determine bonus and dealer.
                bool advanceBonus = (NextActionPlayer == Player.Multiple) ? GetNextAction(Dealer).IsAgari() :
                                    NextAction.IsAgari()                  ? (NextActionPlayer == Dealer) :
                                                                            true;
                bool advanceDealer;
                if (NextAction == GameAction.Nothing)
                {
                    advanceDealer = !GetHand(Dealer).IsTempai();
                    if (Settings.GetSetting<bool>(GameOption.SouthNotReady))
                    {
                        advanceDealer &= GetHand(Dealer.GetNext()).IsTempai();
                    }
                }
                else
                {
                    if (_NagashiWin && Settings.GetSetting<bool>(GameOption.NagashiBonusOnEastTempaiOnly))
                    {
                        advanceBonus = GetHand(Dealer).IsTempai();
                    }
                    advanceDealer = !advanceBonus;
                }

                if (Settings.GetSetting<bool>(GameOption.EightWinRetire) && !advanceDealer && (Bonus >= 8))
                {
                    advanceDealer = true;
                    Bonus = 0;
                }
                else
                {
                    Bonus = advanceBonus ? (Bonus + 1) : 0;
                }

                if (advanceDealer)
                {
                    Dealer = Dealer.GetNext();
                    Round = Round.GetNext();
                    if (Round == Round.East1)
                    {
                        Lapped = true;
                    }
                }

                // Reset state.
                PlayerRecentOpenKan = Player.None;
                Current = Dealer;
                PrevAction = GameAction.Nothing;
                NextAction = GameAction.Nothing;
                NextActionTile = TileType.None;
                NextActionPlayer = Player.None;
                _NextActionSlot = 0;
                _NextAction1 = GameAction.Nothing;
                _NextAction2 = GameAction.Nothing;
                _NextAction3 = GameAction.Nothing;
                _NextAction4 = GameAction.Nothing;
                _NextActionPlayerTarget = Player.None;
                PlayerDeadWallPick = false;

                TableCleared?.Invoke(this, null);

                // Start the next state.
                _AdvanceAction = AdvanceAction.RandomizingBreak;
            }
        }

        public void ExecutePostBreak_KanChosenTile()
        {
            // Reset ippatsu flag after kan, unless RinshanIppatsu is active and it was a closed kan for the current player.
            foreach (Player p in PlayerHelpers.Players)
            {
                GetHand(p).CouldIppatsu = (NextAction == GameAction.ClosedKan) && Settings.GetSetting<bool>(GameOption.RinshanIppatsu) && (Current == p);
            }

            if (DoraCount == 5)
            {
                // This is the 5th kan. Immediately abort.
                NextAction = GameAction.AbortiveDraw;
                NextActionPlayer = Current;
                _AdvanceAction = AdvanceAction.HandEnd;
            }
            else
            {
                // NextAction is PromotedKan or ClosedKan.
                Global.Assert((NextAction == GameAction.PromotedKan) || (NextAction == GameAction.ClosedKan));

                // We will use this to see if we can ron on the chankan in the case of a promoted can or ron
                // with a kokushi in the case of a closed kan. Set PrevAction to the type of kan.
                PrevAction = NextAction;

                _NextAction1 = (Current == Player.Player1) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction2 = (Current == Player.Player2) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction3 = (Current == Player.Player3) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction4 = (Current == Player.Player4) ? GameAction.Nothing : GameAction.DecisionPending;
                NextActionPlayer = Current;

                // We'll use PostDiscardDecisionGather again, but because of PrevAction we'll only look for the applicable rons.
                ChankanFlag = true;
                Sink.GatherPostKanDecisions();
            }
        }

        public void ExecutePostBreak_KanPerformDecision()
        {
            ChankanFlag = false; // Can clear this flag now. Chankan has already been evaluated.

            if (NextAction == GameAction.Nothing)
            {
                // Set the kanburi flag. After the next discard is done, we will clear this flag.
                KanburiFlag = true;

                // No chankan. Continue. PrevAction will have the type of kan that was done.
                NextAction = GameAction.ReplacementTilePick;
                _SkipAdvancePlayer = true;
                StartPlayState(PlayState.PrePickTile);
            }
            else if (NextAction == GameAction.Ron)
            {
                // GameState has been set as to who is getting ronned on or about the tsumo. That game state will handle it.
                StartPlayState(PlayState.HandEnd);
            }
            else
            {
                Global.Assert(false);
            }
        }

        public void ExecutePostBreak_GameEnd()
        {
            // Determine first dealer.
            int roundOffset = Round.GetOffset();
            Player startDealer = Dealer.AddOffset(roundOffset);

            // Give the remaining reach sticks to the winner.
            int extraWinnerPoints = Settings.GetSetting<bool>(GameOption.WinnerGetsPool) ? ConsumePool() : 0;

            // Generate a GameResults and submit it to GameComplete.
            GameComplete?.Invoke(this, new GameCompleteArgsImpl(new GameResultsImpl(Settings,
                                                                                    FirstDealer,
                                                                                    Player1Hand.Score,
                                                                                    Player2Hand.Score,
                                                                                    Player3Hand.Score,
                                                                                    Player4Hand.Score,
                                                                                    extraWinnerPoints,
                                                                                    Player1Hand.Yakitori,
                                                                                    Player2Hand.Yakitori,
                                                                                    Player3Hand.Yakitori,
                                                                                    Player4Hand.Yakitori)));
        }

        private void PerformRoundEndRewindStep()
        {
            // If we're done with this round, rewind us before the previous ron or tsumo.
            if      (NextAction == GameAction.Ron)   { State = PlayState.GatherDecisions; }
            else if (NextAction == GameAction.Tsumo) { State = PlayState.DecideMove; }
            else                                     { throw new Exception("Unexpected state for rewinding!"); }

            WinUndone?.Invoke(this, new WinUndoneArgsImpl(Current));
        }

        public void ExecuteRewindModeChange_DecideMove()
        {
            Global.Assert((_RewindAction != GameAction.Nothing), "Expected _RewindAction to not be Nothing, but it's: " + _RewindAction);
            State = (_RewindAction == GameAction.OpenKan) || // RewindPlayerDrawOrCall.RewindAction....
                    (_RewindAction == GameAction.Chii)    ||
                    (_RewindAction == GameAction.Pon)            ? PlayState.PerformDecision :
                    (_RewindAction == GameAction.PickedFromWall) ? PlayState.PickTile :
                                                                    PlayState.KanChosenTile;
        }

        public void ExecuteRewindPostModeChange_DecideMove()
        {
            // Current player has a full hand. Decide to return a tile to the wall OR undo an open call, closed kan, or
            // promoted kan. We don't do anything at this time, we just determine what we're going to rewind next time.
            bool shouldRewindPonOrChiiOrOpenKan = false; // TODO: Implement this if it becomes necessary.
            if (shouldRewindPonOrChiiOrOpenKan)
            {
                MeldImpl meld = GetHand(Current).GetLatestMeld();
                _RewindAction = (meld.State == MeldState.KanOpen) ? GameAction.OpenKan :
                                (meld.State == MeldState.Chii)    ? GameAction.Chii :
                                                                    GameAction.Pon; 
            }
            else
            {
                _RewindAction = GetHand(Current).PeekLastDrawKanType();
            }

        }

        public void ExecuteRewindPostModeChange_GatherDecisions()
        {
            // Undo the most recent discard.
            Hand discardPlayerHand = GetHand(DiscardPlayerList.Pop());
            ExtendedTile undoneDiscard = discardPlayerHand.Discards.Pop();

            // Add the tile back into the discarding player's hand. Report the undone discard.
            discardPlayerHand.RewindDiscardTile(undoneDiscard.Tile);
            Sink.DiscardUndone(discardPlayerHand.Player, undoneDiscard.Tile);
        }

        public void ExecuteRewindPostModeChange_PerformDecision()
        {
            // A previous player had their tile called (chii, pon, or kan). Undo this. OR it was a ron.
            // First, set the current player to that player. If it's not a ron.
            // TODO: this
        }

        public void ExecuteRewindPostModeChange_KanChosenTile()
        {
            // Current player needs to undo a promoted kan or closed kan.
            // TODO: this
         }

        public void ExecuteRewindPostModeChange_NextTurn()
        {
            // Rewind to the previous player. Note that if we has just performed a call we need to skip to them.
            // TODO: Do something if it was a call.
            Current = Current.GetPrevious();
        }

        public void ExecuteRewindPostModeChange_PickTile()
        {
            // Remove the last drawn tile from the current player's hand.
            HandImpl hand = GetHand(Current);
            ICommand command = hand.PeekLastDrawKan();

            Global.Log("draw to rewind: " + command.Tile.Type + " reaminig drawnskan count " + hand.DrawsAndKans.Count);
            Global.Assert(tc.CommandType == TileCommand.Type.Tile, "RewindPMC_PickTile, expected tile, found: " + tc.CommandType);

            bool succeeded = hand.RewindAddTile(tc.TilePrimary.Tile);
            Global.Assert(succeeded, "Failed to rewind add tile!! Tile: " + tc.TilePrimary.Tile);

            // Increase the number of tiles remaining. Ensure that the tiles match.
            TilesRemaining++;
            int slot = TileHelpers.ClampTile(Offset + (122 - TilesRemaining));
            TileType wallTile = Wall[slot];
            Global.Assert(wallTile.IsEqual(tc.TilePrimary.Tile), "Tile that was rewinded isn't the same tile from the wall! rewind tile: " + tc.TilePrimary.Tile + " wall tile: " + wallTile + " at slot: " + slot);

            // Notify the sink that this happened.
            TilePickUndone?.Invoke(this, new TilePickUndoneArgsImpl(Current, command.Tile));
        }

        private int ConsumePool()
        {
            int poolValue = Pool;
            Pool = 0;
            return poolValue;
        }

        private void PickIntoPlayerHand(Player p, int count)
        {
            bool flipDora = false;
            if (count == 4)
            {
                int[] pickedTiles = new int[4];
                for (int i = 0; i < 4; ++i)
                {
                    pickedTiles[i] = PickIntoPlayerHand(p, TileSource.WallDraw);
                }
                Sink.WallTilePicked(pickedTiles, TileSource.WallDraw);
            }
            else
            {
                // Update PrevAction and determine our source.
                GameAction oldPrevAction = PrevAction;
                PrevAction = (NextAction == GameAction.ReplacementTilePick) ? GameAction.ReplacementTilePick : GameAction.PickedFromWall;
                var source = (NextAction == GameAction.ReplacementTilePick) ? TileSource.ReplacementTileDraw : TileSource.WallDraw;

                int pickedTileSlot = PickIntoPlayerHand(p, source);
                Sink.WallTilePicked(new int[] { pickedTileSlot }, source);

                // If we just did a closed kan, then flip over a dora tile now.
                // If we did a different kan, set the flag to flip the dora after the next discard.
                if (Settings.GetSetting<bool>(GameOption.KanDora) && (source == TileSource.ReplacementTileDraw))
                {
                    // TODO: When we add a setting to always flip after a dora immediately, always flip the dora now.
                    if (oldPrevAction == GameAction.ClosedKan)
                    {
                        flipDora = true;
                    }
                    else
                    {
                        FlipDoraAfterNextDiscard = true;
                    }
                }
            }

            Sink.HandTileAdded(CurrentPlayer, count);

            // Flip the dora after we've reported that the hand tile was added.
            if (flipDora) { FlipDora(); }
        }

        private int PickIntoPlayerHand(Player p, TileSource source)
        {
            // Get the tile from the wall to pick from.
            Global.Assert(source != TileSource.Call);
            int tileNumber;
            if (source == TileSource.DeadWall)
            {
                // Pick from the dead wall.
                PlayerDeadWallPick = true;
                tileNumber = Offset - ((DoraCount == 1) ? 2 :
                                       (DoraCount == 2) ? 1 :
                                       (DoraCount == 3) ? 4 : 3);
            }
            else
            {
                // Pick from the regular wall.
                tileNumber = TileHelpers.ClampTile(Offset + (122 - TilesRemaining));
                TilesRemaining--;
            }

            // Move the tile into the target player's hand.
            Global.Log("PickIntoPlayerHand! Player: " + p + " Slot: " + tileNumber + " tile: " + Wall[tileNumber]);

            GetHand(p).AddTile(Wall[tileNumber].Type);
            return tileNumber;
        }

        internal HandImpl GetHand(Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get hand for non-player: " + p);
            return (p == Player.Player1) ? Player1HandRaw :
                   (p == Player.Player2) ? Player2HandRaw :
                   (p == Player.Player3) ? Player3HandRaw :
                                           Player4HandRaw;
        }

        private IPlayerAI GetAI(Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get hand for non-player: " + p);
            return (p == Player.Player1) ? Player1AI :
                   (p == Player.Player2) ? Player2AI :
                   (p == Player.Player3) ? Player3AI :
                                           Player4AI;
        }

        private GameAction GetNextAction(Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get action for non-player: " + p);
            return (p == Player.Player1) ? _NextAction1 :
                   (p == Player.Player2) ? _NextAction2 :
                   (p == Player.Player3) ? _NextAction3 :
                                           _NextAction4;
        }

        private void SetNextAction(Player p, GameAction action)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get action for non-player: " + p);
            if      (p == Player.Player1) { _NextAction1 = action; }
            else if (p == Player.Player2) { _NextAction2 = action; }
            else if (p == Player.Player3) { _NextAction3 = action; }
            else                          { _NextAction4 = action; }
        }

        internal void PopulateDoraIndicators()
        {
            DoraIndicatorsRaw[0] = WallRaw[TileHelpers.ClampTile(Offset - 6)];
            DoraIndicatorsRaw[1] = WallRaw[TileHelpers.ClampTile(Offset - 8)];
            DoraIndicatorsRaw[2] = WallRaw[TileHelpers.ClampTile(Offset - 10)];
            DoraIndicatorsRaw[3] = WallRaw[TileHelpers.ClampTile(Offset - 12)];
            DoraIndicatorsRaw[4] = WallRaw[TileHelpers.ClampTile(Offset - 14)];
            UraDoraIndicatorsRaw[0] = WallRaw[TileHelpers.ClampTile(Offset - 5)];
            UraDoraIndicatorsRaw[1] = WallRaw[TileHelpers.ClampTile(Offset - 7)];
            UraDoraIndicatorsRaw[2] = WallRaw[TileHelpers.ClampTile(Offset - 9)];
            UraDoraIndicatorsRaw[3] = WallRaw[TileHelpers.ClampTile(Offset - 11)];
            UraDoraIndicatorsRaw[4] = WallRaw[TileHelpers.ClampTile(Offset - 13)];
        }

        private void PerformDiscardState(ITile tile, GameAction nextAction)
        {
            NextAction = nextAction;
            NextActionTile = tile.Type;
            _NextActionSlot = tile.Slot;
            PlayerRecentOpenKan = Player.None;

            DiscardPlayerList.Push(Current);
            GetHand(Current).CouldIppatsu = false;
        }
    }
}
