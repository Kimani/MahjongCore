// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.AI;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MahjongCore.Riichi
{
    #region GameAction
        public enum GameAction
        {
            [SkyValue(0)]  Nothing,
            [SkyValue(1)]  Discard,
            [SkyValue(2)]  Chii,                // Sorting Chii, Pon, OpenKan and Ron
            [SkyValue(3)]  Pon,                 // in ascending order on purpose so
            [SkyValue(4)]  OpenKan,             // you can just sort in order to
            [SkyValue(5)]  Ron,                 // determine what should be done.
            [SkyValue(6)]  Tsumo,
            [SkyValue(7)]  ClosedKan,
            [SkyValue(8)]  PromotedKan,
            [SkyValue(9)]  RiichiDiscard,
            [SkyValue(10)] DecisionPending,
            [SkyValue(11)] PickedFromWall,
            [SkyValue(12)] ReplacementTilePick, // If set, we want to pick our tile from the dead wall instead of the regular wall.
            [SkyValue(13)] AbortiveDraw,        // If Kyuushuukyuuhai or too many kans.
            [SkyValue(14)] OpenRiichiDiscard,   // Will need to update marshalling if this goes over 0xf
        }

        public static class GameActionExtentionMethods
        {
            public static int  GetSkyValue(this GameAction ga)                  { return EnumAttributes.GetAttributeValue<SkyValue, int>(ga); }
            public static bool TryGetGameAction(string text, out GameAction ga) { return EnumHelper.TryGetEnumByCode<GameAction, SkyValue>(text, out ga); }
            public static bool IsAgari(this GameAction ga)                      { return (ga == GameAction.Ron) || (ga == GameAction.Tsumo); }
        }
    #endregion

    #region PlayState
        // If AdvancePlayer is true, should advance the player upon starting the round.
        public enum PlayState
        {
            [SkyValue(0)]                                                     NA,
            [SkyValue(1),  NextState(RandomizingBreak), AdvancePlayer(false)] PreGame,
            [SkyValue(2),  NextState(PreTilePick1),     AdvancePlayer(false)] RandomizingBreak,
            [SkyValue(3),  NextState(TilePick1),        AdvancePlayer(false)] PreTilePick1,      // Pick 4
            [SkyValue(4),  NextState(PreTilePick2),     AdvancePlayer(false)] TilePick1,
            [SkyValue(5),  NextState(TilePick2),        AdvancePlayer(true) ] PreTilePick2,
            [SkyValue(6),  NextState(PreTilePick3),     AdvancePlayer(false)] TilePick2,
            [SkyValue(7),  NextState(TilePick3),        AdvancePlayer(true) ] PreTilePick3,
            [SkyValue(8),  NextState(PreTilePick4),     AdvancePlayer(false)] TilePick3,
            [SkyValue(9),  NextState(TilePick4),        AdvancePlayer(true) ] PreTilePick4,
            [SkyValue(10), NextState(PreTilePick5),     AdvancePlayer(false)] TilePick4,
            [SkyValue(11), NextState(TilePick5),        AdvancePlayer(true) ] PreTilePick5,      // Pick another 4
            [SkyValue(12), NextState(PreTilePick6),     AdvancePlayer(false)] TilePick5,
            [SkyValue(13), NextState(TilePick6),        AdvancePlayer(true) ] PreTilePick6,
            [SkyValue(14), NextState(PreTilePick7),     AdvancePlayer(false)] TilePick6,
            [SkyValue(15), NextState(TilePick7),        AdvancePlayer(true) ] PreTilePick7,
            [SkyValue(16), NextState(PreTilePick8),     AdvancePlayer(false)] TilePick7,
            [SkyValue(17), NextState(TilePick8),        AdvancePlayer(true) ] PreTilePick8,
            [SkyValue(18), NextState(PreTilePick9),     AdvancePlayer(false)] TilePick8,
            [SkyValue(19), NextState(TilePick9),        AdvancePlayer(true) ] PreTilePick9,      // Pick another 4
            [SkyValue(20), NextState(PreTilePick10),    AdvancePlayer(false)] TilePick9,
            [SkyValue(21), NextState(TilePick10),       AdvancePlayer(true) ] PreTilePick10,
            [SkyValue(22), NextState(PreTilePick11),    AdvancePlayer(false)] TilePick10,
            [SkyValue(23), NextState(TilePick11),       AdvancePlayer(true) ] PreTilePick11,
            [SkyValue(24), NextState(PreTilePick12),    AdvancePlayer(false)] TilePick11,
            [SkyValue(25), NextState(TilePick12),       AdvancePlayer(true) ] PreTilePick12,
            [SkyValue(26), NextState(PreTilePick13),    AdvancePlayer(false)] TilePick12,
            [SkyValue(27), NextState(TilePick13),       AdvancePlayer(true) ] PreTilePick13,     // Pick the last 1.
            [SkyValue(28), NextState(PreTilePick14),    AdvancePlayer(false)] TilePick13,
            [SkyValue(29), NextState(TilePick14),       AdvancePlayer(true) ] PreTilePick14,
            [SkyValue(30), NextState(PreTilePick15),    AdvancePlayer(false)] TilePick14,
            [SkyValue(31), NextState(TilePick15),       AdvancePlayer(true) ] PreTilePick15,
            [SkyValue(32), NextState(PreTilePick16),    AdvancePlayer(false)] TilePick15,
            [SkyValue(33), NextState(TilePick16),       AdvancePlayer(true) ] PreTilePick16,
            [SkyValue(34), NextState(DeadWallMove),     AdvancePlayer(false)] TilePick16,
            [SkyValue(35), NextState(PrePickTile),      AdvancePlayer(false)] DeadWallMove,      // Move the dead wall over.
            [SkyValue(36), NextState(PickTile),         AdvancePlayer(true) ] PrePickTile,
            [SkyValue(37), NextState(DecideMove),       AdvancePlayer(false)] PickTile,          // Pick the tile.
            [SkyValue(38),                              AdvancePlayer(false)] DecideMove,        // Decide your move. This can goto GatherDecisions or KanChosenTile.
            [SkyValue(39), NextState(PerformDecision),  AdvancePlayer(false)] GatherDecisions,   // Discard occurred. Decisions on NoAction/Chii/Pon/Kan/Ron are made.
            [SkyValue(40), NextState(PrePickTile),      AdvancePlayer(false)] PerformDecision,   // Winning decision occurs. This can goto PickTile, HandEnd, or DecideMove.
            [SkyValue(41)]                                                    NextTurn,          // Clear to move to the next turn. Advances the state to HandEnd or PickTile.
            [SkyValue(42), NextState(TableCleanup)]                           HandEnd,           // End of the hand. Can move to TableCleanup.
            [SkyValue(43)]                                                    TableCleanup,      // Clean up the table. Advances round. Can advance to GameEnd or RandomizingBreak.
            [SkyValue(44)]                                                    GameEnd,           // End of game. Cannot advance!
            [SkyValue(45), NextState(KanPerformDecision)]                     KanChosenTile,
            [SkyValue(46)]                                                    KanPerformDecision,
        }

        public static class PlayStateExtentionMethods
        {
            public static PlayState GetNext(this PlayState s)
            {
                PlayState nextState = PlayState.NA;
                if (EnumAttributes.HasAttributeValue(s, typeof(NextState)))
                {
                    nextState = EnumAttributes.GetAttributeValue<NextState, PlayState>(s);
                }
                return nextState;
            }

            public static int  GetSkyValue(this PlayState ps)                 { return EnumAttributes.GetAttributeValue<SkyValue, int>(ps); }
            public static bool TryGetPlayState(string text, out PlayState ps) { return EnumHelper.TryGetEnumByCode<PlayState, SkyValue>(text, out ps); }
        }
    #endregion

    public enum TileSource
    {
        WallDraw,            /// Tile came from the wall. A regular draw.
        ReplacementTileDraw, /// Tile came from the dead wall after a kan.
        Call,                /// Tile came from performing a chii or a pon.
    }

    public enum Placement
    {
        Place1,
        Place2,
        Place3,
        Place4
    };

    #region Round
        public enum Round
        {
            [DescriptionName("East 1"),  TextValue("e1"), RoundOffset(0), NextRound(East2)]  East1,
            [DescriptionName("East 2"),  TextValue("e2"), RoundOffset(1), NextRound(East3)]  East2,
            [DescriptionName("East 3"),  TextValue("e3"), RoundOffset(2), NextRound(East4)]  East3,
            [DescriptionName("East 4"),  TextValue("e4"), RoundOffset(3), NextRound(South1)] East4,
            [DescriptionName("South 1"), TextValue("s1"), RoundOffset(0), NextRound(South2)] South1,
            [DescriptionName("South 2"), TextValue("s2"), RoundOffset(1), NextRound(South3)] South2,
            [DescriptionName("South 3"), TextValue("s3"), RoundOffset(2), NextRound(South4)] South3,
            [DescriptionName("South 4"), TextValue("s4"), RoundOffset(3), NextRound(West1)]  South4,
            [DescriptionName("West 1"),  TextValue("w1"), RoundOffset(0), NextRound(West2)]  West1,
            [DescriptionName("West 2"),  TextValue("w2"), RoundOffset(1), NextRound(West3)]  West2,
            [DescriptionName("West 3"),  TextValue("w3"), RoundOffset(2), NextRound(West4)]  West3,
            [DescriptionName("West 4"),  TextValue("w4"), RoundOffset(3), NextRound(North1)] West4,
            [DescriptionName("North 1"), TextValue("n1"), RoundOffset(0), NextRound(North2)] North1,
            [DescriptionName("North 2"), TextValue("n2"), RoundOffset(1), NextRound(North3)] North2,
            [DescriptionName("North 3"), TextValue("n3"), RoundOffset(2), NextRound(North4)] North3,
            [DescriptionName("North 4"), TextValue("n4"), RoundOffset(3), NextRound(East1)]  North4
        };

        public static class RoundExtensionMethods
        {
            public static Round  GetNext(this Round r)      { return EnumAttributes.GetAttributeValue<NextRound, Round>(r); }
            public static int    GetOffset(this Round r)    { return EnumAttributes.GetAttributeValue<RoundOffset, int>(r); }
            public static string GetTextValue(this Round r) { return EnumAttributes.GetAttributeValue<TextValue, string>(r); }
            public static string GetDescName(this Round r)  { return EnumAttributes.GetAttributeValue<DescriptionName, string>(r); }

            public static bool TryGetRound(string text, out Round c)
            {
                Round? rResult = EnumHelper.GetEnumValueFromAttribute<Round, TextValue, string>(text);
                c = (rResult != null) ? rResult.Value : default(Round);
                return rResult != null;
            }
        }
    #endregion

    #region Player
        public enum Player
        {
            [IsSinglePlayer(false), PlayerValue(0)]                               None,
            [IsSinglePlayer(false), PlayerValue(5)]                               All,
            [IsSinglePlayer(false), PlayerValue(6)]                               Multiple,
            [IsSinglePlayer(true),  TextValue("1"), PlayerValue(1), ZeroIndex(0)] Player1,
            [IsSinglePlayer(true),  TextValue("2"), PlayerValue(2), ZeroIndex(1)] Player2,
            [IsSinglePlayer(true),  TextValue("3"), PlayerValue(3), ZeroIndex(2)] Player3,
            [IsSinglePlayer(true),  TextValue("4"), PlayerValue(4), ZeroIndex(3)] Player4
        };

        public static class PlayerExtensionMethods
        {
            public static Player[] Players = new Player[] { Player.Player1, Player.Player2, Player.Player3, Player.Player4 };

            public static bool IsPlayer(this Player p)       { return EnumAttributes.GetAttributeValue<IsSinglePlayer, bool>(p); }
            public static int  GetZeroIndex(this Player p)   { return EnumAttributes.GetAttributeValue<ZeroIndex, int>(p); }
            public static int  GetPlayerValue(this Player p) { return EnumAttributes.GetAttributeValue<PlayerValue, int>(p); }

            public static bool TryGetPlayer(string text, out Player p)
            {
                int textValue;
                bool found = int.TryParse(text, out textValue);
                if (found)
                {
                    Player? pResult = EnumHelper.GetEnumValueFromAttribute<Player, PlayerValue, int>(textValue);
                    p = (pResult != null) ? pResult.Value : default(Player);
                }
                else
                {
                    p = default(Player);
                }
                return found;
            }

            public static Player GetNext(this Player p)
            {
                return (p == Player.Player1) ? Player.Player2 :
                       (p == Player.Player2) ? Player.Player3 :
                       (p == Player.Player3) ? Player.Player4 :
                                               Player.Player1;
            }

            public static Player GetPrevious(this Player p)
            {
                return (p == Player.Player1) ? Player.Player4 :
                       (p == Player.Player2) ? Player.Player1 :
                       (p == Player.Player3) ? Player.Player2 :
                                               Player.Player3;
            }

            public static CalledDirection GetTargetPlayerDirection(this Player p, Player target)
            {
                return (!p.IsPlayer() || (p == target)) ? CalledDirection.None :
                        (p.GetNext() == target)         ? CalledDirection.Right :
                        (p.GetPrevious() == target)     ? CalledDirection.Left :
                                                          CalledDirection.Across;
            }

            public static Player AddOffset(this Player p, int offset)
            {
                int playerValue = EnumAttributes.GetAttributeValue<PlayerValue, int>(p);
                int offsetValue = playerValue + offset - 1;
                while (offsetValue < 0)
                {
                    offsetValue += 4;
                }
                int targetPlayer = (offsetValue % 4) + 1;
                return (targetPlayer == 1) ? Player.Player1 :
                       (targetPlayer == 2) ? Player.Player2 :
                       (targetPlayer == 3) ? Player.Player3 :
                                             Player.Player4;
            }

            public static Player GetRandom()
            {
                int targetPlayer = RiichiGlobal.RandomRange(1, 5);
                return (targetPlayer == 1) ? Player.Player1 :
                       (targetPlayer == 2) ? Player.Player2 :
                       (targetPlayer == 3) ? Player.Player3 :
                                             Player.Player4;
            }
        }
    #endregion

    public class DiscardInfo
    {
        public bool           InReach;
        public bool           CanNormalDiscard;
        public bool           CanKyuushuuKyuuhai;
        public bool           CanTsumo;
        public bool           CanReach;
        public bool           CanOpenReach;
        public List<TileType> RestrictedTiles = new List<TileType>();
        public TileType       SuufurendanTile;
        public KanOptions     KanOptions;
    }

    public interface GameStateSink
    {
        void HandCleared(Player p);
        void HandSort(Player p, bool initialSort);
        void HandPrePickTile(Player p, int count);
        void HandTileAdded(Player p, int count);
        void HandPerformedAbortiveDraw(Player p, TileType tile, int handSlot);
        void HandPerformedDiscard(Player p, TileType tile, int handSlot);
        void HandPerformedReach(Player p, TileType tile, int handSlot, bool fOpenReach);
        void HandPerformedKan(Player p, TileType tile, KanType type);
        void HandPerformedStoredCall(Player p, CallOption co);
        void HandPerformedRon(Player p, WinResults results);
        void HandPerformedTsumo(Player p, WinResults results);
        void MultiWin(WinResults win1, WinResults win2, WinResults win3, WinResults win4);
        void ExhaustiveDraw(WinResults results);
        void GatherPostDiscardDecisions();
        void GatherPostKanDecisions();
        void PerformSave();
        void PerformRandomizingBreak(int finalRoll);
        void PerformDeadWallMove();
        void PerformDiscard(Player p, DiscardInfo info);
        void DoraTileFlipped();
        bool PreCheckPlayState(PlayState state);
        void WallTilePicked(int[] tileSlot, TileSource source);
        void GameComplete(GameResults results);
        void TableCleanUpForNextRound();

        // Rewind callbacks
        bool PreCheckRewindPlayState(PlayState state);
        void DiscardUndone(Player p, TileType tile);
        void TsumoUndone(Player p);
        void DrawUndone(Player p, TileType tile);
    }

    // GameState is intended to be a pure implementation of a riichi mahjong game state. Any hooks into the GameState should
    // be done through GameStateSink. It will let you know when things happen, and you can react to them at that time.
    public class GameState
    {
        public TileColor        TileColor          { get; private set; }
        public TileType[]       Wall               { get; private set; } = new TileType[TileHelpers.TOTAL_TILE_COUNT];
        public TileType[]       DoraIndicators     { get; private set; } = new TileType[5];
        public TileType[]       UraDoraIndicators  { get; private set; } = new TileType[5];
        public Round            CurrentRound       { get; private set; }
        public Player           CurrentDealer      { get; private set; }
        public Player           CurrentPlayer      { get; private set; }
        public Player           WaremePlayer       { get; private set; }
        public Player           NextActionPlayer   { get; private set; }
        public PlayState        CurrentState       { get; private set; }
        public GameAction       PrevAction         { get; private set; }
        public GameAction       NextAction         { get; private set; }
        public GameSettings     Settings           { get; private set; }
        public TutorialSettings TutorialSettings   { get; private set; }
        public Hand             Player1Hand        { get; private set; }
        public Hand             Player2Hand        { get; private set; }
        public Hand             Player3Hand        { get; private set; }
        public Hand             Player4Hand        { get; private set; }
        public TileType         NextActionTile     { get; private set; }
        public bool             CurrentRoundLapped { get; private set; }
        public bool             PlayerDeadWallPick { get; private set; }
        public bool             ChankanFlag        { get; private set; }
        public bool             KanburiFlag        { get; private set; }
        public bool             IsTutorial         { get; private set; }
        public int              NextActionSlot     { get; private set; } = -1;
        public int              Offset             { get; private set; }
        public int              TilesRemaining     { get; private set; }
        public int              Bonus              { get; private set; }
        public int              Pool               { get; private set; }
        public int              DoraCount          { get; private set; }
        public int              Roll               { get; private set; }
        public GameStateSink    Sink               { private get; set; }

        public Player           PlayerRecentOpenKan      { get; private set; }
        public bool             FlipDoraAfterNextDiscard { get; private set; }
        public Stack<Player>    DiscardPlayerList        { get; private set; } = new Stack<Player>();

        private List<ExtendedTile>            _Player1Discards = new List<ExtendedTile>();
        private List<ExtendedTile>            _Player2Discards = new List<ExtendedTile>();
        private List<ExtendedTile>            _Player3Discards = new List<ExtendedTile>();
        private List<ExtendedTile>            _Player4Discards = new List<ExtendedTile>();
        private bool                          _IppatsuFlag1 = false;
        private bool                          _IppatsuFlag2 = false;
        private bool                          _IppatsuFlag3 = false;
        private bool                          _IppatsuFlag4 = false;
        
        private Dictionary<PlayState, Action> _PreBreakStateHandlers = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _PostBreakStateHandlers = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindModeChangeHandlers = new Dictionary<PlayState, Action>();
        private Dictionary<PlayState, Action> _RewindPostModeChangeHandlers = new Dictionary<PlayState, Action>();
        private DiscardInfo                   _DiscardInfoCache = new DiscardInfo();
        private GameAction                    _NextAction1 = GameAction.DecisionPending;
        private GameAction                    _NextAction2 = GameAction.DecisionPending;
        private GameAction                    _NextAction3 = GameAction.DecisionPending;
        private GameAction                    _NextAction4 = GameAction.DecisionPending;
        private Player                        _NextActionPlayerTarget = Player.None;
        private WinResults                    _WinResultCache = new WinResults();
        private bool                          _SkipAdvancePlayer = false;
        private WinResults[]                  _MultiWinResults = new WinResults[] { new WinResults(), new WinResults(), new WinResults(), new WinResults() };
        private GameAction                    _RewindAction = GameAction.Nothing;

        public GameState(GameStateSink sink, GameSettings settings)                { Initialize(sink, settings, null); }
        public GameState(GameStateSink sink, TutorialSettings tSettings)           { Initialize(sink, null, tSettings); }
        public GameState(GameStateSink sink, SaveState state)                      { InitializeFromState(sink, state, null); }
        public GameState(GameStateSink sink, SaveState state, TutorialSettings ts) { InitializeFromState(sink, state, ts); }
        public SaveState GetSaveState()                                            { return SaveState.GetState(this); }
        public void ContinueCurrentPlayState()                                     { AdvancePlayState(CurrentState, false, true); } // Used by tutorial to continue execution of the game after being paused.
        public void AdvancePlayState()                  { AdvancePlayState(CurrentState.GetNext(), EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(CurrentState.GetNext()), false); }
        private void StartPlayState(PlayState mode)     { AdvancePlayState(mode, EnumAttributes.GetAttributeValue<AdvancePlayer, bool>(mode), false); }

        private void Initialize(GameStateSink sink, GameSettings gs, TutorialSettings ts)
        {
            InitializeCommon(sink, gs, ts);

            // Initialize everything else.
            TileColor                 = TileColor.Orange;
            Offset                    = 0;
            CurrentRound              = Round.East1;
            CurrentDealer             = PlayerExtensionMethods.GetRandom();
            CurrentPlayer             = CurrentDealer;
            CurrentState              = PlayState.PreGame;
            PrevAction                = GameAction.Nothing;
            NextAction                = GameAction.Nothing;
            NextActionTile            = TileType.None;
            NextActionPlayer          = Player.None;
            IsTutorial                = false;
            FlipDoraAfterNextDiscard  = false;
            PlayerRecentOpenKan       = Player.None;
        }

        public void InitializeFromState(GameStateSink sink, SaveState state, TutorialSettings ts)
        {
            InitializeCommon(sink, state.CustomSettings, ts);

            // Initialize other things.
            NextActionPlayer = CurrentPlayer;
            _NextAction1     = GameAction.Nothing;
            _NextAction2     = GameAction.Nothing;
            _NextAction3     = GameAction.Nothing;
            _NextAction4     = GameAction.Nothing;

            // Initialize from state.
            TileColor                 = state.TileColor;
            CurrentRound              = state.CurrentRound;
            CurrentRoundLapped        = state.CurrentRoundLapped;
            Bonus                     = state.Bonus;
            Pool                      = state.Pool;
            Roll                      = state.Roll;
            Offset                    = state.Offset;
            TilesRemaining            = state.TilesRemaining;
            DoraCount                 = state.DoraCount;
            CurrentDealer             = state.CurrentDealer;
            CurrentPlayer             = state.CurrentPlayer;
            WaremePlayer              = state.WaremePlayer;
            PlayerDeadWallPick        = state.PlayerDeadWallPick;
            PrevAction                = state.PrevAction;
            NextAction                = state.NextAction;
            CurrentState              = state.CurrentState;
            PlayerRecentOpenKan       = state.PlayerRecentOpenKan;
            FlipDoraAfterNextDiscard  = state.FlipDoraAfterNextDiscard;
            _IppatsuFlag1             = state.Players[0].Ippatsu;
            _IppatsuFlag2             = state.Players[1].Ippatsu;
            _IppatsuFlag3             = state.Players[2].Ippatsu;
            _IppatsuFlag4             = state.Players[3].Ippatsu;
            _Player1Discards          = CommonHelpers.SafeCopy(state.Players[0].Discards);
            _Player2Discards          = CommonHelpers.SafeCopy(state.Players[1].Discards);
            _Player3Discards          = CommonHelpers.SafeCopy(state.Players[2].Discards);
            _Player4Discards          = CommonHelpers.SafeCopy(state.Players[3].Discards);

            for (int i = 0; i < TileHelpers.TOTAL_TILE_COUNT; ++i)
            {
                Wall[i] = state.Wall[i];
            }

            GetDoraIndicators();

            for (int i = 0; i < 4; ++i)
            {
                GetHandZeroIndexed(i).Load(state.Players[i]);
            }

            NextActionTile = TileType.None;
            if (CurrentState == PlayState.GatherDecisions)
            {
                // Set NextActionTile to the most recently discarded tile. Should be for the current player.
                List<ExtendedTile> discards = GetDiscards(CurrentPlayer);
                NextActionTile = discards[discards.Count - 1].Tile;
            }
            else if (CurrentState == PlayState.KanPerformDecision)
            {
                // Set NextActionTile to the last tile of the most recent Kan. Should be for the current player.
                // TODO: This. We need to figure out which promoted kan was the thing. Probably need to store NextActionTile in the state I guess.
            }

            DiscardPlayerList.Clear();
            foreach (Player p in state.DiscardPlayerList)
            {
                DiscardPlayerList.Push(p);
            }
        }

        private void InitializeCommon(GameStateSink sink, GameSettings gs, TutorialSettings ts)
        {
            // Set settings and determine if we're in tutorial mode if ts is null or not.
            Sink             = sink;
            Settings         = (gs != null) ? gs : new GameSettings();
            IsTutorial       = (ts != null);
            TutorialSettings = (ts != null) ? ts : new TutorialSettings();

            int score = Settings.GetSetting<int>(GameOption.StartingPoints);
            Player1Hand = new Hand(this, Player.Player1, score);
            Player2Hand = new Hand(this, Player.Player2, score);
            Player3Hand = new Hand(this, Player.Player3, score);
            Player4Hand = new Hand(this, Player.Player4, score);

            foreach (PlayState ps in Enum.GetValues(typeof(PlayState)))
            {
                TryAddStateHandlerFunction(ps, _PreBreakStateHandlers, "ExecutePreBreak_");
                TryAddStateHandlerFunction(ps, _PostBreakStateHandlers, "ExecutePostBreak_");
                TryAddStateHandlerFunction(ps, _RewindModeChangeHandlers, "ExecuteRewindModeChange_");
                TryAddStateHandlerFunction(ps, _RewindPostModeChangeHandlers, "ExecuteRewindPostModeChange_");
            }
        }

        public void SubmitPostDiscardDecision(Player p, PostDiscardDecision decision)
        {
            if (decision.Validate())
            {
                // Set the decision.
                GameAction action = (decision.DecisionToMake == PostDiscardDecision.Decision.Ron)     ? GameAction.Ron :
                                    (decision.DecisionToMake == PostDiscardDecision.Decision.Nothing) ? GameAction.Nothing :
                                    (decision.CallToMake.Type == MeldState.Chii)                      ? GameAction.Chii :
                                    (decision.CallToMake.Type == MeldState.Pon)                       ? GameAction.Pon :
                                                                                                        GameAction.OpenKan;

                if      (p == Player.Player1) { _NextAction1 = action; Player1Hand.StoredCallOption = decision.CallToMake; }
                else if (p == Player.Player2) { _NextAction2 = action; Player2Hand.StoredCallOption = decision.CallToMake; }
                else if (p == Player.Player3) { _NextAction3 = action; Player3Hand.StoredCallOption = decision.CallToMake; }
                else if (p == Player.Player4) { _NextAction4 = action; Player4Hand.StoredCallOption = decision.CallToMake; }

                // Take action if all decisions are accounted for.
                if ((_NextAction1 != GameAction.DecisionPending) && (_NextAction2 != GameAction.DecisionPending) &&
                    (_NextAction3 != GameAction.DecisionPending) && (_NextAction4 != GameAction.DecisionPending))
                {
                    // Set up the next action that is to be taken.
                    NextAction = GameAction.Nothing;

                    if (_NextAction1.GetSkyValue() > NextAction.GetSkyValue()) { NextAction = _NextAction1; NextActionPlayer = Player.Player1; }
                    if (_NextAction2.GetSkyValue() > NextAction.GetSkyValue()) { NextAction = _NextAction2; NextActionPlayer = Player.Player2; }
                    if (_NextAction3.GetSkyValue() > NextAction.GetSkyValue()) { NextAction = _NextAction3; NextActionPlayer = Player.Player3; }
                    if (_NextAction4.GetSkyValue() > NextAction.GetSkyValue()) { NextAction = _NextAction4; NextActionPlayer = Player.Player4; }

                    // Check to see if multiple people have ronned.
                    if (NextAction == GameAction.Ron)
                    {
                        int ronCount = ((_NextAction1 == GameAction.Ron) ? 1 : 0) + ((_NextAction2 == GameAction.Ron) ? 1 : 0) +
                                       ((_NextAction3 == GameAction.Ron) ? 1 : 0) + ((_NextAction4 == GameAction.Ron) ? 1 : 0);
                        if (ronCount > 1)
                        {
                            NextActionPlayer = Player.Multiple;
                            if (_NextAction1 != GameAction.Ron) { Player1Hand.StoredCallOption = null; }
                            if (_NextAction2 != GameAction.Ron) { Player2Hand.StoredCallOption = null; }
                            if (_NextAction3 != GameAction.Ron) { Player3Hand.StoredCallOption = null; }
                            if (_NextAction4 != GameAction.Ron) { Player4Hand.StoredCallOption = null; }
                        }
                    }

                    if (NextActionPlayer != Player.Multiple)
                    {
                        // Clear out stored call options on any players besides the one that won.
                        if (NextActionPlayer != Player.Player1) { Player1Hand.StoredCallOption = null; }
                        if (NextActionPlayer != Player.Player2) { Player2Hand.StoredCallOption = null; }
                        if (NextActionPlayer != Player.Player3) { Player3Hand.StoredCallOption = null; }
                        if (NextActionPlayer != Player.Player4) { Player4Hand.StoredCallOption = null; }
                    }

                    // Goto the next step.
                    AdvancePlayState();
                }
            }
            else
            {
                RiichiGlobal.Assert("Post discard decision failed validation!");
            }
        }

        public void SubmitDiscardDecision(DiscardDecision decision)
        {
            decision.Validate(GetHand(CurrentPlayer));

            if      ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.Discard))           { GetHand(CurrentPlayer).Discard(decision.Slot); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.RiichiDiscard))     { GetHand(CurrentPlayer).Reach(decision.Slot, false); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.OpenRiichiDiscard)) { GetHand(CurrentPlayer).Reach(decision.Slot, true); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.Tsumo))             { HandPerformedTsumo(CurrentPlayer); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.ClosedKan))         { GetHand(CurrentPlayer).Kan(decision.Slot, false); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.PromotedKan))       { GetHand(CurrentPlayer).Kan(decision.Slot, true); }
            else if ((CurrentState == PlayState.DecideMove) && (decision.DecisionToMake == DiscardDecision.Decision.AbortiveDraw))      { GetHand(CurrentPlayer).AbortiveDraw(decision.Slot, (decision.Slot != -1)); }
            else    { RiichiGlobal.Assert(false, "Action and Play State do not match! Action: " + decision.DecisionToMake + " State: " + CurrentState); }
        }

        public void RewindPlayState()
        {
            try
            {
                // Check if we should rewind.
                if (Sink.PreCheckRewindPlayState(CurrentState))
                {
                    // Use the rewind mode handler to rewind the mode to the correct previous mode. This must have a corresponding handler.
                    CommonHelpers.Check(_RewindModeChangeHandlers.ContainsKey(CurrentState), "Cannot rewind from here!");
                    _RewindModeChangeHandlers[CurrentState].Invoke();

                    // Now that we're at a previous mode, execute a handler. This might not exist, that is fine.
                    var handler = _RewindPostModeChangeHandlers[CurrentState];
                    RiichiGlobal.Log("is there a post modechange handler for: " + CurrentState + " ? ---> " + handler);

                    _RewindPostModeChangeHandlers[CurrentState]?.Invoke();
                }
                else
                {
                    // TODO: "Setup state so that it's how it should be at the beginning of this mode." <- whatever this means.
                }
            }
            catch (Exception e)
            {
                RiichiGlobal.Assert(e.Message + "\n" + e.StackTrace);
            }
        }

        private void TryAddStateHandlerFunction(PlayState state, Dictionary<PlayState, Action> basket, string prefix)
        {
            // https://www.roelvanlisdonk.nl/?p=3790
            MethodInfo info = typeof(GameState).GetMethod(prefix + state.ToString());
            if (info != null)
            {
                var thisParameter = Expression.Constant(this);
                MethodCallExpression methodCall = Expression.Call(thisParameter, info);
                Expression<Action> lambda = Expression.Lambda<Action>(methodCall);
                basket.Add(state, lambda.Compile());
            }
        }

        private void AdvancePlayState(PlayState nextMode, bool fAdvancePlayer, bool fSkipCheck)
        {
            // Advance player, set flags/game mode/etc.
            CurrentState = nextMode;
            _RewindAction = GameAction.Nothing;

            if (_SkipAdvancePlayer)
            {
                _SkipAdvancePlayer = false;
            }
            else if (fAdvancePlayer)
            {
                CurrentPlayer = CurrentPlayer.GetNext();
            }

            // Perform things based on the state that can happen before a potential breakpoint. If we're skipping the
            // breakpoint check that also means we shouldn't do this, since we already did it.
            if (!fSkipCheck && _PreBreakStateHandlers.ContainsKey(nextMode))
            {
                _PreBreakStateHandlers[nextMode]?.Invoke();
            }

            // Check to see if we should continue to execute the game mode.
            bool fContinue = true;
            if (!fSkipCheck && IsTutorial)
            {
                fContinue = Sink.PreCheckPlayState(CurrentState);
            }

            // Execute the bulk of the game mode.
            if (fContinue && _PostBreakStateHandlers.ContainsKey(nextMode))
            {
                _PostBreakStateHandlers[nextMode]?.Invoke();
            }
        }

        public void ExecutePreBreak_DeadWallMove()
        {
            // Before we move the dead wall, sort all the hands.
            Player1Hand.Sort(true);
            Player2Hand.Sort(false);
            Player3Hand.Sort(false);
            Player4Hand.Sort(false);
        }

        public void ExecutePostBreak_RandomizingBreak()
        {
            Roll                     = TutorialSettings.OverrideDiceRoll ? TutorialSettings.OverrideDiceRollValue : (RiichiGlobal.RandomRange(1, 7) + RiichiGlobal.RandomRange(1, 7));
            Offset                   = GameStateHelpers.GetOffset(CurrentDealer, Roll);
            TilesRemaining           = 122;
            DoraCount                = 0;
            WaremePlayer             = Settings.GetSetting<bool>(GameOption.Wareme) ? CurrentDealer.AddOffset(Roll - 1) : Player.None;
            Player1Hand.Furiten      = false;
            Player2Hand.Furiten      = false;
            Player3Hand.Furiten      = false;
            Player4Hand.Furiten      = false;
            PlayerRecentOpenKan      = Player.None;
            PlayerDeadWallPick       = false;
            FlipDoraAfterNextDiscard = false;
            _IppatsuFlag1            = false;
            _IppatsuFlag2            = false;
            _IppatsuFlag3            = false;
            _IppatsuFlag4            = false;

            if (!TutorialSettings.OverrideDiceRoll)
            {
                Wall = TileHelpers.GetRandomBoard(Wall, Settings.GetSetting<RedDora>(GameOption.RedDoraOption));
            }

            GetDoraIndicators();
            Sink.PerformRandomizingBreak(Roll);
        }

        public void ExecutePostBreak_PreTilePick1()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick2()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick3()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick4()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick5()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick6()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick7()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick8()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick9()  { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick10() { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick11() { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick12() { Sink.HandPrePickTile(CurrentPlayer, 4); }
        public void ExecutePostBreak_PreTilePick13() { Sink.HandPrePickTile(CurrentPlayer, 1); }
        public void ExecutePostBreak_PreTilePick14() { Sink.HandPrePickTile(CurrentPlayer, 1); }
        public void ExecutePostBreak_PreTilePick15() { Sink.HandPrePickTile(CurrentPlayer, 1); }
        public void ExecutePostBreak_PreTilePick16() { Sink.HandPrePickTile(CurrentPlayer, 1); }
        public void ExecutePostBreak_PrePickTile()   { Sink.PerformSave();
                                                       Sink.HandPrePickTile(CurrentPlayer, 1); }

        public void ExecutePostBreak_TilePick1()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick2()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick3()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick4()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick5()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick6()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick7()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick8()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick9()     { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick10()    { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick11()    { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick12()    { PickIntoPlayerHand(CurrentPlayer, 4); }
        public void ExecutePostBreak_TilePick13()    { PickIntoPlayerHand(CurrentPlayer, 1); }
        public void ExecutePostBreak_TilePick14()    { PickIntoPlayerHand(CurrentPlayer, 1); }
        public void ExecutePostBreak_TilePick15()    { PickIntoPlayerHand(CurrentPlayer, 1); }
        public void ExecutePostBreak_TilePick16()    { PickIntoPlayerHand(CurrentPlayer, 1); }
        public void ExecutePostBreak_PickTile()      { PickIntoPlayerHand(CurrentPlayer, 1); }

        public void ExecutePostBreak_DeadWallMove()
        {
            ++DoraCount;
            Sink.PerformDeadWallMove();
            Player1Hand.Sort(true);
            Player2Hand.Sort(true);
            Player3Hand.Sort(true);
            Player4Hand.Sort(true);
        }

        public void ExecutePostBreak_DecideMove()
        {
            // Collect discard information.
            Hand hand = GetHand(CurrentPlayer);
            bool fOverrideNoReach = hand.OverrideNoReachFlag;
            hand.StartDiscardState();

            _DiscardInfoCache.InReach            = hand.IsInReach();
            _DiscardInfoCache.CanKyuushuuKyuuhai = hand.CanKyuushuuKyuuhai();
            _DiscardInfoCache.CanTsumo           = hand.CanTsumo();
            _DiscardInfoCache.CanReach           = !fOverrideNoReach && hand.CanReach() && (TilesRemaining >= 4);
            _DiscardInfoCache.CanOpenReach       = _DiscardInfoCache.CanReach && Settings.GetSetting<bool>(GameOption.OpenRiichi);
            _DiscardInfoCache.SuufurendanTile    = hand.GetSuufurendanTile();
            _DiscardInfoCache.KanOptions         = hand.GetKanOptions();
            _DiscardInfoCache.CanNormalDiscard   = (!_DiscardInfoCache.InReach || _DiscardInfoCache.KanOptions.HasOptions()) && (!IsTutorial || !TutorialSettings.DisableAnyDiscard);

            _DiscardInfoCache.RestrictedTiles.Clear();
            TileType noDiscardTile = hand.GetNoDiscardTile();
            if (noDiscardTile != TileType.None)
            {
                _DiscardInfoCache.RestrictedTiles.Add(noDiscardTile);
            }

            if (IsTutorial && (TutorialSettings.RestrictDiscardTiles != null) && (TutorialSettings.RestrictDiscardTiles.Count > 0))
            {
                foreach (TileType tt in TutorialSettings.RestrictDiscardTiles)
                {
                    _DiscardInfoCache.RestrictedTiles.Add(tt);
                }
            }

            Sink.PerformDiscard(CurrentPlayer, _DiscardInfoCache);
        }

        public void ExecutePostBreak_GatherDecisions()
        {
            // Should save now.
            Sink.PerformSave();

            // Set up all the actions that will happen. The current player has no say since they just discarded.
            _NextAction1 = (CurrentPlayer == Player.Player1) ? GameAction.Nothing : GameAction.DecisionPending;
            _NextAction2 = (CurrentPlayer == Player.Player2) ? GameAction.Nothing : GameAction.DecisionPending;
            _NextAction3 = (CurrentPlayer == Player.Player3) ? GameAction.Nothing : GameAction.DecisionPending;
            _NextAction4 = (CurrentPlayer == Player.Player4) ? GameAction.Nothing : GameAction.DecisionPending;
            NextActionPlayer = CurrentPlayer;

            // Tell the sink that all four players should now submit decisions.
            Sink.GatherPostDiscardDecisions();
        }

        public void ExecutePostBreak_PerformDecision()
        {
            PlayerDeadWallPick = false; // Can clear this flag at this juncture - Rinshan has already been evaluated.

            bool fUpdateFuriten = false;
            Player furitenDiscardingPlayer = CurrentPlayer;
            TileType furitenDiscardedTile = NextActionTile;

            // Flip the dora if we should now. You don't get it on a ron after discarding after a kan.
            if ((NextAction != GameAction.Ron) && FlipDoraAfterNextDiscard)
            {
                FlipDoraAfterNextDiscard = false;
                if (Settings.GetSetting<bool>(GameOption.KanDora))
                {
                    ++DoraCount;
                    Sink.DoraTileFlipped();
                }
            }

            if ((PrevAction == GameAction.ReplacementTilePick) &&
                (NextAction != GameAction.Ron) &&
                (DoraCount > 4) &&
                !Player1Hand.IsFourKans() &&
                !Player2Hand.IsFourKans() &&
                !Player3Hand.IsFourKans() &&
                !Player4Hand.IsFourKans())
            {
                // This is the fourth kan and no one player has all four kans.
                // And noone ronned on the fourth kan discard.
                // Abortive draw at this jucture.
                NextAction = GameAction.AbortiveDraw;
                NextActionTile = TileType.None;
                NextActionSlot = -1;

                StartPlayState(PlayState.HandEnd);
            }
            else if (NextAction == GameAction.Nothing)
            {
                if (Player1Hand.IsInReach() && Player2Hand.IsInReach() && Player3Hand.IsInReach() && Player4Hand.IsInReach())
                {
                    // Four reaches! Draw game!
                    NextActionTile = TileType.None;
                    NextActionSlot = -1;
                    NextAction = GameAction.AbortiveDraw;
                    StartPlayState(PlayState.HandEnd);
                }
                else
                {
                    PrevAction = GameAction.Discard;
                    
                    fUpdateFuriten = true;

                    if (TilesRemaining == 0)
                    {
                        NextAction = GameAction.Nothing;
                        StartPlayState(PlayState.HandEnd);
                    }
                    else
                    {
                        AdvancePlayState();
                    }
                }
            }
            else if (NextAction == GameAction.Chii ||
                     NextAction == GameAction.Pon ||
                     NextAction == GameAction.OpenKan)
            {
                // Clear all ippatsu flags.
                _IppatsuFlag1 = false;
                _IppatsuFlag2 = false;
                _IppatsuFlag3 = false;
                _IppatsuFlag4 = false;

                // Take action.
                if ((NextAction == GameAction.OpenKan) && (DoraCount == 5))
                {
                    // This is the 5th kan. Immediately abort.
                    NextActionTile = TileType.None;
                    NextActionSlot = -1;
                    NextActionPlayer = NextActionPlayer;
                    NextAction = GameAction.AbortiveDraw;
                    StartPlayState(PlayState.HandEnd);
                }
                else
                {
                    fUpdateFuriten = true;

                    // Make the winning player perform their stored call.
                    CallOption co = null;
                    if (NextActionPlayer == Player.Player1) { co = Player1Hand.StoredCallOption; Player1Hand.PerformStoredCall(); }
                    if (NextActionPlayer == Player.Player2) { co = Player2Hand.StoredCallOption; Player2Hand.PerformStoredCall(); }
                    if (NextActionPlayer == Player.Player3) { co = Player3Hand.StoredCallOption; Player3Hand.PerformStoredCall(); }
                    if (NextActionPlayer == Player.Player4) { co = Player4Hand.StoredCallOption; Player4Hand.PerformStoredCall(); }

                    // Make the discarded tile have the Called flag set and also who called it.
                    var discards = GetDiscards(CurrentPlayer);
                    ExtendedTile targetTile = discards[discards.Count - 1];
                    targetTile.Called = true;
                    targetTile.Caller = NextActionPlayer;

                    // Set the given player as the current player.
                    Player prevPlayer = CurrentPlayer;
                    CurrentPlayer = NextActionPlayer;
                    Sink.HandPerformedStoredCall(CurrentPlayer, co);

                    // Set the game state to DecideMove for the player.
                    // Set PrevAction to the call type to show that we're discarding from a call.
                    // This has some effect such as what kinds of tiles we can discard and whether or
                    // not we expect to have a sideways tile IE picking from the wall.
                    PrevAction = NextAction; // Becomes GameAction.Chii/Pon/OpenKan

                    if (NextAction == GameAction.OpenKan)
                    {
                        PlayerRecentOpenKan = prevPlayer; // For Sekinin Barai
                        NextAction = GameAction.ReplacementTilePick;
                        StartPlayState(PlayState.PickTile);
                    }
                    else
                    {
                        StartPlayState(PlayState.DecideMove);
                    }
                }
            }
            else if (NextAction == GameAction.Ron)
            {
                // GameState has been set as to who is getting ronned on or about the tsumo. That game state will handle it.
                _NextActionPlayerTarget = CurrentPlayer;
                StartPlayState(PlayState.HandEnd);
            }
            else
            {
                RiichiGlobal.Assert(false);
            }

            // Set the furiten flags on all the players who didn't just move.
            if (fUpdateFuriten)
            {
                if (furitenDiscardingPlayer != Player.Player1) { Player1Hand.UpdateFuriten(furitenDiscardedTile); }
                if (furitenDiscardingPlayer != Player.Player2) { Player2Hand.UpdateFuriten(furitenDiscardedTile); }
                if (furitenDiscardingPlayer != Player.Player3) { Player3Hand.UpdateFuriten(furitenDiscardedTile); }
                if (furitenDiscardingPlayer != Player.Player4) { Player4Hand.UpdateFuriten(furitenDiscardedTile); }
            }

            // Clear the kanburi flag now. It's okay that we've already advanced state.
            // If a winner was found then kanburi has already been scored at this juncture.
            KanburiFlag = false;
        }

        public void ExecutePostBreak_HandEnd()
        {
            // Process Nagashi Mangan.
            if ((TilesRemaining == 0) && (NextAction == GameAction.Nothing))
            {
                bool player1Nagashi = Player1Hand.CheckNagashiMangan();
                bool player2Nagashi = Player2Hand.CheckNagashiMangan();
                bool player3Nagashi = Player3Hand.CheckNagashiMangan();
                bool player4Nagashi = Player4Hand.CheckNagashiMangan();

                int nagashiCount = (player1Nagashi ? 1 : 0) + (player2Nagashi ? 1 : 0) + (player3Nagashi ? 1 : 0) + (player4Nagashi ? 1 : 0);
                if (nagashiCount > 1)
                {
                    NextActionPlayer = Player.Multiple;
                    _NextAction1 = player1Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction2 = player2Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction3 = player3Nagashi ? GameAction.Tsumo : GameAction.Nothing;
                    _NextAction4 = player4Nagashi ? GameAction.Tsumo : GameAction.Nothing;

                    // Start at the player before the dealer so that the dealer gets resolved first.
                    _NextActionPlayerTarget = CurrentDealer.GetPrevious();
                }
                else if (nagashiCount == 1)
                {
                    if (player1Nagashi) { NextAction = GameAction.Tsumo; NextActionPlayer = Player.Player1; }
                    if (player2Nagashi) { NextAction = GameAction.Tsumo; NextActionPlayer = Player.Player2; }
                    if (player3Nagashi) { NextAction = GameAction.Tsumo; NextActionPlayer = Player.Player3; }
                    if (player4Nagashi) { NextAction = GameAction.Tsumo; NextActionPlayer = Player.Player4; }
                }
            }

            // Determine our state delta.
            if (NextActionPlayer == Player.Multiple)
            {
                Player processPlayer = _NextActionPlayerTarget;
                for (int i = 0; i < 4; ++i, processPlayer = processPlayer.GetNext())
                {
                    GameAction action = GetNextPlayerAction(processPlayer);
                    Player target = (action == GameAction.Tsumo) ? Player.Multiple :
                                    (action == GameAction.Ron)   ? _NextActionPlayerTarget :
                                                                   Player.None;

                    _MultiWinResults[i].Populate(this, processPlayer, target, PlayerRecentOpenKan, action, ConsumePool());
                }
            }
            else if (NextAction.IsAgari())
            {
                Player target = (NextAction == GameAction.Ron) ? _NextActionPlayerTarget : Player.Multiple;
                _WinResultCache.Populate(this, NextActionPlayer, target, PlayerRecentOpenKan, NextAction, ConsumePool());

                // Clear other player's streak.
                if (NextActionPlayer != Player.Player1) { Player1Hand.Streak = 0; }
                if (NextActionPlayer != Player.Player2) { Player2Hand.Streak = 0; }
                if (NextActionPlayer != Player.Player3) { Player3Hand.Streak = 0; }
                if (NextActionPlayer != Player.Player4) { Player4Hand.Streak = 0; }
            }
            else if (NextAction == GameAction.Nothing)
            {
                // Draw game! Determine score outputs. No warame in this situation because the math doesn't work.
                bool fP1Tempai = Player1Hand.IsTempai();
                bool fP2Tempai = Player2Hand.IsTempai();
                bool fP3Tempai = Player3Hand.IsTempai();
                bool fP4Tempai = Player4Hand.IsTempai();
                int tCount = (fP1Tempai ? 1 : 0) + (fP2Tempai ? 1 : 0) + (fP3Tempai ? 1 : 0) + (fP4Tempai ? 1 : 0);

                if ((tCount == 1) || (tCount == 2) || (tCount == 3))
                {
                    int deltaGainPoints = 3000 / tCount;
                    int deltaLosePoints = -(3000 / (4 - tCount));

                    _WinResultCache.Player1Delta = (fP1Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player2Delta = (fP2Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player3Delta = (fP3Tempai ? deltaGainPoints : deltaLosePoints);
                    _WinResultCache.Player4Delta = (fP4Tempai ? deltaGainPoints : deltaLosePoints);
                }
            }

            // Apply deltas to the game state.
            if (NextActionPlayer == Player.Multiple)
            {
                for (int i = 0; i < 4; ++i)
                {
                    Player1Hand.Score += _MultiWinResults[i].Player1Delta + _MultiWinResults[i].Player1PoolDelta;
                    Player2Hand.Score += _MultiWinResults[i].Player2Delta + _MultiWinResults[i].Player2PoolDelta;
                    Player3Hand.Score += _MultiWinResults[i].Player3Delta + _MultiWinResults[i].Player3PoolDelta;
                    Player4Hand.Score += _MultiWinResults[i].Player4Delta + _MultiWinResults[i].Player4PoolDelta;
                }
            }
            else
            {
                Player1Hand.Score += _WinResultCache.Player1Delta + _WinResultCache.Player1PoolDelta;
                Player2Hand.Score += _WinResultCache.Player2Delta + _WinResultCache.Player2PoolDelta;
                Player3Hand.Score += _WinResultCache.Player3Delta + _WinResultCache.Player3PoolDelta;
                Player4Hand.Score += _WinResultCache.Player4Delta + _WinResultCache.Player4PoolDelta;
            }

            // Save.
            Sink.PerformSave();

            // Notify the sink.
            if (NextActionPlayer == Player.Multiple)
            {
                Sink.MultiWin(_MultiWinResults[0], _MultiWinResults[1], _MultiWinResults[2], _MultiWinResults[3]);
            }
            else if (NextAction == GameAction.Tsumo)
            {
                Sink.HandPerformedTsumo(NextActionPlayer, _WinResultCache);
            }
            else if (NextAction == GameAction.Ron)
            {
                RiichiGlobal.Assert(_NextActionPlayerTarget != NextActionPlayer); // NextActionPlayer rons on _NextActionPlayerTarget.
                Sink.HandPerformedRon(NextActionPlayer, _WinResultCache);
            }
            else if (NextAction == GameAction.AbortiveDraw)
            {
                Sink.HandPerformedAbortiveDraw(NextActionPlayer, NextActionTile, NextActionSlot);
            }
            else
            {
                RiichiGlobal.Assert(NextAction == GameAction.Nothing);
                Sink.ExhaustiveDraw(_WinResultCache);
            }
        }

        public void ExecutePostBreak_TableCleanup()
        {
            // Clean up the wall, discards, hands, etc.
            Player1Hand.Clear();
            Player2Hand.Clear();
            Player3Hand.Clear();
            Player4Hand.Clear();
            _Player1Discards.Clear();
            _Player2Discards.Clear();
            _Player3Discards.Clear();
            _Player4Discards.Clear();
            DiscardPlayerList.Clear();

            for (int i = 0; i < Wall.Length; ++i)              { Wall[i] = TileType.None; }
            for (int i = 0; i < DoraIndicators.Length; ++i)    { DoraIndicators[i] = TileType.None; }
            for (int i = 0; i < UraDoraIndicators.Length; ++i) { UraDoraIndicators[i] = TileType.None; }

            // Evaluate if the game is done here.
            // If the game is done, then we goto PlayState.GameEnd. Otherwise we'll advance to the next round for cleanup.
            int dealerPoints = GetHand(CurrentDealer).Score;
            int checkScore = Settings.GetSetting<int>(GameOption.VictoryPoints);
            Round endRound = Settings.GetSetting<bool>(GameOption.Tonpussen) ? Round.East4 : Round.South4;
            bool knockedOut = Settings.GetSetting<bool>(GameOption.Buttobi) && ((Player1Hand.Score < 0) || (Player2Hand.Score < 0) || (Player3Hand.Score < 0) || (Player4Hand.Score < 0));
            bool dealerInLead = ((CurrentDealer == Player.Player1) || (dealerPoints > Player1Hand.Score)) &&
                                ((CurrentDealer == Player.Player2) || (dealerPoints > Player2Hand.Score)) &&
                                ((CurrentDealer == Player.Player3) || (dealerPoints > Player3Hand.Score)) &&
                                ((CurrentDealer == Player.Player4) || (dealerPoints > Player4Hand.Score));
            bool dealerWon = ((NextActionPlayer == Player.Multiple) && GetNextPlayerAction(CurrentDealer).IsAgari()) ||
                             ((NextActionPlayer == CurrentDealer) && NextAction.IsAgari());
            bool endGameRenchan = (CurrentRound == endRound) &&
                                   dealerWon &&
                                   (!dealerInLead || !Settings.GetSetting<bool>(GameOption.EndgameDealerFinish));

            if (knockedOut
                ||
                ((CurrentRound >= endRound) || CurrentRoundLapped) &&
                 !endGameRenchan &&
                 ((Player1Hand.Score >= checkScore) || (Player2Hand.Score >= checkScore) || (Player3Hand.Score >= checkScore) || (Player4Hand.Score >= checkScore)))
            {
                StartPlayState(PlayState.GameEnd);
            }
            else
            {
                // Clear a bunch of game state elements and advance it.
                PlayerRecentOpenKan = Player.None;
                TileColor = TileColor.GetNextColor();

                bool advanceHomba;
                bool advanceDealer;
                if (NextActionPlayer == Player.Multiple)
                {
                    GameAction dealerAction = GetNextPlayerAction(CurrentDealer);
                    advanceHomba = dealerAction.IsAgari();
                    advanceDealer = !advanceHomba;
                }
                else if (NextAction.IsAgari())
                {
                    advanceHomba = NextActionPlayer == CurrentDealer;
                    advanceDealer = !advanceHomba;
                }
                else if (NextAction == GameAction.AbortiveDraw)
                {
                    advanceHomba = true;
                    advanceDealer = false;
                }
                else // Draw
                {
                    advanceHomba = true;
                    advanceDealer = !GetHand(CurrentDealer).IsTempai();

                    if (Settings.GetSetting<bool>(GameOption.SouthNotReady))
                    {
                        Player south = CurrentDealer.GetNext();
                        advanceDealer &= GetHand(south).IsTempai();
                    }
                }

                Bonus = advanceHomba ? (Bonus + 1) : 0;

                if (Settings.GetSetting<bool>(GameOption.EightWinRetire) && !advanceDealer && (Bonus >= 8))
                {
                    advanceDealer = true;
                    Bonus = 0;
                }

                if (advanceDealer)
                {
                    CurrentDealer = CurrentDealer.GetNext();
                    CurrentRound = CurrentRound.GetNext();
                    if (CurrentRound == Round.East1)
                    {
                        CurrentRoundLapped = true;
                    }
                }

                // Reset state.
                CurrentPlayer = CurrentDealer;
                PrevAction = GameAction.Nothing;
                NextAction = GameAction.Nothing;
                NextActionTile = TileType.None;
                NextActionSlot = 0;
                NextActionPlayer = Player.None;
                _NextAction1 = GameAction.Nothing;
                _NextAction2 = GameAction.Nothing;
                _NextAction3 = GameAction.Nothing;
                _NextAction4 = GameAction.Nothing;
                _NextActionPlayerTarget = Player.None;
                Player1Hand.Furiten = false;
                Player2Hand.Furiten = false;
                Player3Hand.Furiten = false;
                Player4Hand.Furiten = false;
                _IppatsuFlag1 = false;
                _IppatsuFlag2 = false;
                _IppatsuFlag3 = false;
                _IppatsuFlag4 = false;
                PlayerDeadWallPick = false;

                Sink.TableCleanUpForNextRound();

                // Start the next state.
                StartPlayState(PlayState.RandomizingBreak);
            }
        }

        public void ExecutePostBreak_KanChosenTile()
        {
            if ((NextAction == GameAction.ClosedKan) && Settings.GetSetting<bool>(GameOption.RinshanIppatsu))
            {
                // Reset all ippatsu flags except for the current player.
                if      (CurrentPlayer != Player.Player1) { _IppatsuFlag1 = false; }
                else if (CurrentPlayer != Player.Player2) { _IppatsuFlag2 = false; }
                else if (CurrentPlayer != Player.Player3) { _IppatsuFlag3 = false; }
                else if (CurrentPlayer != Player.Player4) { _IppatsuFlag4 = false; }
            }
            else
            {
                // Reset all ippatsu flags.
                _IppatsuFlag1 = false;
                _IppatsuFlag2 = false;
                _IppatsuFlag3 = false;
                _IppatsuFlag4 = false;
            }

            // Notify the sink that the kan occured.
            Sink.HandPerformedKan(CurrentPlayer, NextActionTile, ((NextAction == GameAction.ClosedKan) ? KanType.Concealed : KanType.Promoted));

            if (DoraCount == 5)
            {
                // This is the 5th kan. Immediately abort.
                NextAction = GameAction.AbortiveDraw;
                NextActionPlayer = CurrentPlayer;
                StartPlayState(PlayState.HandEnd);
            }
            else
            {
                // NextAction is PromotedKan or ClosedKan.
                RiichiGlobal.Assert((NextAction == GameAction.PromotedKan) || (NextAction == GameAction.ClosedKan));

                // We will use this to see if we can ron on the chankan in the case of a promoted can or ron
                // with a kokushi in the case of a closed kan. Set PrevAction to the type of kan.
                PrevAction = NextAction;

                _NextAction1 = (CurrentPlayer == Player.Player1) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction2 = (CurrentPlayer == Player.Player2) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction3 = (CurrentPlayer == Player.Player3) ? GameAction.Nothing : GameAction.DecisionPending;
                _NextAction4 = (CurrentPlayer == Player.Player4) ? GameAction.Nothing : GameAction.DecisionPending;
                NextActionPlayer = CurrentPlayer;

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
                RiichiGlobal.Assert(false);
            }
        }

        public void ExecutePostBreak_GameEnd()
        {
            Sink.PerformSave();

            // Determine first dealer.
            int roundOffset = CurrentRound.GetOffset();
            Player startDealer = CurrentDealer.AddOffset(roundOffset);

            // Give the remaining reach sticks to the winner.
            int extraWinnerPoints = Settings.GetSetting<bool>(GameOption.WinnerGetsPool) ? ConsumePool() : 0;

            // Generate a GameResults and submit it to GameComplete.
            GameResults gameResults = new GameResults(Settings,
                                                      startDealer,
                                                      Player1Hand.Score,
                                                      Player2Hand.Score,
                                                      Player3Hand.Score,
                                                      Player4Hand.Score,
                                                      extraWinnerPoints,
                                                      Player1Hand.Yakitori,
                                                      Player2Hand.Yakitori,
                                                      Player3Hand.Yakitori,
                                                      Player4Hand.Yakitori);
            Sink.GameComplete(gameResults);
        }

        public void ExecuteRewindModeChange_HandEnd()         { PerformRoundEndRewindStep(); }
        public void ExecuteRewindModeChange_TableCleanup()    { PerformRoundEndRewindStep(); }
        public void ExecuteRewindModeChange_GameEnd()         { PerformRoundEndRewindStep(); }
        public void ExecuteRewindModeChange_GatherDecisions() { CurrentState = PlayState.DecideMove; }
        public void ExecuteRewindModeChange_KanChosenTile()   { CurrentState = PlayState.DecideMove; }
        public void ExecuteRewindModeChange_NextTurn()        { CurrentState = PlayState.GatherDecisions; }
        public void ExecuteRewindModeChange_PerformDecision() { CurrentState = PlayState.NextTurn; }
        public void ExecuteRewindModeChange_PickTile()        { CurrentState = PlayState.NextTurn; }

        private void PerformRoundEndRewindStep()
        {
            // If we're done with this round, rewind us before the previous ron or tsumo.
            if      (NextAction == GameAction.Ron) { CurrentState = PlayState.GatherDecisions; }
            else if (NextAction == GameAction.Tsumo)
            {
                CurrentState = PlayState.DecideMove;
                Sink.TsumoUndone(CurrentPlayer);
            }
            else
            {
                RiichiGlobal.Assert("Unexpected state for rewinding!");
            }
        }

        public void ExecuteRewindModeChange_DecideMove()
        {
            RiichiGlobal.Assert((_RewindAction != GameAction.Nothing), "Expected _RewindAction to not be Nothing, but it's: " + _RewindAction);
            CurrentState = (_RewindAction == GameAction.OpenKan) || // RewindPlayerDrawOrCall.RewindAction....
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
                Meld meld = GetHand(CurrentPlayer).GetLatestMeld();
                _RewindAction = (meld.State == MeldState.KanOpen) ? GameAction.OpenKan :
                                (meld.State == MeldState.Chii)    ? GameAction.Chii :
                                                                    GameAction.Pon; 
            }
            else
            {
                _RewindAction = GetHand(CurrentPlayer).PeekLastDrawKanType();
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
            CurrentPlayer = CurrentPlayer.GetPrevious();
        }

        public void ExecuteRewindPostModeChange_PickTile()
        {
            // Remove the last drawn tile from the current player's hand.
            Hand hand = GetHand(CurrentPlayer);
            TileCommand tc = hand.PeekLastDrawKan();

            RiichiGlobal.Log("draw to rewind: " + tc.TilePrimary.Tile + " reaminig drawnskan count " + hand.DrawsAndKans.Count);
            RiichiGlobal.Assert(tc.CommandType == TileCommand.Type.Tile, "RewindPMC_PickTile, expected tile, found: " + tc.CommandType);

            bool succeeded = hand.RewindAddTile(tc.TilePrimary.Tile);
            RiichiGlobal.Assert(succeeded, "Failed to rewind add tile!! Tile: " + tc.TilePrimary.Tile);

            // Increase the number of tiles remaining. Ensure that the tiles match.
            TilesRemaining++;
            int slot = TileHelpers.ClampTile(Offset + (122 - TilesRemaining));
            TileType wallTile = Wall[slot];
            RiichiGlobal.Assert(wallTile.IsEqual(tc.TilePrimary.Tile), "Tile that was rewinded isn't the same tile from the wall! rewind tile: " + tc.TilePrimary.Tile + " wall tile: " + wallTile + " at slot: " + slot);

            // Notify the sink that this happened.
            Sink.DrawUndone(CurrentPlayer, tc.TilePrimary.Tile);
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
            if (flipDora)
            {
                ++DoraCount;
                Sink.DoraTileFlipped();
            }
        }

        private int PickIntoPlayerHand(Player p, TileSource source)
        {
            // Get the tile from the wall to pick from.
            RiichiGlobal.Assert(source != TileSource.Call);
            int tileNumber;
            if (source == TileSource.ReplacementTileDraw)
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
            RiichiGlobal.Log("PickIntoPlayerHand! Player: " + p + " Slot: " + tileNumber + " tile: " + Wall[tileNumber]);

            GetHand(p).AddTile(Wall[tileNumber]);
            return tileNumber;
        }

        public Hand GetHandZeroIndexed(int slot)
        {
            RiichiGlobal.Assert((slot >= 0) && (slot <= 3));
            return (slot == 0) ? Player1Hand :
                   (slot == 1) ? Player2Hand :
                   (slot == 2) ? Player3Hand :
                                 Player4Hand;
        }

        public List<ExtendedTile> GetDiscardsZeroIndexed(int slot)
        {
            RiichiGlobal.Assert((slot >= 0) && (slot <= 3));
            return (slot == 0) ? _Player1Discards :
                   (slot == 1) ? _Player2Discards :
                   (slot == 2) ? _Player3Discards :
                                 _Player4Discards;
        }

        private GameAction GetNextPlayerActionZeroIndexed(int slot)
        {
            RiichiGlobal.Assert((slot >= 0) && (slot <= 3));
            return (slot == 0) ? _NextAction1 :
                   (slot == 1) ? _NextAction2 :
                   (slot == 2) ? _NextAction3 :
                                 _NextAction4;
        }

        public bool IsInReach(Player p)
        {
            List<ExtendedTile> discards = GetDiscards(p);
            bool inReach = false;
            foreach (ExtendedTile et in discards)
            {
                if (et.Reach || et.OpenReach)
                {
                    inReach = true;
                    break;
                }
            }
            return inReach;
        }

        public bool IsInOpenReach(Player p)
        {
            List<ExtendedTile> discards = GetDiscards(p);
            bool inOpenReach = false;
            foreach (ExtendedTile et in discards)
            {
                if (et.OpenReach)
                {
                    inOpenReach = true;
                    break;
                }
            }
            return inOpenReach;
        }

        public bool IsInDoubleReach(Player p)
        {
            List<ExtendedTile> discards = GetDiscards(p);
            bool isDoubleReach = false;
            if ((discards.Count >= 1) && discards[0].Reach)
            {
                isDoubleReach = true;

                // Check the players who went before the player we are looking at. If any of their first tiles got called on then we can't cant double reach.
                for (Player playerCheck = CurrentDealer; playerCheck != p; playerCheck = playerCheck.GetNext())
                {
                    List<ExtendedTile> pDiscards = GetDiscards(p);
                    if ((pDiscards.Count > 0) && pDiscards[0].Called)
                    {
                        isDoubleReach = false;
                        break;
                    }
                }
            }
            return isDoubleReach;
        }

        public bool GetIppatsu(Player p)
        {
            return (p == Player.Player1) ? _IppatsuFlag1 :
                   (p == Player.Player2) ? _IppatsuFlag2 :
                   (p == Player.Player3) ? _IppatsuFlag3 :
                                           _IppatsuFlag4; 
        }

        private void GetDoraIndicators()
        {
            DoraIndicators[0] = Wall[TileHelpers.ClampTile(Offset - 6)];
            DoraIndicators[1] = Wall[TileHelpers.ClampTile(Offset - 8)];
            DoraIndicators[2] = Wall[TileHelpers.ClampTile(Offset - 10)];
            DoraIndicators[3] = Wall[TileHelpers.ClampTile(Offset - 12)];
            DoraIndicators[4] = Wall[TileHelpers.ClampTile(Offset - 14)];
            UraDoraIndicators[0] = Wall[TileHelpers.ClampTile(Offset - 5)];
            UraDoraIndicators[1] = Wall[TileHelpers.ClampTile(Offset - 7)];
            UraDoraIndicators[2] = Wall[TileHelpers.ClampTile(Offset - 9)];
            UraDoraIndicators[3] = Wall[TileHelpers.ClampTile(Offset - 11)];
            UraDoraIndicators[4] = Wall[TileHelpers.ClampTile(Offset - 13)];
        }

        public void HandPerformedTsumo(Player p)
        {
            NextAction = GameAction.Tsumo;
            NextActionPlayer = p;
            StartPlayState(PlayState.HandEnd);
        }

        public void HandPerformedKan(Player p, int handSlot, TileType tile, KanType type)
        {
            RiichiGlobal.Assert(p == CurrentPlayer);

            NextActionTile = tile;
            NextActionSlot = handSlot;
            NextAction = (type == KanType.Promoted) ? GameAction.PromotedKan : GameAction.ClosedKan;

            // Advance the game state.
            StartPlayState(PlayState.KanChosenTile);
        }

        private void PerformDiscardState(ExtendedTile et, int slot, GameAction nextAction)
        {
            NextAction = nextAction;
            NextActionTile = et.Tile;
            NextActionSlot = slot;

            GetDiscards(CurrentPlayer).Add(et);
            PlayerRecentOpenKan = Player.None;
            DiscardPlayerList.Push(CurrentPlayer);

            // Reset appropriate ippatsu flags.
            if      (CurrentPlayer == Player.Player1) { _IppatsuFlag1 = false; }
            else if (CurrentPlayer == Player.Player2) { _IppatsuFlag2 = false; }
            else if (CurrentPlayer == Player.Player3) { _IppatsuFlag3 = false; }
            else if (CurrentPlayer == Player.Player4) { _IppatsuFlag4 = false; }
        }

        public void HandPerformedDiscard(TileType tile, int handSlot)
        {
            PerformDiscardState(new ExtendedTile(tile), handSlot, GameAction.Discard);

            // Notify the sink that the discard occured.
            Sink.HandPerformedDiscard(CurrentPlayer, tile, handSlot);

            // Advance the game state.
            StartPlayState(PlayState.GatherDecisions);
        }

        public void HandPerformedReach(TileType tile, int handSlot, bool fOpenReach)
        {
            PerformDiscardState(new ExtendedTile(tile, fOpenReach), handSlot, (fOpenReach ? GameAction.OpenRiichiDiscard : GameAction.RiichiDiscard));
            Hand hand = GetHand(CurrentPlayer);
            if (Settings.GetSetting<bool>(GameOption.Buttobi))
            {
                RiichiGlobal.Assert((hand.Score >= 1000), "Tried to reach with less than 1000 points!");
            }
            hand.Score -= 1000;

            if      (CurrentPlayer == Player.Player1) { _IppatsuFlag1 = true; }
            else if (CurrentPlayer == Player.Player2) { _IppatsuFlag2 = true; }
            else if (CurrentPlayer == Player.Player3) { _IppatsuFlag3 = true; }
            else if (CurrentPlayer == Player.Player4) { _IppatsuFlag4 = true; }

            // Notify the sink that the reach occured.
            Sink.HandPerformedReach(CurrentPlayer, tile, handSlot, fOpenReach);

            // Advance the game state.
            StartPlayState(PlayState.GatherDecisions);
        }

        public void HandPerformedAbortiveDraw(Player p, TileType tile, int handSlot)
        {
            NextActionPlayer = p;
            NextActionTile = tile;
            NextActionSlot = handSlot;
            NextAction = GameAction.AbortiveDraw;

            StartPlayState(PlayState.HandEnd);
        }

        public void HandCleared(Player p)                            { Sink.HandCleared(p); }
        public void HandSort(Player p, bool fAnimation)              { Sink.HandSort(p, fAnimation); }
        public void HandPerformedStoredCall(Player p, CallOption co) { Sink.HandPerformedStoredCall(p, co); }
        public Hand GetHand(Player p)                                { return GetHandZeroIndexed(p.GetZeroIndex()); }
        public List<ExtendedTile> GetDiscards(Player p)              { return GetDiscardsZeroIndexed(p.GetZeroIndex()); }
        private GameAction GetNextPlayerAction(Player p)             { return GetNextPlayerActionZeroIndexed(p.GetZeroIndex()); }
    }
}
