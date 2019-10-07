// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MahjongCore.Riichi.Impl
{
    internal enum AdvanceAction
    {
        Done,
        Advance,
        KanChosenTile,
        HandEnd,
        GameEnd,
        ReplacementPickTile,
        DecidePostCallMove,
        RandomizingBreak,
        GatherDecisions,
        PrePickTile
    }

    internal class GameStateImpl : IGameState
    {
        // IGameState
        public event Action<Player,int>               WallPicking;
        public event Action<ITile[], TileSource>      WallPicked;
        public event Action<Player, ITile>            WallPickUndone;
        public event Action<IWinResult>               Ron;
        public event Action<IWinResult>               Tsumo;
        public event Action<Player>                   WinUndone;
        public event Action<IDiscardInfo>             DiscardRequested;
        public event Action<IPostDiscardInfo>         PostDiscardRequested;
        public event Action<IPostDiscardInfo>         PostKanRequested;
        public event Action<IWinResult[]>             MultiWin;
        public event Action<IWinResult>               ExhaustiveDraw;
        public event Action<Player, AbortiveDrawType> AbortiveDraw;
        public event Action<IGameResult>              GameComplete;
        public event Action                           DiceRolled;
        public event Action                           DeadWallMoved;
        public event Action<ITile>                    DoraIndicatorFlipped;
        public event Action                           PreCheckAdvance;
        public event Action                           TableCleared;
        public event Action                           PreCheckRewind;
        public event Action<Player, IMeld>            DecisionCancelled;
        public event Action<Player>                   Chombo;

        public ITile[]          Wall               { get { return WallRaw; } }
        public ITile[]          DoraIndicators     { get { return DoraIndicatorsRaw; } }
        public ITile[]          UraDoraIndicators  { get { return UraDoraIndicatorsRaw; } }
        public Round            Round              { get; internal set; } = Round.East1;
        public Player           FirstDealer        { get; internal set; } = Player.None;
        public Player           Dealer             { get; internal set; } = Player.None;
        public Player           Current            { get; internal set; } = Player.None;
        public Player           Wareme             { get; internal set; } = Player.None;
        public PlayState        State              { get; internal set; } = PlayState.PreGame;
        public GameAction       NextAction         { get; internal set; } = GameAction.Nothing;
        public GameAction       PreviousAction     { get; internal set; } = GameAction.Nothing;
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

        public ISaveState Save()         { return new SaveStateImpl(this); }
        private void FireGameResultNow() { ExecutePostBreak_GameEnd(); }

        public void Start()
        {
            CommonHelpers.Check(CanStart, "Can't call start!");
            CanStart = false;
            CanAdvance = true;
            Advance();
        }

        public void Advance()
        {
            CommonHelpers.Check(CanAdvance, "Advance reentry detected! Cannot call advance at this time.");
            Advance(State.GetNext(), EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(State.GetNext()), false);
        }

        public void Resume()
        {
            CommonHelpers.Check(CanResume, "Cannot resume, as this hasn't been paused.");
            Advance(State, false, true);
        }

        public void Rewind()
        {
            if (!CheckForPause(true))
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
            CommonHelpers.Check(ExpectingPause, "Not expecting pause at this time. Can only pause during PreCheckAdvance or PreCheckRewind handlers.");
            ShouldPause = true;
            CanResume = true;
        }

        public void SubmitDiscard(IDiscardDecision decision)
        {
            CommonHelpers.Check(ExpectingDiscard, "Not expecting discard decision at this time!");
            ExpectingDiscard = false;

            // If we're currently processing DecideMove (evidenced by the CanAdvance reentry flag being cleared)
            // then we want to setup _AdvanceAction. Otherwise we want to pump state advancement at this time.
            AdvanceAction = ProcessDiscardDecision(decision);
            if (CanAdvance)
            {
                DetermineAdvanceState(AdvanceAction, out PlayState state, out bool advancePlayer);
                Advance(state, advancePlayer, false);
            }
        }

        public void SubmitPostDiscard(IPostDiscardDecision decision)
        {
            CommonHelpers.Check(((GetNextAction(decision.Player) == GameAction.DecisionPending) && (State == PlayState.GatherDecisions)),
                                "Not expecting post discard decision at this time.");

            // If we're currently processing DecideMove (evidenced by the CanAdvance reentry flag being cleared)
            // then we just want to leave _AdvanceAction set. Otherwise we want to pump state advancement at this time.
            AdvanceAction = ProcessPostDiscardDecision(decision);
            if (CanAdvance && (AdvanceAction == AdvanceAction.Advance))
            {
                Advance();
            }
        }

        public void SubmitResultCommand(IResultCommand command)
        {
            CommonHelpers.Check((command.Action != ResultAction.Invalid), "Found invalid command.");

            // Process per command logic.
            bool advanceRounds = false;

            var commandImpl = command as ResultCommandImpl;
            if      (commandImpl.Action == ResultAction.Tsumo)          { advanceRounds = true; ProcessTsumoCommand(commandImpl.Winner, commandImpl.Han, commandImpl.Fu); }
            else if (commandImpl.Action == ResultAction.Ron)            { advanceRounds = true; ProcessRonCommand(commandImpl.Winner, commandImpl.Target, commandImpl.Han, commandImpl.Fu); }
            else if (commandImpl.Action == ResultAction.Draw)           { advanceRounds = true; ProcessExhaustiveDrawCommand(commandImpl.Player1Tempai, commandImpl.Player2Tempai, commandImpl.Player3Tempai, commandImpl.Player4Tempai); }
            else if (commandImpl.Action == ResultAction.AbortiveDraw)   { advanceRounds = true; ProcessAbortiveDrawCommand(); }
            else if (commandImpl.Action == ResultAction.Chombo)         { advanceRounds = true; ProcessChomboCommand(commandImpl.Target); }
            else if (commandImpl.Action == ResultAction.MultiWin)       { advanceRounds = true; ProcessMultiWinCommand(commandImpl.MultiWins); }
            else if (commandImpl.Action == ResultAction.FireGameResult) { FireGameResultNow(); }
            else                                                        { throw new Exception("Unexpected result command."); }

            // Advance to the next round all through table cleanup.
            if (advanceRounds)
            {
                ExecutePostBreak_HandEnd();
                ExecutePostBreak_TableCleanup();

                DetermineAdvanceState(AdvanceAction, out PlayState nextState, out bool dummy);
                State = nextState;
            }
        }

        public void SubmitOverride(OverrideState key, object value)
        {
            if      (key == OverrideState.Pool)    { Pool = (int)value; }
            else if (key == OverrideState.Bonus)   { Bonus = (int)value; }
            else if (key == OverrideState.Lapped)  { Lapped = (bool)value; }
            else if (key == OverrideState.Roll)    { Roll = (int)value; }
            else if (key == OverrideState.Board)   { ProcessBoardOverride((IBoardTemplate)value); }
            else if (key == OverrideState.Current) { Current = (Player)value; }
            else if (key == OverrideState.DoraCount)
            {
                var intVal = (int)value;
                CommonHelpers.Check(intVal >= 0, "Can't be below zero.");
                DoraCount = intVal;
            }
            else if (key == OverrideState.Round)
            {
                Round oldRound = Round;
                Round = (Round)value;

                int playerOffsetDelta = Round.GetOffset() - oldRound.GetOffset();
                Current = Current.AddOffset(playerOffsetDelta);
                Dealer = Dealer.AddOffset(playerOffsetDelta);
            }
            else if (key == OverrideState.WallTile)
            {
                ITile tile = (ITile)value;
                WallRaw[tile.Slot].Type = tile.Type;
                // TODO: Fix up entire wall to correct for this tile getting set specifically.
            }
            else if (key == OverrideState.Dealer)
            {
                Dealer = (Player)value;
                FirstDealer = Dealer.AddOffset(-Round.GetOffset());
            }
            else if (key == OverrideState.FirstDealer)
            {
                FirstDealer = (Player)value;
                Dealer = FirstDealer.AddOffset(Round.GetOffset());
            }
            else if (key == OverrideState.Wareme)
            {
                CommonHelpers.Check((((Player)value == Player.None) || Settings.GetSetting<bool>(GameOption.Wareme)), "Attempting to set wareme player while wareme is disabled!");
                Wareme = (Player)value;
            }
            else { throw new Exception("Unrecognized override option: " + key); }
    }

        // IComparable<IGameState>
        public int CompareTo(IGameState other)
        {
            int value = Round.CompareTo(other.Round);               if (value != 0) { return value; }
            value = FirstDealer.CompareTo(other.FirstDealer);       if (value != 0) { return value; }
            value = Dealer.CompareTo(other.Dealer);                 if (value != 0) { return value; }
            value = Current.CompareTo(other.Current);               if (value != 0) { return value; }
            value = Wareme.CompareTo(other.Wareme);                 if (value != 0) { return value; }
            value = State.CompareTo(other.State);                   if (value != 0) { return value; }
            value = NextAction.CompareTo(other.NextAction);         if (value != 0) { return value; }
            value = PreviousAction.CompareTo(other.PreviousAction); if (value != 0) { return value; }
            value = Player1Hand.CompareTo(other.Player1Hand);       if (value != 0) { return value; }
            value = Player2Hand.CompareTo(other.Player2Hand);       if (value != 0) { return value; }
            value = Player3Hand.CompareTo(other.Player3Hand);       if (value != 0) { return value; }
            value = Player4Hand.CompareTo(other.Player4Hand);       if (value != 0) { return value; }
            value = Settings.CompareTo(other.Settings);             if (value != 0) { return value; }
            value = ExtraSettings.CompareTo(other.ExtraSettings);   if (value != 0) { return value; }
            value = Lapped.CompareTo(other.Lapped);                 if (value != 0) { return value; }
            value = Offset.CompareTo(other.Offset);                 if (value != 0) { return value; }
            value = TilesRemaining.CompareTo(other.TilesRemaining); if (value != 0) { return value; }
            value = Bonus.CompareTo(other.Bonus);                   if (value != 0) { return value; }
            value = Pool.CompareTo(other.Pool);                     if (value != 0) { return value; }
            value = DoraCount.CompareTo(other.DoraCount);           if (value != 0) { return value; }
            value = Roll.CompareTo(other.Roll);                     if (value != 0) { return value; }
            return 0;
        }

        // IClonable
        public object Clone() { return new GameStateImpl(Save()); }

        // GameStateImpl
        internal TileImpl[]       WallRaw                  { get; set; } = new TileImpl[TileHelpers.TOTAL_TILE_COUNT];
        internal TileImpl[]       DoraIndicatorsRaw        { get; set; } = new TileImpl[5];
        internal TileImpl[]       UraDoraIndicatorsRaw     { get; set; } = new TileImpl[5];
        internal HandImpl         Player1HandRaw           { get; set; }
        internal HandImpl         Player2HandRaw           { get; set; }
        internal HandImpl         Player3HandRaw           { get; set; }
        internal HandImpl         Player4HandRaw           { get; set; }
        internal Stack<Player>    DiscardPlayerList        { get; set; } = new Stack<Player>();
        internal Player           PlayerRecentOpenKan      { get; set; } = Player.None;
        internal Player           NextActionPlayer         { get; set; } = Player.None;
        internal TileType         NextActionTile           { get; set; } = TileType.None;
        internal Player           NextActionPlayerTarget   { get; set; } = Player.None;
        internal GameAction       NextAction1              { get; set; } = GameAction.Nothing;
        internal GameAction       NextAction2              { get; set; } = GameAction.Nothing;
        internal GameAction       NextAction3              { get; set; } = GameAction.Nothing;
        internal GameAction       NextAction4              { get; set; } = GameAction.Nothing;
        internal GameAction       RewindAction             { get; set; } = GameAction.Nothing;
        internal AdvanceAction    AdvanceAction            { get; set; } = AdvanceAction.Done;
        internal AbortiveDrawType NextAbortiveDrawType     { get; set; } = AbortiveDrawType.Other;
        internal bool             PlayerDeadWallPick       { get; set; } = false;
        internal bool             FlipDoraAfterNextDiscard { get; set; } = false;
        internal bool             ChankanFlag              { get; set; } = false;
        internal bool             KanburiFlag              { get; set; } = false;
        internal bool             SkipAdvancePlayer        { get; set; } = false;
        internal bool             HasExtraSettings         { get; set; } = false;
        internal bool             ExpectingPause           { get; set; } = false;
        internal bool             ShouldPause              { get; set; } = false;
        internal bool             CanAdvance               { get; set; } = false;
        internal bool             CanResume                { get; set; } = false;
        internal bool             CanStart                 { get; set; } = true;
        internal bool             ExpectingDiscard         { get; set; } = false;
        internal bool             NagashiWin               { get; set; } = false;
        internal int              NextActionSlot           { get; set; } = -1;

        private readonly Dictionary<PlayState, Action> _PreBreakStateHandlers   = new Dictionary<PlayState, Action>();
        private readonly Dictionary<PlayState, Action> _PostBreakStateHandlers  = new Dictionary<PlayState, Action>();
        private readonly Dictionary<PlayState, Action> _RewindPreHandlers       = new Dictionary<PlayState, Action>();
        private readonly Dictionary<PlayState, Action> _RewindPostHandlers      = new Dictionary<PlayState, Action>();
        private WinResultImpl                          _WinResultCache          = new WinResultImpl();
        private DiscardInfoImpl                        _DiscardInfoCache        = new DiscardInfoImpl();
        private PostDiscardInfoImpl                    _PostDiscardInfoCache    = new PostDiscardInfoImpl();
        private PostDiscardDecisionImpl                _CachedPostDiscardPass   = new PostDiscardDecisionImpl();

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
        public void  ExecutePostBreak_PreTilePick1()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick2()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick3()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick4()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick5()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick6()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick7()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick8()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick9()                       { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick10()                      { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick11()                      { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick12()                      { WallPicking?.Invoke(Current, 4); }
        public void  ExecutePostBreak_PreTilePick13()                      { WallPicking?.Invoke(Current, 1); }
        public void  ExecutePostBreak_PreTilePick14()                      { WallPicking?.Invoke(Current, 1); }
        public void  ExecutePostBreak_PreTilePick15()                      { WallPicking?.Invoke(Current, 1); }
        public void  ExecutePostBreak_PreTilePick16()                      { WallPicking?.Invoke(Current, 1); }
        public void  ExecutePostBreak_PrePickTile()                        { WallPicking?.Invoke(Current, 1); }
        public void  ExecutePostBreak_TilePick1()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick2()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick3()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick4()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick5()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick6()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick7()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick8()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick9()                          { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick10()                         { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick11()                         { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick12()                         { PickIntoPlayerHand(Current, 4); }
        public void  ExecutePostBreak_TilePick13()                         { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick14()                         { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick15()                         { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_TilePick16()                         { PickIntoPlayerHand(Current, 1); }
        public void  ExecutePostBreak_PickTile()                           { PickIntoPlayerHand(Current, 1); }
        private void ApplyWinResultsToPlayerScores(WinResultImpl win)      { foreach (Player p in PlayerHelpers.Players) { GetHand(p).Score += win.GetPlayerDelta(p) + win.GetPlayerPoolDelta(p); } }
        internal int GetNextWallDrawSlot(int offset = 0)                   { return TileHelpers.ClampTile(Offset + (TileHelpers.MAIN_WALL_TILE_COUNT - TilesRemaining) - Math.Max(0, (DoraCount - 1)) + offset); } // TODO: Fix for if kan dora are disabled...

        public void ExecutePreBreak_DeadWallMove()
        {
            Player1HandRaw.Sort(true);
            Player2HandRaw.Sort(true);
            Player3HandRaw.Sort(true);
            Player4HandRaw.Sort(true);
        }

        public void ExecutePostBreak_RandomizingBreak()
        {
            Roll                     = (ExtraSettings.OverrideDiceRoll != null) ? ExtraSettings.OverrideDiceRoll.Value : (Global.RandomRange(1, 7) + Global.RandomRange(1, 7));
            Offset                   = GameStateHelpers.GetOffset(Dealer, Roll);
            TilesRemaining           = TileHelpers.MAIN_WALL_TILE_COUNT;
            DoraCount                = 0;
            Wareme                   = Settings.GetSetting<bool>(GameOption.Wareme) ? Dealer.AddOffset(Roll - 1) : Player.None;
            PlayerRecentOpenKan      = Player.None;
            PlayerDeadWallPick       = false;
            FlipDoraAfterNextDiscard = false;

            if (ExtraSettings.OverrideDiceRoll == null)
            {
                TileHelpers.GetRandomBoard(WallRaw, Settings.GetSetting<RedDora>(GameOption.RedDoraOption));
            }

            DiceRolled?.Invoke();
            PopulateDoraIndicators();
        }

        public void ExecutePostBreak_DeadWallMove()
        {
            FlipDora();
            DeadWallMoved?.Invoke();
        }

        public void ExecutePostBreak_DecideMove()
        {
            // Populate the discard info and then clear OverrideNoReach, which may get used during Populate.
            HandImpl hand = GetHand(Current);
            _DiscardInfoCache.Populate(hand);
            hand.OverrideNoReachFlag = false;

            // Query the AI for the discard decision, or raise the event requesting for the discard decision. If we 
            // can query the AI or the discard decision is supplied immediately during the event, then _AdvanceAction
            // will get set. Otherwise, _AdvanceAction will be set to "Done".
            ExpectingDiscard = true;
            if (GameStateHelpers.GetAI(this, Current) is IPlayerAI ai)
            {
                SubmitDiscard(ai.GetDiscardDecision(_DiscardInfoCache));
            }
            else
            {
                AdvanceAction = AdvanceAction.Done;
                DiscardRequested?.Invoke(_DiscardInfoCache);
            }
        }

        public void ExecutePostBreak_GatherDecisions()
        {
            // Set actions as pending except for current player which will make no decision.
            foreach (Player p in PlayerHelpers.Players) { SetNextAction(p, ((Current == p) ? GameAction.Nothing : GameAction.DecisionPending)); }
            NextActionPlayer = Current;

            // Poll decisions from AI, or query decisions from caller. If we populate all actions then _AdvanceAction
            // will get set. Otherwise a later call by the caller to SubmitPostDiscard will pump advancements.
            AdvanceAction = AdvanceAction.Done;
            foreach (Player p in PlayerHelpers.Players) { QueryPostDiscardDecision(GetNextAction(p), GetHand(p), GameStateHelpers.GetAI(this, p)); }
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
                (PreviousAction == GameAction.ReplacementTilePick) &&
                (NextAction != GameAction.Ron) &&
                (DoraCount > 4) &&
                !Player1HandRaw.FourKans && !Player2HandRaw.FourKans && !Player3HandRaw.FourKans && !Player4HandRaw.FourKans)
            {
                // Four kans, no one player has all four kans, and noone ronned on the fourth kan discard. Draw.
                KanburiFlag = false;
                NextAction = GameAction.AbortiveDraw;
                NextActionTile = TileType.None;
                NextActionSlot = -1;
                AdvanceAction = AdvanceAction.HandEnd;
                NextAbortiveDrawType = AbortiveDrawType.FourKans;
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
                    NextActionSlot = -1;
                    AdvanceAction = AdvanceAction.HandEnd;
                    NextAbortiveDrawType = AbortiveDrawType.FourReach;
                }
                else
                {
                    updateFuriten = true;
                    PreviousAction = GameAction.Discard;
                    AdvanceAction = (TilesRemaining == 0) ? AdvanceAction.HandEnd : AdvanceAction.Advance;
                }
            }
            else if ((NextAction == GameAction.Chii) || (NextAction == GameAction.Pon) || (NextAction == GameAction.OpenKan))
            {
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).CouldIppatsu = false; }
                KanburiFlag = false;

                // Make the discarded tile have the Called flag set and also who called it.
                List<TileImpl> targetDiscards = GetHand(Current).DiscardsRaw;
                var targetTile = targetDiscards[targetDiscards.Count - 1];
                targetTile.Called = true;
                targetTile.Ghost = true;
                targetTile.Ancillary = NextActionPlayer;

                // Perform the stored call. This will sort the hand and fire events.
                GetHand(NextActionPlayer).PerformCachedCall();

                // Take action.
                if ((NextAction == GameAction.OpenKan) && (DoraCount == 5))
                {
                    // This is the 5th kan. Immediately abort.
                    NextActionTile = TileType.None;
                    NextActionPlayer = NextActionPlayer;
                    NextAction = GameAction.AbortiveDraw;
                    NextActionSlot = -1;
                    AdvanceAction = AdvanceAction.HandEnd;
                    NextAbortiveDrawType = AbortiveDrawType.FiveKans;
                }
                else
                {
                    // Set PrevAction to the call type to show that we're discarding from a call.
                    PreviousAction = NextAction; // Becomes GameAction.Chii/Pon/OpenKan
                    if (NextAction == GameAction.OpenKan)
                    {
                        PlayerRecentOpenKan = Current; // For Sekinin Barai
                        NextAction = GameAction.ReplacementTilePick;
                        AdvanceAction = AdvanceAction.ReplacementPickTile;
                    }
                    else
                    {
                        AdvanceAction = AdvanceAction.DecidePostCallMove;
                    }

                    updateFuriten = true;
                    Current = NextActionPlayer;
                }
            }
            else if (NextAction == GameAction.Ron)
            {
                NextActionPlayerTarget = Current;
                AdvanceAction = AdvanceAction.HandEnd;
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
                    if (furitenDiscardingPlayer != p) { GetHand(p).UpdateTemporaryFuriten(furitenDiscardedTile); }
                }
            }
        }

        public void ExecutePostBreak_HandEnd()
        {
            // Process Nagashi Mangan.
            NagashiWin = false;
            if ((TilesRemaining == 0) && (NextAction == GameAction.Nothing))
            {
                bool player1Nagashi = Player1HandRaw.CheckNagashiMangan();
                bool player2Nagashi = Player2HandRaw.CheckNagashiMangan();
                bool player3Nagashi = Player3HandRaw.CheckNagashiMangan();
                bool player4Nagashi = Player4HandRaw.CheckNagashiMangan();

                int nagashiCount = (player1Nagashi ? 1 : 0) + (player2Nagashi ? 1 : 0) + (player3Nagashi ? 1 : 0) + (player4Nagashi ? 1 : 0);
                NagashiWin = (nagashiCount != 0);

                if (nagashiCount > 1)
                {
                    NextActionPlayer = Player.Multiple;
                    NextAction1 = player1Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    NextAction2 = player2Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    NextAction3 = player3Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    NextAction4 = player4Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    NextActionPlayerTarget = Dealer.GetPrevious(); // Start at the player before the dealer so that the dealer gets resolved first.
                }
                else if (NagashiWin)
                {
                    NextAction = GameAction.Tsumo;
                    NextActionPlayer = player1Nagashi ? Player.Player1 :
                                       player2Nagashi ? Player.Player2 :
                                       player3Nagashi ? Player.Player3 :
                                                        Player.Player4;
                }
            }

            // Determine our state delta.
            WinResultImpl[] multiWinResults = null;
            bool consumePool = !NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiConsumesPool);
            int bonus = (!NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiUsesBonus)) ? Bonus : 0;

            if (NextActionPlayer == Player.Multiple)
            {
                int winCount = 0;
                foreach (Player p in PlayerHelpers.Players) { if (GetNextAction(p).IsAgari()) { ++winCount; } }
                multiWinResults = new WinResultImpl[winCount];

                Player processPlayer = NagashiWin ? Dealer : NextActionPlayerTarget;
                int resultSlot = 0;
                for (int i = 0; i < 4; ++i, processPlayer = processPlayer.GetNext())
                {
                    HandImpl hand = GetHand(processPlayer);
                    WinType winType = GetNextAction(processPlayer).GetWinType();
                    if ((winType == WinType.Ron) || (winType == WinType.Tsumo))
                    {
                        hand.Streak++;
                        hand.Yakitori = false;
                        multiWinResults[resultSlot++] = new WinResultImpl(this,
                                                                          processPlayer,
                                                                          ((winType == WinType.Tsumo) ? Player.Multiple : NextActionPlayerTarget),
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
                HandImpl winner = GetHand(NextActionPlayer);
                winner.Streak++;
                winner.Yakitori = false;
                foreach (Player p in PlayerHelpers.Players) { if (p != NextActionPlayer) { GetHand(p).Streak = 0; } }

                _WinResultCache.Populate(this,
                                         NextActionPlayer,
                                         ((NextAction == GameAction.Ron) ? NextActionPlayerTarget : Player.Multiple),
                                         PlayerRecentOpenKan,
                                         NextAction.GetWinType(),
                                         bonus,
                                         (consumePool ? ConsumePool() : 0));
            }
            else if (NextAction == GameAction.Chombo)
            {
                // Reset all reaches.
                int reachCount = (Player1Hand.Reach.IsReach() ? 1 : 0) + (Player2Hand.Reach.IsReach() ? 1 : 0) +
                                 (Player3Hand.Reach.IsReach() ? 1 : 0) + (Player4Hand.Reach.IsReach() ? 1 : 0);
                CommonHelpers.Check((reachCount >= Pool), "Reaches don't match pool!");
                Pool -= reachCount;
                Player1HandRaw.ResetForChombo();
                Player2HandRaw.ResetForChombo();
                Player3HandRaw.ResetForChombo();
                Player4HandRaw.ResetForChombo();

                // Adjust scores if it should be done now.
                if (Settings.GetSetting<ChomboType>(GameOption.ChomboTypeOption) == ChomboType.BeforeRanking)
                {
                    var penaltyType = Settings.GetSetting<ChomboPenalty>(GameOption.ChomboPenaltyOption);
                    if (penaltyType == ChomboPenalty.ReverseMangan)
                    {
                        GameStateHelpers.IterateHands(this, (IHand hand) => 
                        {
                            bool dealerScore = (Dealer == NextActionPlayer) || hand.Dealer;
                            (hand as HandImpl).Score += (hand.Player == NextActionPlayer) ? -penaltyType.GetPointLoss() : (dealerScore ? 4000 : 2000);
                        });
                    }
                    else if (penaltyType == ChomboPenalty.Reverse3000All)
                    {
                        GameStateHelpers.IterateHands(this, (IHand hand) => { (hand as HandImpl).Score += (hand.Player == NextActionPlayer) ? -penaltyType.GetPointLoss() : 3000; });
                    }
                    else if ((penaltyType == ChomboPenalty.Penalty8000) ||
                             (penaltyType == ChomboPenalty.Penalty12000) ||
                             (penaltyType == ChomboPenalty.Penalty20000))
                    {
                        GetHand(NextActionPlayer).Score -= penaltyType.GetPointLoss();
                    }
                    else
                    {
                        throw new Exception("Unrecognized chombo type");
                    }
                }

                // Perform the chombo.
                GetHand(NextActionPlayer).PerformChombo();
                
            }
            else if (NextAction == GameAction.Nothing)
            {
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).Streak = 0; }
                _WinResultCache.PopulateDraw(Player1Hand.Tempai, Player2Hand.Tempai, Player3Hand.Tempai, Player4Hand.Tempai);
            }

            // Apply deltas to the game state.
            if (NextActionPlayer == Player.Multiple) { foreach (WinResultImpl result in multiWinResults) { ApplyWinResultsToPlayerScores(result); } }
            else                                     { ApplyWinResultsToPlayerScores(_WinResultCache); }

            // Notify listeners.
            if      (NextActionPlayer == Player.Multiple)   { MultiWin?.Invoke(multiWinResults); }
            else if (NextAction == GameAction.Tsumo)        { Tsumo?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.Ron)          { Ron?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.Nothing)      { ExhaustiveDraw?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.AbortiveDraw) { AbortiveDraw?.Invoke(Current, NextAbortiveDrawType); }
            else if (NextAction == GameAction.Chombo)       { Chombo?.Invoke(NextActionPlayer); }
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
                AdvanceAction = AdvanceAction.GameEnd;
            }
            else
            {
                // Determine bonus and dealer.
                bool advanceBonus = (NextActionPlayer == Player.Multiple) ? GetNextAction(Dealer).IsAgari() :
                                    NextAction.IsAgari()                  ? (NextActionPlayer == Dealer) :
                                    (NextAction == GameAction.Chombo)     ? false :
                                                                            true;
                bool advanceDealer;
                if (NextAction == GameAction.Nothing)
                {
                    advanceDealer = !GetHand(Dealer).Tempai;
                    if (Settings.GetSetting<bool>(GameOption.SouthNotReady))
                    {
                        advanceDealer &= GetHand(Dealer.GetNext()).Tempai;
                    }
                }
                else if (NextAction == GameAction.Chombo)
                {
                    advanceDealer = false;
                }
                else
                {
                    if (NagashiWin && Settings.GetSetting<bool>(GameOption.NagashiBonusOnEastTempaiOnly))
                    {
                        advanceBonus = GetHand(Dealer).Tempai;
                    }
                    advanceDealer = !advanceBonus;
                }

                if (Settings.GetSetting<bool>(GameOption.EightWinRetire) && 
                    !advanceDealer &&
                    (Bonus >= 8) && 
                    (NextAction != GameAction.AbortiveDraw) &&
                    (NextAction != GameAction.Chombo))
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
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).Reset(true); }
                DiscardPlayerList.Clear();

                for (int i = 0; i < DoraIndicators.Length; ++i)        { DoraIndicatorsRaw[i] = null; }
                for (int i = 0; i < UraDoraIndicators.Length; ++i)     { UraDoraIndicatorsRaw[i] = null; }
                foreach (TileImpl tile in WallRaw)                     { tile.Type = TileType.None;
                                                                         tile.Ancillary = Player.None;
                                                                         tile.Ghost = false; }

                PlayerRecentOpenKan = Player.None;
                Current = Dealer;
                PreviousAction = GameAction.Nothing;
                NextAction = GameAction.Nothing;
                NextActionTile = TileType.None;
                NextActionPlayer = Player.None;
                NextActionSlot = 0;
                NextAction1 = GameAction.Nothing;
                NextAction2 = GameAction.Nothing;
                NextAction3 = GameAction.Nothing;
                NextAction4 = GameAction.Nothing;
                NextActionPlayerTarget = Player.None;
                PlayerDeadWallPick = false;

                TableCleared?.Invoke();

                // Start the next state.
                AdvanceAction = AdvanceAction.RandomizingBreak;
            }
        }

        public void ExecutePostBreak_KanChosenTile()
        {
            if (DoraCount == 5)
            {
                // This is the 5th kan. Immediately abort.
                NextAction = GameAction.AbortiveDraw;
                NextActionPlayer = Current;
                NextAbortiveDrawType = AbortiveDrawType.FiveKans;
                AdvanceAction = AdvanceAction.HandEnd;
            }
            else
            {
                CommonHelpers.Check(((NextAction == GameAction.PromotedKan) || (NextAction == GameAction.ClosedKan)), "Expected closed or promoted kan, found: " + NextAction);

                // Reset ippatsu flag after kan, unless RinshanIppatsu is active and it was a closed kan for the current player.
                foreach (Player p in PlayerHelpers.Players)
                {
                    GetHand(p).CouldIppatsu &= ((Current == p) && (NextAction == GameAction.ClosedKan) && Settings.GetSetting<bool>(GameOption.RinshanIppatsu));
                }

                // We will use this to see if we can ron on the chankan in the case of a promoted can or ron
                // with a kokushi in the case of a closed kan. Set PrevAction to the type of kan.
                // We'll use GatherDecisions again, but because of PrevAction we'll only look for the applicable rons.
                PreviousAction = NextAction;
                ChankanFlag = true;
                AdvanceAction = AdvanceAction.GatherDecisions;
            }
        }

        public void ExecutePostBreak_KanPerformDecision()
        {
            CommonHelpers.Check(((NextAction == GameAction.Nothing) || (NextAction == GameAction.Ron)), ("Expected nothing or ron, found: " + NextAction));

            if (NextAction == GameAction.Nothing)
            {
                // No chankan. Set the kanburi flag until the next discard is done. Clear chankan. PrevAction will stash the type of kan that was done.
                KanburiFlag = true;
                ChankanFlag = false;
                NextAction = GameAction.ReplacementTilePick;
                SkipAdvancePlayer = true;
                AdvanceAction = AdvanceAction.PrePickTile;
            }
            else
            {
                // HandEnd will process the ron.
                AdvanceAction = AdvanceAction.HandEnd;
            }
        }

        public void ExecutePostBreak_GameEnd()
        {
            // Generate a GameResults and submit it to GameComplete.
            GameComplete?.Invoke(new GameResultImpl(Settings,
                                                    FirstDealer,
                                                    Player1Hand.Score,
                                                    Player2Hand.Score,
                                                    Player3Hand.Score,
                                                    Player4Hand.Score,
                                                    (Settings.GetSetting<bool>(GameOption.WinnerGetsPool) ? ConsumePool() : 0),
                                                    Player1Hand.Yakitori,
                                                    Player2Hand.Yakitori,
                                                    Player3Hand.Yakitori,
                                                    Player4Hand.Yakitori,
                                                    Player1Hand.Chombo,
                                                    Player2Hand.Chombo,
                                                    Player3Hand.Chombo,
                                                    Player4Hand.Chombo));
            AdvanceAction = AdvanceAction.Done;
        }

        public void ExecuteRewindModeChange_DecideMove()
        {
            CommonHelpers.Check((RewindAction != GameAction.Nothing), "Expected _RewindAction to not be Nothing, but it is!");
            State = (RewindAction == GameAction.OpenKan) ||
                    (RewindAction == GameAction.Chii)    ||
                    (RewindAction == GameAction.Pon)            ? PlayState.PerformDecision :
                    (RewindAction == GameAction.PickedFromWall) ? PlayState.PickTile :
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
                RewindAction = (meld.State == MeldState.KanOpen) ? GameAction.OpenKan :
                               (meld.State == MeldState.Chii)    ? GameAction.Chii :
                                                                   GameAction.Pon; 
            }
            else
            {
                RewindAction = GetHand(Current).PeekLastDrawKanType();
            }
        }

        public void ExecuteRewindPostModeChange_GatherDecisions()
        {
            // Undo the most recent discard.
            HandImpl discardPlayerHand = GetHand(DiscardPlayerList.Pop());
            TileImpl undoneDiscard = discardPlayerHand.DiscardsRaw.Pop();

            // Add the tile back into the discarding player's hand. This will fire events.
            discardPlayerHand.RewindDiscardTile(undoneDiscard);
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

            Global.Log("Draw to Rewind: " + command.Tile.Type + " Remaining DrawsAndKans Count: " + hand.DrawsAndKans.Count);
            CommonHelpers.Check((command.Command == CommandType.Tile), ("RewindPMC_PickTile, expected tile, found: " + command.Command));
            CommonHelpers.Check(hand.RewindAddTile(command.Tile.Type), ("Failed to rewind add tile!! Tile: " + command.Tile.Type));

            // Increase the number of tiles remaining. Ensure that the tiles match.
            TilesRemaining++;
            int slot = GetNextWallDrawSlot();
            TileType wallTile = Wall[slot].Type;
            CommonHelpers.Check(wallTile.IsEqual(command.Tile.Type), ("Tile that was rewinded isn't the same tile from the wall! Rewind tile: " + command.Tile.Type + " wall tile: " + wallTile + " at slot: " + slot));

            // Notify the sink that this happened.
            WallPickUndone?.Invoke(Current, command.Tile);
        }

        internal void Reset()
        {
            (Settings as GameSettingsImpl).Locked = false;

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

            foreach (TileImpl tile in WallRaw)                    { tile.Reset(); }
            for (int i = 0; i < DoraIndicatorsRaw.Length; ++i)    { DoraIndicatorsRaw[i] = null; }
            for (int i = 0; i < UraDoraIndicatorsRaw.Length; ++i) { UraDoraIndicatorsRaw[i] = null; }

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
            PreviousAction = GameAction.Nothing;
            NextAction = GameAction.Nothing;
            PlayerRecentOpenKan = Player.None;
            NextActionPlayer = Player.None;
            NextActionTile = TileType.None;
            PlayerDeadWallPick = false;
            FlipDoraAfterNextDiscard = false;
            ChankanFlag = false;
            KanburiFlag = false;
            NextActionPlayerTarget = Player.None;
            NextAction1 = GameAction.Nothing;
            NextAction2 = GameAction.Nothing;
            NextAction3 = GameAction.Nothing;
            NextAction4 = GameAction.Nothing;
            RewindAction = GameAction.Nothing;
            AdvanceAction = AdvanceAction.Done;
            SkipAdvancePlayer = false;
            HasExtraSettings = false;
            NextActionSlot = -1;
            ExpectingPause = false;
            ShouldPause = false;
            CanStart = true;
            CanAdvance = false;
            CanResume = false;
            ExpectingDiscard = false;
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

        internal void FixPostStateLoad()
        {
            PopulateDoraIndicators();
        }

        internal void SanityCheck()
        {
            // TODO: this!!!
        }

        internal HandImpl GetHand(Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get hand for non-player: " + p);
            return (p == Player.Player1) ? Player1HandRaw :
                   (p == Player.Player2) ? Player2HandRaw :
                   (p == Player.Player3) ? Player3HandRaw :
                                           Player4HandRaw;
        }

        internal void ReplaceTile(TileType tileRemove, TileType tileAdd, Player target)
        {
            // Replace an instance of tileRemove with tileAdd. The target player has tried to manually add tileRemove to it's hand,
            // so we need to put the tile it's giving up back into the board somewhere. Look through the wall first (excluding flipped
            // dora tiles), then other players' hands, then discards, and then the dora tile. Throw if we cannot find tileRemove anywhere.
            // TODO: this
        }

        internal void IterateDiscards(Func<Player, TileImpl, bool> callback)
        {
            // TODO: Turn this into IterateDiscardsAndCalls. Use TileCommands...
            int[] discards = new int[] { 0, 0, 0, 0 };
            int[] remainingDiscards = new int[] { Player1Hand.Discards.Count, Player2Hand.Discards.Count,
                                                  Player3Hand.Discards.Count, Player4Hand.Discards.Count };

            Player checkPlayer = Dealer;
            while ((remainingDiscards[0] > 0) || (remainingDiscards[1] > 0) || (remainingDiscards[2] > 0) || (remainingDiscards[3] > 0))
            {
                int nextDiscardPlayer = checkPlayer.GetZeroIndex();
                TileImpl nextDiscard = GetHand(checkPlayer).DiscardsRaw[discards[nextDiscardPlayer]];
                if (!callback(checkPlayer, nextDiscard))
                {
                    break;
                }

                discards[nextDiscardPlayer]++;
                remainingDiscards[nextDiscardPlayer]--;
                checkPlayer = nextDiscard.Called ? nextDiscard.Ancillary : checkPlayer.GetNext();
            }
        }

        private void Initialize(IGameSettings settings, IExtraSettings extra)
        {
            InitializeCommon(settings, extra);

            FirstDealer = Player.Player1; // FOR TESTING // PlayerHelpers.GetRandom();
            Dealer = FirstDealer;
            Current = FirstDealer;
        }

        private void InitializeFromState(ISaveState state, IExtraSettings extra)
        {
            InitializeCommon(state.Settings, extra);
            CommonHelpers.Check((state is SaveStateImpl), "SaveState not from MahjongCore, external save states not supported at this time.");
            (state as SaveStateImpl).PopulateState(this);

            CanStart = false;
            CanResume = true;
        }

        private void InitializeCommon(IGameSettings settings, IExtraSettings extra, bool skipHandlers = false)
        {
            for (int i = 0; i < WallRaw.Length; ++i)
            {
                WallRaw[i] = new TileImpl { Slot = i, Location = Location.Wall };
            }

            // Set settings and determine if we're in tutorial mode if ts is null or not.
            Settings         = settings ?? new GameSettingsImpl();
            ExtraSettings    = extra ?? new ExtraSettingsImpl();
            HasExtraSettings = (extra != null);

            int score = Settings.GetSetting<int>(GameOption.StartingPoints);
            Player1HandRaw = new HandImpl(this, Player.Player1, score);
            Player2HandRaw = new HandImpl(this, Player.Player2, score);
            Player3HandRaw = new HandImpl(this, Player.Player3, score);
            Player4HandRaw = new HandImpl(this, Player.Player4, score);

            if (!skipHandlers)
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
            RewindAction = GameAction.Nothing;
            CanAdvance = false;
            bool queuedSkipCheck = skipCheck;
            bool queuedAdvancePlayer = advancePlayer;
            PlayState queuedMode = nextMode;

            do
            {
                CommonHelpers.Check(queuedMode != PlayState.NA, "Next play mode to advance is NA! Invalid state!");
                AdvanceAction = AdvanceAction.Advance;

                if (queuedSkipCheck)
                {
                    queuedSkipCheck = false;
                }
                else
                {
                    // Advance player, set flags/game mode/etc.
                    State = queuedMode;

                    if (SkipAdvancePlayer)
                    {
                        SkipAdvancePlayer = false;
                    }
                    else if (queuedAdvancePlayer)
                    {
                        Current = Current.GetNext();
                    }

                    // Execute a pre-break handler which will make the new state valid if we end up pausing here.
                    // Skip this if we previously paused because we already did it.
                    if (_PreBreakStateHandlers.ContainsKey(queuedMode))
                    {
                        _PreBreakStateHandlers[queuedMode].Invoke();
                    }

                    if (CheckForPause(false))
                    {
                        AdvanceAction = AdvanceAction.Done;
                        break;
                    }
                }

                // Execute the bulk of the game mode.
                if (_PostBreakStateHandlers.ContainsKey(queuedMode))
                {
                    _PostBreakStateHandlers[queuedMode].Invoke();
                }

                // Determine what the next step will be.
                DetermineAdvanceState(AdvanceAction, out queuedMode, out queuedAdvancePlayer);
            } while (AdvanceAction != AdvanceAction.Done);
            CanAdvance = true;
        }

        private void DetermineAdvanceState(AdvanceAction action, out PlayState state, out bool advancePlayer)
        {
            advancePlayer = false;

            switch (action)
            {
                case AdvanceAction.Done:               state = PlayState.NA;
                                                       break;

                case AdvanceAction.Advance:            state = State.GetNext();
                                                       advancePlayer = EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(state);
                                                       break;

                case AdvanceAction.GatherDecisions:    state = PlayState.GatherDecisions;
                                                       break;

                case AdvanceAction.PrePickTile:        state = PlayState.PrePickTile;
                                                       advancePlayer = true;
                                                       break;

                case AdvanceAction.RandomizingBreak:   state = PlayState.RandomizingBreak;
                                                       // TODO: more?
                                                       break;

                case AdvanceAction.GameEnd:            state = PlayState.GameEnd;
                                                       // TODO: more?
                                                       break;

                case AdvanceAction.DecidePostCallMove: state = PlayState.DecideMove;
                                                       break;

                case AdvanceAction.KanChosenTile:      state = PlayState.KanChosenTile;
                                                       break;

                default:                               Global.LogExtra("Unexpected advance action? Action: " + action + " current play state: " + State);
                                                       throw new Exception("Unexpected AdvanceAction");
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
                        if ((PreviousAction == GameAction.PromotedKan) || (PreviousAction == GameAction.ClosedKan))
                        {
                            PostKanRequested?.Invoke(_PostDiscardInfoCache);
                        }
                        else
                        {
                            PostDiscardRequested?.Invoke(_PostDiscardInfoCache);
                        }
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
            AdvanceAction nextAction = (NextAction1 != GameAction.DecisionPending) &&
                                       (NextAction2 != GameAction.DecisionPending) &&
                                       (NextAction3 != GameAction.DecisionPending) &&
                                       (NextAction4 != GameAction.DecisionPending) ? AdvanceAction.Advance : AdvanceAction.Done;

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
                    (((NextAction1.IsAgari() ? 1 : 0) + (NextAction2.IsAgari() ? 1 : 0) + (NextAction3.IsAgari() ? 1 : 0) + (NextAction4.IsAgari() ? 1 : 0)) > 1))
                {
                    NextActionPlayer = Player.Multiple;
                }

                // Clear out stored call options on anyone that didn't win the decision.
                foreach (Player p in PlayerHelpers.Players)
                {
                    if ((NextActionPlayer != p) && (GetHand(p).CachedCall != null))
                    {
                        DecisionCancelled?.Invoke(p, GetHand(p).CachedCall);
                        GetHand(p).CachedCall = null;
                    }
                }
            }
            return nextAction;
        }

        private AdvanceAction ProcessDiscardDecision(IDiscardDecision decision)
        {
            ExpectingDiscard = false;
            HandImpl hand = GetHand(Current);
            CommonHelpers.Check((!(decision is DiscardDecisionImpl) || ((DiscardDecisionImpl)decision).Validate(hand)), "Post discard decision failed validation.");

            AdvanceAction action = AdvanceAction.Advance;
            switch (decision.Decision)
            {
                case DiscardDecisionType.Discard:           PerformDiscardState(decision.Tile, GameAction.Discard);
                                                            hand.PerformDiscard(decision.Tile, ReachType.None);
                                                            action = AdvanceAction.GatherDecisions;
                                                            break;

                case DiscardDecisionType.OpenRiichiDiscard:
                case DiscardDecisionType.RiichiDiscard:     bool openReach = (decision.Decision == DiscardDecisionType.OpenRiichiDiscard);
                                                            hand.PerformDiscard(decision.Tile, (openReach ? ReachType.OpenReach : ReachType.Reach));
                                                            PerformDiscardState(decision.Tile, (openReach ? GameAction.OpenRiichiDiscard : GameAction.RiichiDiscard));
                                                            hand.Score -= 1000;
                                                            hand.CouldIppatsu = true;
                                                            break;

                case DiscardDecisionType.Tsumo:             NextAction = GameAction.Tsumo;
                                                            NextActionPlayer = Current;
                                                            action = AdvanceAction.HandEnd;
                                                            break;

                case DiscardDecisionType.ClosedKan:         NextActionTile = decision.Tile.Type;
                                                            NextAction = GameAction.ClosedKan;
                                                            NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            hand.PerformClosedKan(decision.Tile.Type);
                                                            break;

                case DiscardDecisionType.PromotedKan:       NextActionTile = decision.Tile.Type;
                                                            NextAction = GameAction.PromotedKan;
                                                            NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            hand.PerformPromotedKan(decision.Tile);
                                                            break;

                case DiscardDecisionType.AbortiveDraw:      NextActionPlayer = Current;
                                                            NextAction = GameAction.AbortiveDraw;
                                                            NextAbortiveDrawType = (decision.Tile == null) ? AbortiveDrawType.KyuushuuKyuuhai : AbortiveDrawType.Suufuurendan;
                                                            if (decision.Tile != null)
                                                            {
                                                                PerformDiscardState(decision.Tile, GameAction.AbortiveDraw);
                                                                hand.PerformDiscard(decision.Tile, ReachType.None);
                                                            }
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

        private bool CheckForPause(bool rewindHandler)
        {
            Exception stashedException = null;
            bool pause = false;
            try
            {
                ExpectingPause = true;
                ShouldPause = false;
                if (rewindHandler)
                {
                    PreCheckRewind?.Invoke();
                }
                else
                {
                    PreCheckAdvance?.Invoke();
                }
                pause = ShouldPause;
            }
            catch (Exception e)
            {
                stashedException = e;
            }
            finally
            {
                ExpectingPause = false;
                ShouldPause = false;
                if (stashedException != null)
                {
                    throw stashedException;
                }
            }
            return pause;
        }

        private void FlipDora()
        {
            DoraIndicatorFlipped?.Invoke(DoraIndicators[DoraCount]);
            DoraCount = DoraCount + 1;
        }

        private int ConsumePool()
        {
            int poolValue = Pool;
            Pool = 0;
            return poolValue;
        }

        private void PerformRoundEndRewindStep()
        {
            CommonHelpers.Check(NextAction.IsAgari(), ("Expected ron or tsumo, found: " + NextAction));

            // If we're done with this round, rewind us before the previous NextAction == Ron or NextAction == Tsumo.
            State = (NextAction == GameAction.Ron) ? PlayState.GatherDecisions : PlayState.DecideMove;
            WinUndone?.Invoke(Current);
        }

        private void PickIntoPlayerHand(Player p, int count)
        {
            CommonHelpers.Check((count == 4 || count == 1), ("Expected 1 or 4 tiles, found: " + count));

            bool flipDora = false;
            ITile[] tiles = new ITile[count];
            ITile[] wallTiles = new ITile[count];
            TileSource source = TileSource.Wall;

            if (count == 4)
            {
                for (int i = 0; i < 4; ++i)
                {
                    tiles[i] = PickIntoPlayerHand(p, TileSource.Wall, out wallTiles[i]);
                }
            }
            else
            {
                // If we did a closed kan, or if we always flip doras immediately, flip a new dora now. Otherwise do so after the next discard.
                if (NextAction == GameAction.ReplacementTilePick)
                {
                    source = TileSource.DeadWall;
                    if (Settings.GetSetting<bool>(GameOption.KanDora))
                    {
                        if ((PreviousAction == GameAction.ClosedKan) || (Settings.GetSetting<bool>(GameOption.FlipDoraTilesImmediately)))
                        {
                            flipDora = true;
                        }
                        else
                        {
                            FlipDoraAfterNextDiscard = true;
                        }
                    }
                }

                // Update PrevAction and determine our source.
                PreviousAction = (source == TileSource.DeadWall) ? GameAction.ReplacementTilePick : GameAction.PickedFromWall;
                tiles[0] = PickIntoPlayerHand(p, source, out wallTiles[0]);
            }

            WallPicked?.Invoke(wallTiles, source);
            GetHand(p).AddTileCompleted(tiles);
            if (flipDora) { FlipDora(); }
        }

        private ITile PickIntoPlayerHand(Player p, TileSource source, out ITile wallTile)
        {
            // Get the tile from the wall to pick from.
            CommonHelpers.Check((source != TileSource.Call), "Should not be picking from a call. Doesn't make sense.");
            int tileNumber;
            if (source == TileSource.DeadWall)
            {
                PlayerDeadWallPick = true;
                tileNumber = Offset - ((DoraCount == 1) ? 2 :
                                       (DoraCount == 2) ? 1 :
                                       (DoraCount == 3) ? 4 : 3);
            }
            else
            {
                tileNumber = GetNextWallDrawSlot();
            }

            TileImpl wallTileRaw = WallRaw[tileNumber];
            ITile handTile = GetHand(p).AddTile(wallTileRaw);
            Global.Log("PickIntoPlayerHand! Player: " + p + " Slot: " + tileNumber + " Tile: " + wallTileRaw.Type);

            wallTileRaw.Ghost = true;
            wallTileRaw.Ancillary = p;
            TilesRemaining--;

            wallTile = wallTileRaw;
            return handTile;
        }

        private GameAction GetNextAction(Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get action for non-player: " + p);
            return (p == Player.Player1) ? NextAction1 :
                   (p == Player.Player2) ? NextAction2 :
                   (p == Player.Player3) ? NextAction3 :
                                           NextAction4;
        }

        private void SetNextAction(Player p, GameAction action)
        {
            CommonHelpers.Check(p.IsPlayer(), "Tried to get action for non-player: " + p);
            if      (p == Player.Player1) { NextAction1 = action; }
            else if (p == Player.Player2) { NextAction2 = action; }
            else if (p == Player.Player3) { NextAction3 = action; }
            else                          { NextAction4 = action; }
        }

        private void PerformDiscardState(ITile tile, GameAction nextAction)
        {
            NextAction = nextAction;
            NextActionTile = tile.Type;
            NextActionSlot = tile.Slot;
            PlayerRecentOpenKan = Player.None;

            DiscardPlayerList.Push(Current);
            GetHand(Current).CouldIppatsu = false;
        }

        private void ProcessTsumoCommand(Player winner, int han, int fu)
        {
            NextAction = GameAction.Tsumo;
            NextActionPlayer = winner;
            NextActionPlayerTarget = Player.Multiple;
            PlayerRecentOpenKan = Player.None; // So no Sekinin Barai gets in the way.

            HandImpl winningHand = GetHand(winner);
            var candidateHand = new CandidateHand { Han = han, Fu = fu };
            winningHand.WinningHandCache = candidateHand;
        }

        private void ProcessRonCommand(Player winner, Player target, int han, int fu)
        {
            NextAction = GameAction.Ron;
            NextActionPlayer = winner;
            NextActionPlayerTarget = target;
            PlayerRecentOpenKan = Player.None; // So no Sekinin Barai gets in the way.

            HandImpl winningHand = GetHand(winner);
            var candidateHand = new CandidateHand { Han = han, Fu = fu };
            winningHand.WinningHandCache = candidateHand;
        }

        private void ProcessExhaustiveDrawCommand(bool player1Tempai, bool player2Tempai, bool player3Tempai, bool player4Tempai)
        {
            NextAction = GameAction.Nothing;
            Player1HandRaw.Tempai = player1Tempai;
            Player2HandRaw.Tempai = player2Tempai;
            Player3HandRaw.Tempai = player3Tempai;
            Player4HandRaw.Tempai = player4Tempai;
        }

        private void ProcessAbortiveDrawCommand()
        {
            NextAction = GameAction.AbortiveDraw;
            NextAbortiveDrawType = AbortiveDrawType.Other;
        }

        private void ProcessChomboCommand(Player chombo)
        {
            NextAction = GameAction.Chombo;
            NextActionPlayer = chombo;
        }

        private void ProcessMultiWinCommand(IResultCommand[] wins)
        {
            // TODO: this
        }

        private class AssignedTile
        {
            public TileType Tile;
            public bool     Assigned;

            public AssignedTile(TileType type)
            {
                Tile = type;
                Assigned = false;
            }
        }

        private void ProcessBoardOverride(IBoardTemplate template)
        {
            if (template is BoardTemplateImpl templateImpl) { templateImpl.VerifyOrThrow(this); }

            // Build a wall in a very contrived way. All tiles that a player needs in order to fulfull its called melds are in the player's haipai.
            // For concealed kans, the first three tiles are in the player's haipai. The fourth is the tile drawn on that player's turn.
            // Be careful to grab that from the dead wall in the case of kans. Closed kans happen as soon as possible.
            // TODO: Make this more random and less contrived. Would need one heck of an algorithm.

            // Initialize the wall with all the tiles that have been set. Reset the ones that have not yet been set.
            foreach (TileImpl tile in WallRaw)                              { tile.Type = TileType.None; }
            CommonHelpers.Iterate(template.Wall, (ITile wallTile, int i) => { WallRaw[wallTile.Slot].Type = wallTile.Type; });

            // Create a second GameState that we will use to iterate through the game. This can have whatever tiles, the only
            // important thing is that it tells us who goes, who discards, who draws, and what tile in the wall/etc they do so from.
            var sampleGame = new GameStateImpl(Settings);
            GameStateHelpers.InitializeToDiceRolled(sampleGame);
            sampleGame.SubmitOverride(OverrideState.Round, Round);
            sampleGame.SubmitOverride(OverrideState.Dealer, Dealer);
            sampleGame.SubmitOverride(OverrideState.Current, Dealer);
            sampleGame.Roll = Roll;
            sampleGame.Offset = GameStateHelpers.GetOffset(Dealer, Roll);
            bool pauseOnNextState = false;
            sampleGame.PreCheckAdvance += () => { if (pauseOnNextState) { pauseOnNextState = false; sampleGame.Pause(); } };

            // For each tile that is drawn, check if the tile in the wall has a value already.
            // If so:  add the tile to the player's set of "Allocated tiles": tiles the player 'has' that may or may not have been 'assigned' to a call or discard yet.
            // If not: add the slot to the player's set of "Available Wall Slots". Later, if we need to ensure that a player has a tile
            //         to fulfill a discard, we can Allocate one of the wall slots to have been that tile.
            var playerAllocatedTiles = new Dictionary<Player, List<AssignedTile>>();
            var playerAvailableWallSlots = new Dictionary<Player, List<int>>();
            foreach (Player p in PlayerHelpers.Players)
            {
                playerAllocatedTiles[p] = new List<AssignedTile>();
                playerAvailableWallSlots[p] = new List<int>();
            }

            sampleGame.WallPicked += (ITile[] tiles, TileSource source) =>
            {
                foreach (ITile tile in tiles)
                {
                    if (WallRaw[tile.Slot].Type.IsTile()) { playerAllocatedTiles[sampleGame.Current].Add(new AssignedTile(WallRaw[tile.Slot].Type)); }
                    else                                  { playerAvailableWallSlots[sampleGame.Current].Add(tile.Slot); }
                    ProcessBoardOverride_Log(("Wall Picked, tile #" + tile.Slot), this, playerAllocatedTiles, playerAvailableWallSlots);
                }
            };

            // If there are no discards, advance to randomizing break.
            if (template.DiscardCount == 0)
            {
                GameStateHelpers.AdvanceToDeadWallMoved(sampleGame);
            }

            // Process all the requested discards.
            bool forceAnotherDiscard = false;
            for (int remainingDiscards = template.DiscardCount; 
                 (((remainingDiscards > 0) || forceAnotherDiscard) && // Loop if there are more discards to do or if we must do another discard because of a call.
                  (sampleGame.TilesRemaining >= 0) &&                 // Don't loop if there are no more tiles in the wall...
                  (sampleGame.State != PlayState.HandEnd) &&          // Don't loop if the hand is over. 
                  (sampleGame.State != PlayState.GameEnd));)          // Don't loop if the game is over.
            {
                // Consume the flag.
                forceAnotherDiscard = false;

                // At the start of each loop, we are in a state where we're awaiting drawing a tile. It might be a previous players turn, we might still be on randomizing break, etc.
                // This is because we want to end our processing in a position where noone has their 14th tile. So the first thing to do is advance to our next discard decision.
                ProcessBoardOverride_AdvanceToDiscardDecision(sampleGame);

                // Check if we have closed kans in the queue. If so, perform them now.
                Player currentPlayer = sampleGame.Current;
                IHand hand = GameStateHelpers.GetHand(sampleGame, currentPlayer);

                while ((BoardTemplateHelpers.GetNextPlayerMeld(template, sampleGame, currentPlayer) is IMeld closedKanMeld) &&
                       (closedKanMeld.State == MeldState.KanConcealed))
                {
                    // Perform a closed kan. If we have unassigned allocated tiles, use them. Otherwise use available wall slots. We need to gather all four tiles at this time.
                    ProcessBoardOverride_AssignAndEnsureHandHasTiles(
                        sampleGame,
                        hand,
                        playerAvailableWallSlots[closedKanMeld.Owner],
                        playerAllocatedTiles[closedKanMeld.Owner],
                        new TileType[] {
                            closedKanMeld.Tiles[0].Type, closedKanMeld.Tiles[1].Type,
                            closedKanMeld.Tiles[2].Type, closedKanMeld.Tiles[3].Type });

                    // Setup the eventing and submit the meld. We want to advance through to the next discard decision.
                    // Set this up so that we can make another closed kan if such is requested.
                    pauseOnNextState = true;
                    bool callObserved = false;
                    void calledVerifier(Player p, IMeld m)       { callObserved = true; }
                    void breakOnPerformDecision()                { if (sampleGame.State == PlayState.PerformDecision) { sampleGame.Pause(); } }
                    void passOnChankanRon(IPostDiscardInfo info) { sampleGame.SubmitPostDiscard(DecisionFactory.BuildPostDiscardDecision(info.Hand.Player, PostDiscardDecisionType.Nothing, null)); }

                    hand.Called += calledVerifier;
                    sampleGame.PreCheckAdvance += breakOnPerformDecision;
                    sampleGame.PostDiscardRequested += passOnChankanRon;
                    sampleGame.SubmitDiscard(
                        DecisionFactory.BuildDiscardDecision(
                            DiscardDecisionType.ClosedKan,
                            hand.Tiles[hand.GetTileSlot(closedKanMeld.Tiles[0].Type, true)]));
                    hand.Called -= calledVerifier;
                    sampleGame.PreCheckAdvance -= breakOnPerformDecision;
                    sampleGame.PostDiscardRequested -= passOnChankanRon;

                    CommonHelpers.Check(callObserved, "Didn't observe a closed kan even though we submitted a closed kan!");
                }

                // Check if we need to make a specific discard. If so, allocate the tile, but also check if that tile is going to
                // be called to make a meld. If so, we need to make sure the caller has all the tiles to make the meld so that when
                // we submit the discard, the call lights up as an option.
                ITile specifiedDiscard = BoardTemplateHelpers.GetNextPlayerDiscard(template, sampleGame, currentPlayer);
                IMeld nextCall = BoardTemplateHelpers.GetMeld(template, specifiedDiscard, currentPlayer);

                if (specifiedDiscard != null)
                {
                    TileType specifiedDiscardToAssign = specifiedDiscard.Type;
                    Global.LogExtra("About to assign tile for a specified discard: " + specifiedDiscardToAssign + " for player: " + sampleGame.Current);
                    ProcessBoardOverride_AssignAndEnsureHandHasTiles(
                        sampleGame,
                        hand,
                        playerAvailableWallSlots[sampleGame.Current],
                        playerAllocatedTiles[sampleGame.Current],
                        new TileType[] { specifiedDiscardToAssign });
                    ProcessBoardOverride_Log(("Assigned tile for discard: " + specifiedDiscardToAssign), this, playerAllocatedTiles, playerAvailableWallSlots);

                    if (nextCall != null)
                    {
                        var handMeldTiles = new List<TileType>();
                        MeldHelpers.IterateTiles(nextCall, (ITile tile) => { if (!tile.Called) { handMeldTiles.Add(tile.Type); } });

                        Player caller = nextCall.Owner;
                        ProcessBoardOverride_AssignAndEnsureHandHasTiles(
                            sampleGame,
                            GameStateHelpers.GetHand(sampleGame, caller),
                            playerAvailableWallSlots[caller],
                            playerAllocatedTiles[caller],
                            handMeldTiles.ToArray());
                    }
                }

                // Now discard the tile we need to discard if its specific. Otherwise just discard anything, it doesn't really matter
                // since we're not using sampleGame directly, just as a sample to measure discard counts and perform online sanity checking.
                // If post discard decisions get requested, cancel in all scenarios unless it's a call we are supposed to be making.
                TileType tileToDiscard = (specifiedDiscard != null) ? specifiedDiscard.Type : hand.Tiles[hand.TileCount-1].Type;

                void wallPickingPause(Player p, int i) { pauseOnNextState = true; }
                void postRequested(IPostDiscardInfo info)
                {
                    IMeld meldToCall = null;
                    CommonHelpers.Iterate(info.Calls, (IMeld meld, int i) =>
                    {
                        if ((nextCall != null) && (meld.CompareTo(nextCall) == 0))
                        {
                            meldToCall = meld;
                            forceAnotherDiscard = true;
                        }
                    });

                    sampleGame.SubmitPostDiscard(DecisionFactory.BuildPostDiscardDecision(
                        info.Hand.Player,
                        (meldToCall != null ? PostDiscardDecisionType.Call : PostDiscardDecisionType.Nothing),
                        meldToCall));
                }

                sampleGame.WallPicking += wallPickingPause;
                sampleGame.PostDiscardRequested += postRequested;
                sampleGame.SubmitDiscard(DecisionFactory.BuildDiscardDecision(
                    (specifiedDiscard?.Reach.IsReach() ?? false) ? DiscardDecisionType.RiichiDiscard : DiscardDecisionType.Discard,
                    hand.Tiles[hand.GetTileSlot(tileToDiscard, true)]));
                --remainingDiscards;
                sampleGame.WallPicking -= wallPickingPause;
                sampleGame.PostDiscardRequested -= postRequested;
            }

            // Now it's time to fill out this GameState. Start by getting a list of randomly organized tiles that remain.
            // Remove anything that is already assigned in the wall either by the previous discard/call process or via manual assignment.
            // Also remove anything specified in the hands at this stage, which will will assign into the wall later.
            var randomBoard = new List<ITile>(TileHelpers.GetRandomBoard(null, Settings.GetSetting<RedDora>(GameOption.RedDoraOption)));
            foreach (TileImpl wallTile in WallRaw)
            {
                if (wallTile.Type.IsTile()) { RemoveFromRandomTileCollection(randomBoard, wallTile.Type); }
            }

            foreach (Player player in PlayerHelpers.Players)
            {
                CommonHelpers.Iterate(template.GetHand(player), (ITile tile) =>
                {
                    Global.Assert(tile.Type.IsTile());
                    RemoveFromRandomTileCollection(randomBoard, tile.Type);
                });
            }

            // Next, populate discards, because we know what number of discards each player has and what they need to be. Assign each unspecified tile, or just use the ones from template.
            foreach (Player player in PlayerHelpers.Players)
            {
                IHand sampleHand = GameStateHelpers.GetHand(sampleGame, player);
                var actualDiscards = new List<ITile>();
                for (int discardSlot = 0; discardSlot < sampleHand.Discards.Count; ++discardSlot)
                {
                    ITile specifiedTile = BoardTemplateHelpers.GetPlayerDiscardBySlot(template, player, discardSlot);
                    if (specifiedTile != null)
                    {
                        IMeld calledMeld = BoardTemplateHelpers.GetMeld(template, specifiedTile, player);
                        actualDiscards.Add(TileFactory.BuildTile(specifiedTile.Type, discardSlot, specifiedTile.Called, (calledMeld?.Owner ?? Player.None)));
                    }
                    else
                    {
                        TileType randomTile = randomBoard.Pop().Type;
                        ProcessBoardOverride_AssignTileOrThrow(
                            playerAvailableWallSlots[player],
                            playerAllocatedTiles[player],
                            randomTile);
                        ProcessBoardOverride_Log(("Assigned a random tile for a discard at slot: " + discardSlot + " should be tile: " + randomTile), this, playerAllocatedTiles, playerAvailableWallSlots);

                        actualDiscards.Add(TileFactory.BuildTile(randomTile, discardSlot, false));
                    }
                }

                IHand actualHand = GameStateHelpers.GetHand(this, player);
                actualHand.SubmitOverride(OverrideHand.Discards, actualDiscards.ToArray());
                ProcessBoardOverride_Log(("Submitted discards for player: " + player), this, playerAllocatedTiles, playerAvailableWallSlots);
            }

            // Next, Populate all the melds.
            foreach (Player player in PlayerHelpers.Players)
            {
                GameStateHelpers.GetHand(this, player).SubmitOverride(
                    OverrideHand.Melds,
                    GameStateHelpers.GetHand(sampleGame, player).Melds);
            }

            // Next, try to assign tiles defined for the player's hand.
            foreach (Player player in PlayerHelpers.Players)
            {
                IHand actualHand = GameStateHelpers.GetHand(this, player);
                int handCount = 13 - (actualHand.MeldCount * 3);

                var handTiles = new List<TileType>();
                CommonHelpers.Iterate(template.GetHand(player), (ITile tile) =>
                {
                    Global.LogExtra("Need to add specified tile to hand for player " + player + ", tile: " + tile.Type);
                    ProcessBoardOverride_AssignTileOrThrow(
                        playerAvailableWallSlots[player],
                        playerAllocatedTiles[player],
                        tile.Type);
                    handTiles.Add(tile.Type);

                    ProcessBoardOverride_Log(("Assigned a specified tile " + tile.Type + " for player " + player + ", handTiles so far looks like: " + TileHelpers.GetTileString(handTiles)), this, playerAllocatedTiles, playerAvailableWallSlots);
                });

                while ((handTiles.Count < handCount) && ProcessBoardOverride_PeekAvailableAssignedTile(playerAllocatedTiles[player], out TileType availableAssignedTile))
                {
                    Global.LogExtra("Adding assigned but unapplied tile: " + availableAssignedTile + " for player: " + player);
                    ProcessBoardOverride_AssignTileOrThrow(
                        playerAvailableWallSlots[player],
                        playerAllocatedTiles[player],
                        availableAssignedTile);
                    handTiles.Add(availableAssignedTile);

                    ProcessBoardOverride_Log(("Added an assigned and then unapplied tile " + availableAssignedTile + " for player " + player + ", handTiles so far looks like: " + TileHelpers.GetTileString(handTiles)), this, playerAllocatedTiles, playerAvailableWallSlots);
                }

                while (handTiles.Count < handCount)
                {
                    TileType randomTile = randomBoard.Pop().Type;
                    Global.LogExtra("Adding random tile to hand for player " + player + ", tile: " + randomTile);
                    ProcessBoardOverride_AssignTileOrThrow(
                        playerAvailableWallSlots[player],
                        playerAllocatedTiles[player],
                        randomTile);
                    handTiles.Add(randomTile);

                    ProcessBoardOverride_Log(("Assigned a random tile " + randomTile + " for player " + player + ", handTiles so far looks like: " + TileHelpers.GetTileString(handTiles)), this, playerAllocatedTiles, playerAvailableWallSlots);
                }

                handTiles.Sort();
                actualHand.SubmitOverride(OverrideHand.Hand, handTiles.ToArray());
                ProcessBoardOverride_Log(("Hand overridden for player: " + player), this, playerAllocatedTiles, playerAvailableWallSlots);
            }

            // Finally, elaborate the rest of the wall with whatever tiles remain.
            foreach (TileImpl wallTile in WallRaw)
            {
                if (!wallTile.Type.IsTile())
                {
                    var randomTile = randomBoard.Pop();
                    Global.LogExtra("Popping a random tile: " + randomTile + " for slot# " + wallTile.Slot + " with remaining randomboard: " + TileHelpers.GetTileString(randomBoard));
                    wallTile.Type = randomTile.Type;
                }
            }
            CommonHelpers.Check((randomBoard.Count == 0), "Should have exactly consumed all remaining random tiles.");

            // Set the remaining fields from sampleGame
            TilesRemaining = sampleGame.TilesRemaining;
            Current = sampleGame.Current;
            DoraCount = sampleGame.DoraCount;
            Offset = sampleGame.Offset;
            State = sampleGame.State;
        }

        private bool ProcessBoardOverride_PeekAvailableAssignedTile(List<AssignedTile> allocatedTiles, out TileType availableAssigned)
        {
            availableAssigned = TileType.None;
            foreach (AssignedTile aTile in allocatedTiles)
            {
                if (!aTile.Assigned)
                {
                    availableAssigned = aTile.Tile;
                    return true;
                }
            }
            return false;
        }

        private void RemoveFromRandomTileCollection(List<ITile> randomBoard, TileType tile)
        {
            for (int i = 0; i < randomBoard.Count; ++i)
            {
                if (randomBoard[i].Type.IsEqual(tile, true))
                {
                    randomBoard.RemoveAt(i);
                    return;
                }
            }
        }

        private void ProcessBoardOverride_Log(
            string message,
            IGameState state,
            Dictionary<Player, List<AssignedTile>> assignedTiles,
            Dictionary<Player, List<int>> availbleWallSlots)
        {
            if (Global.CanLogExtra)
            {
                var sb = new StringBuilder();
                sb.AppendLine("* " + message);

                // Output the wall.
                {
                    sb.Append("| Wall: ");
                    var prevTile = TileType.None;
                    CommonHelpers.Iterate(state.Wall, (ITile tile) =>
                    {
                        if (tile.Type.IsTile()) { tile.Type.GetSummary(sb, prevTile); }
                        else                    { sb.Append("."); }
                        prevTile = tile.Type;
                    });
                    sb.AppendLine();
                }

                // Output Player Stuff.
                foreach (Player p in PlayerHelpers.Players)
                {

                    sb.Append("| " + p.ToString() + " hand: ");
                    {
                        var prevTile = TileType.None;
                        HandHelpers.IterateTiles(GameStateHelpers.GetHand(state, p), (TileType tile) =>
                        {
                            tile.GetSummary(sb, prevTile);
                            prevTile = tile;
                        });
                    }
                    sb.AppendLine();

                    sb.Append("| " + p.ToString() + " discards: ");
                    {
                        var prevTile = TileType.None;
                        CommonHelpers.Iterate(GameStateHelpers.GetHand(state, p).Discards, (ITile tile, int i) =>
                        {
                            tile.Type.GetSummary(sb, prevTile);
                            prevTile = tile.Type;
                        });
                    }
                    sb.AppendLine();

                    sb.Append("| " + p.ToString() + " wall slots: ");
                    CommonHelpers.Iterate(availbleWallSlots[p], (int wallSlot) => { sb.Append(wallSlot + " "); });
                    sb.AppendLine();

                    sb.Append((p == Player.Player4) ? "\\" : "|");
                    sb.Append(" " + p.ToString() + " assigned tiles: ");
                    CommonHelpers.Iterate(assignedTiles[p], (AssignedTile aTile) => 
                    {
                        aTile.Tile.GetSummary(sb, null);
                        sb.Append(aTile.Assigned);
                        sb.Append(" ");
                    });
                    sb.AppendLine();
                }

                // Print out the log data.
                Global.LogExtra(sb.ToString());
            }
        }

        private void ProcessBoardOverride_AdvanceToDiscardDecision(IGameState sampleGame)
        {
            bool discardRequested = false;
            void discardAction(IDiscardInfo info) { discardRequested = true; }

            sampleGame.DiscardRequested += discardAction;
            sampleGame.Resume();
            CommonHelpers.Check(discardRequested, "Resume should have resulted in us breaking after a discard was requested...");
            CommonHelpers.Check((sampleGame.State == PlayState.DecideMove), "When we stopped we should have stopped on the PerformDecision state...");
            sampleGame.DiscardRequested += discardAction;
        }

        private void ProcessBoardOverride_AssignTileOrThrow(List<int> availableWallSlots, List<AssignedTile> allocatedTiles, TileType tile)
        {
            // If we have an allocated tile that matches 'tile' that hasn't been assigned yet, assign it.
            foreach (AssignedTile allocatedTile in allocatedTiles)
            {
                if (allocatedTile.Tile.IsEqual(tile, true) && !allocatedTile.Assigned)
                {
                    allocatedTile.Assigned = true;
                    return;
                }
            }

            // Otherwise take the first available slot and allocate/assign it. If we don't have any wall slots then oh well.
            if (availableWallSlots.Count > 0)
            {
                WallRaw[availableWallSlots.Pop()].Type = tile;
                allocatedTiles.Add(new AssignedTile(tile) { Assigned = true });
                return;
            }

            // Couldn't assign the tile! We should have been able to. Throw!
            throw new Exception("Couldn't assign tile, none allocated and no available wall slots!");
        }

        private void ProcessBoardOverride_AssignAndEnsureHandHasTiles(
            IGameState state,
            IHand playerHand,
            List<int> availableWallSlots,
            List<AssignedTile> allocatedTiles,
            TileType[] tiles)
        {
            // Assign the tiles.
            CommonHelpers.Iterate(tiles, (TileType type) => { ProcessBoardOverride_AssignTileOrThrow(availableWallSlots, allocatedTiles, type); });

            // Ensure that playerHand has all of the tiles in 'tiles' in it.
            bool[] tilesFound = new bool[tiles.Length];
            for (int i = 0; i < tilesFound.Length; ++i) { tilesFound[i] = false; }

            HandHelpers.IterateTiles(playerHand, (TileType handTile) =>
            {
                for (int i = 0; i < tiles.Length; ++i)
                {
                    if (!tilesFound[i] && tiles[i].IsEqual(handTile, true))
                    {
                        tilesFound[i] = true;
                        break;
                    }
                }
            });

            bool anyTilesNotFound = false;
            foreach (bool tileFound in tilesFound)
            {
                if (!tileFound)
                {
                    anyTilesNotFound = true;
                    break;
                }
            }

            if (anyTilesNotFound)
            {
                List<TileType> tilesRemove = new List<TileType>();
                List<TileType> tilesAdd = new List<TileType>();
                HandHelpers.IterateTiles(playerHand, (TileType handTile) =>
                {
                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        if (!tilesFound[i] && !tiles[i].IsEqual(handTile, true))
                        {
                            tilesFound[i] = true;
                            tilesRemove.Add(handTile);
                            tilesAdd.Add(tiles[i]);
                            break;
                        }
                    }
                });
                playerHand.ReplaceTiles(tilesRemove, tilesAdd);
            }
        }
    }
}
