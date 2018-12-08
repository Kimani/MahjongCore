// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

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
        public event PlayerIndexEventHandler        WallPicking;
        public event TileCollectionEventHandler     WallPicked;
        public event PlayerTileEventHandler         WallPickUndone;
        public event WinResultEventHandler          Ron;
        public event WinResultEventHandler          Tsumo;
        public event PlayerEventHandler             WinUndone;
        public event DiscardInfoEventHandler        DiscardRequested;
        public event PostDiscardInfoEventHandler    PostDiscardRequested;
        public event PostDiscardInfoEventHandler    PostKanRequested;
        public event WinCollectionEventHandler      MultiWin;
        public event WinResultEventHandler          ExhaustiveDraw;
        public event PlayerAbortiveDrawEventHandler AbortiveDraw;
        public event GameResultEventHandler         GameComplete;
        public event EventHandler                   DiceRolled;
        public event EventHandler                   DeadWallMoved;
        public event TileEventHandler               DoraIndicatorFlipped;
        public event EventHandler                   PreCheckAdvance;
        public event EventHandler                   TableCleared;
        public event EventHandler                   PreCheckRewind;
        public event PlayerMeldEventHandler         DecisionCancelled;

#pragma warning disable 67
        public event PlayerEventHandler             Chombo;
#pragma warning restore 67

        public ITile[]          Wall               { get { return WallRaw; } }
        public ITile[]          DoraIndicators     { get { return DoraIndicatorsRaw; } }
        public ITile[]          UraDoraIndicators  { get { return UraDoraIndicatorsRaw; } }
        public Round            Round              { get; internal set; } = Round.East1;
        public Player           FirstDealer        { get; internal set; } = Player.None;
        public Player           Dealer             { get; internal set; } = Player.None;
        public Player           Current            { get; internal set; } = Player.None;
        public Player           Wareme             { get; internal set; } = Player.None;
        public PlayState        State              { get; internal set; } = PlayState.PreGame;
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
        
        internal GameAction    NextAction               { get; set; }         = GameAction.Nothing;
        internal Player        PlayerRecentOpenKan      { get; set; }         = Player.None;
        internal Player        NextActionPlayer         { get; set; }         = Player.None;
        internal TileType      NextActionTile           { get; set; }         = TileType.None;
        internal bool          PlayerDeadWallPick       { get; set; }         = false;
        internal bool          FlipDoraAfterNextDiscard { get; set; }         = false;
        internal bool          ChankanFlag              { get; private set; } = false;
        internal bool          KanburiFlag              { get; private set; } = false;

        private Dictionary<PlayState, Action> _PreBreakStateHandlers  = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _PostBreakStateHandlers = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindPreHandlers      = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindPostHandlers     = new Dictionary<PlayState, Action>();
        private WinResultImpl                 _WinResultCache         = new WinResultImpl();
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
        private void FlipDora()                                            { DoraIndicatorFlipped?.Invoke(DoraIndicators[DoraCount++]); }
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
            // Populate the discard info and then clear OverrideNoReach, which may get used during Populate.
            HandImpl hand = GetHand(Current);
            _DiscardInfoCache.Populate(hand);
            hand.OverrideNoReachFlag = false;

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
                (PreviousAction == GameAction.ReplacementTilePick) &&
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
                    PreviousAction = GameAction.Discard;
                    _AdvanceAction = (TilesRemaining == 0) ? AdvanceAction.HandEnd : AdvanceAction.Advance;
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
                    _NextActionSlot = -1;
                    _AdvanceAction = AdvanceAction.HandEnd;
                    _NextAbortiveDrawType = AbortiveDrawType.FiveKans;
                }
                else
                {
                    // Set PrevAction to the call type to show that we're discarding from a call.
                    PreviousAction = NextAction; // Becomes GameAction.Chii/Pon/OpenKan
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
                    if (furitenDiscardingPlayer != p) { GetHand(p).UpdateTemporaryFuriten(furitenDiscardedTile); }
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
            WinResultImpl[] multiWinResults = null;
            bool consumePool = !_NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiConsumesPool);
            int bonus = (!_NagashiWin || Settings.GetSetting<bool>(GameOption.NagashiUsesBonus)) ? Bonus : 0;

            if (NextActionPlayer == Player.Multiple)
            {
                int winCount = 0;
                foreach (Player p in PlayerHelpers.Players) { if (GetNextAction(p).IsAgari()) { ++winCount; } }
                multiWinResults = new WinResultImpl[winCount];

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
                        multiWinResults[resultSlot++] = new WinResultImpl(this,
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
                HandImpl winner = GetHand(NextActionPlayer);
                winner.Streak++;
                winner.Yakitori = false;
                foreach (Player p in PlayerHelpers.Players) { if (p != NextActionPlayer) { GetHand(p).Streak = 0; } }

                _WinResultCache.Populate(this,
                                         NextActionPlayer,
                                         ((NextAction == GameAction.Ron) ? _NextActionPlayerTarget : Player.Multiple),
                                         PlayerRecentOpenKan,
                                         NextAction.GetWinType(),
                                         bonus,
                                         (consumePool ? ConsumePool() : 0));
            }
            else if (NextAction == GameAction.Nothing)
            {
                foreach (Player p in PlayerHelpers.Players) { GetHand(p).Streak = 0; }
                _WinResultCache.PopulateDraw(Player1Hand.Tempai, Player2Hand.Tempai, Player3Hand.Tempai, Player4Hand.Tempai);
            }

            // Apply deltas to the game state.
            if (NextActionPlayer == Player.Multiple) { foreach (WinResultImpl result in multiWinResults) { ApplyWinResultsToPlayerScores(result); } }
            else                                     { ApplyWinResultsToPlayerScores(_WinResultCache); }

            // Notify the sink.
            if      (NextActionPlayer == Player.Multiple)   { MultiWin?.Invoke(multiWinResults); }
            else if (NextAction == GameAction.Tsumo)        { Tsumo?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.Ron)          { Ron?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.Nothing)      { ExhaustiveDraw?.Invoke(_WinResultCache); }
            else if (NextAction == GameAction.AbortiveDraw) { AbortiveDraw?.Invoke(Current, _NextAbortiveDrawType); }
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
                // Determine bonus and dealer.
                bool advanceBonus = (NextActionPlayer == Player.Multiple) ? GetNextAction(Dealer).IsAgari() :
                                    NextAction.IsAgari()                  ? (NextActionPlayer == Dealer) :
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
                else
                {
                    if (_NagashiWin && Settings.GetSetting<bool>(GameOption.NagashiBonusOnEastTempaiOnly))
                    {
                        advanceBonus = GetHand(Dealer).Tempai;
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
            if (DoraCount == 5)
            {
                // This is the 5th kan. Immediately abort.
                NextAction = GameAction.AbortiveDraw;
                NextActionPlayer = Current;
                _NextAbortiveDrawType = AbortiveDrawType.FiveKans;
                _AdvanceAction = AdvanceAction.HandEnd;
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
                _AdvanceAction = AdvanceAction.GatherDecisions;
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
                _SkipAdvancePlayer = true;
                _AdvanceAction = AdvanceAction.PrePickTile;
            }
            else
            {
                // HandEnd will process the ron.
                _AdvanceAction = AdvanceAction.HandEnd;
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
                                                    Player4Hand.Yakitori));
        }

        public void ExecuteRewindModeChange_DecideMove()
        {
            CommonHelpers.Check((_RewindAction != GameAction.Nothing), "Expected _RewindAction to not be Nothing, but it is!");
            State = (_RewindAction == GameAction.OpenKan) ||
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
            PreviousAction = GameAction.Nothing;
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
                case AdvanceAction.Done:    state = PlayState.NA;
                                            break;

                case AdvanceAction.Advance: state = State.GetNext();
                                            advancePlayer = EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(State.GetNext());
                                            break;

                default:                    throw new Exception("Unexpected AdvanceAction");
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
                        DecisionCancelled?.Invoke(p, GetHand(p).CachedCall);
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
                case DiscardDecisionType.Discard:           PerformDiscardState(decision.Tile, GameAction.Discard);
                                                            hand.PerformDiscard(decision.Tile, ReachType.None);
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
                                                            _NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            hand.PerformClosedKan(decision.Tile.Type);
                                                            break;

                case DiscardDecisionType.PromotedKan:       NextActionTile = decision.Tile.Type;
                                                            NextAction = GameAction.PromotedKan;
                                                            _NextActionSlot = decision.Tile.Slot;
                                                            action = AdvanceAction.KanChosenTile;
                                                            hand.PerformPromotedKan(decision.Tile);
                                                            break;

                case DiscardDecisionType.AbortiveDraw:      NextActionPlayer = Current;
                                                            NextAction = GameAction.AbortiveDraw;
                                                            _NextAbortiveDrawType = (decision.Tile == null) ? AbortiveDrawType.KyuushuuKyuuhai : AbortiveDrawType.Suufuurendan;
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

            WallPicked?.Invoke(wallTiles);
            GetHand(p).AddTileCompleted(tiles, source);
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
