﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
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
            public static PlayState GetNext(this PlayState s)                 { return EnumAttributes.HasAttributeValue(s, typeof(NextState)) ? EnumAttributes.GetAttributeValue<NextState, PlayState>(s) : PlayState.NA; }
            public static int  GetSkyValue(this PlayState ps)                 { return EnumAttributes.GetAttributeValue<SkyValue, int>(ps); }
            public static bool TryGetPlayState(string text, out PlayState ps) { return EnumHelper.TryGetEnumByCode<PlayState, SkyValue>(text, out ps); }

            public static PlayState GetPlayState(string text)
            {
                CommonHelpers.Check(EnumHelper.TryGetEnumByCode<PlayState, SkyValue>(text, out PlayState ps), ("Could not parse into PlayState: " + text));
                return ps;
            }
        }
    #endregion

    #region GameAction
        public enum GameAction
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
            [SkyValue(15), WinValue(WinType.None)]  Chombo,
        }

        public static class GameActionExtentionMethods
        {
            public static WinType GetWinType(this GameAction ga)                   { return EnumAttributes.GetAttributeValue<WinValue, WinType>(ga); }
            public static int     GetSkyValue(this GameAction ga)                  { return EnumAttributes.GetAttributeValue<SkyValue, int>(ga); }
            public static bool    TryGetGameAction(string text, out GameAction ga) { return EnumHelper.TryGetEnumByCode<GameAction, SkyValue>(text, out ga); }
            public static bool    IsAgari(this GameAction ga)                      { return (ga == GameAction.Ron) || (ga == GameAction.Tsumo); }
            public static bool    IsOpenCall(this GameAction ga)                   { return (ga == GameAction.Chii) || (ga == GameAction.Pon) || (ga == GameAction.OpenKan); }
            public static bool    IsKan(this GameAction ga)                        { return (ga == GameAction.ClosedKan) || (ga == GameAction.PromotedKan) || (ga == GameAction.OpenKan); }

            public static GameAction GetGameAction(string text)
            {
                CommonHelpers.Check(EnumHelper.TryGetEnumByCode<GameAction, SkyValue>(text, out GameAction ga), ("Failed to parse GameAction: " + text));
                return ga;
            }
        }
    #endregion

    public interface IBoardTemplate
    {
        IReadOnlyList<ITile> Wall         { get; }
        IReadOnlyList<ITile> Hand1        { get; }
        IReadOnlyList<ITile> Hand2        { get; }
        IReadOnlyList<ITile> Hand3        { get; }
        IReadOnlyList<ITile> Hand4        { get; }
        IReadOnlyList<ITile> Discards1    { get; }
        IReadOnlyList<ITile> Discards2    { get; }
        IReadOnlyList<ITile> Discards3    { get; }
        IReadOnlyList<ITile> Discards4    { get; }
        IReadOnlyList<IMeld> Melds        { get; }
        int                  DiscardCount { get; }

        IReadOnlyList<ITile> GetDiscards(Player player);
        IReadOnlyList<ITile> GetHand(Player player);
    }

    public enum OverrideState
    {
        [OptionValueType(typeof(Round))]          Round,
        [OptionValueType(typeof(int))]            Pool,
        [OptionValueType(typeof(int))]            Bonus,
        [OptionValueType(typeof(Player))]         Dealer,
        [OptionValueType(typeof(Player))]         Current,
        [OptionValueType(typeof(Player))]         FirstDealer,
        [OptionValueType(typeof(Player))]         Wareme,
        [OptionValueType(typeof(bool))]           Lapped,
        [OptionValueType(typeof(ITile))]          WallTile,
        [OptionValueType(typeof(int))]            DoraCount,
        [OptionValueType(typeof(int))]            Roll,
        [OptionValueType(typeof(IBoardTemplate))] Board,
    }

    public interface IGameState : IComparable<IGameState>, ICloneable
    {
        event Action<Player,int>               WallPicking;
        event Action<ITile[], TileSource>      WallPicked;
        event Action<Player, ITile>            WallPickUndone;
        event Action<IWinResult>               Ron;
        event Action<IWinResult>               Tsumo;
        event Action<Player>                   WinUndone;
        event Action<IDiscardInfo>             DiscardRequested;
        event Action<IPostDiscardInfo>         PostDiscardRequested;
        event Action<IPostDiscardInfo>         PostKanRequested;
        event Action<IWinResult[]>             MultiWin;
        event Action<IWinResult>               ExhaustiveDraw;
        event Action<Player, AbortiveDrawType> AbortiveDraw;
        event Action<IGameResult>              GameComplete;
        event Action<Player>                   Chombo;
        event Action                           DiceRolled;
        event Action                           DeadWallMoved;
        event Action<ITile>                    DoraIndicatorFlipped;
        event Action                           PreCheckAdvance;
        event Action                           TableCleared;
        event Action                           PreCheckRewind;
        event Action<Player, IMeld>            DecisionCancelled; // If Meld is null, it was a ron that was head bumped.

        ITile[]        Wall               { get; }
        ITile[]        DoraIndicators     { get; }
        ITile[]        UraDoraIndicators  { get; }
        IGameSettings  Settings           { get; }
        IExtraSettings ExtraSettings      { get; }
        IHand          Player1Hand        { get; }
        IHand          Player2Hand        { get; }
        IHand          Player3Hand        { get; }
        IHand          Player4Hand        { get; }
        IPlayerAI      Player1AI          { get; set; }
        IPlayerAI      Player2AI          { get; set; }
        IPlayerAI      Player3AI          { get; set; }
        IPlayerAI      Player4AI          { get; set; }
        Round          Round              { get; }
        Player         FirstDealer        { get; }
        Player         Dealer             { get; }
        Player         Current            { get; }
        Player         Wareme             { get; }
        PlayState      State              { get; }
        GameAction     NextAction         { get; }
        GameAction     PreviousAction     { get; }
        bool           Lapped             { get; }
        int            Offset             { get; }
        int            TilesRemaining     { get; }
        int            Bonus              { get; }
        int            Pool               { get; }
        int            DoraCount          { get; }
        int            Roll               { get; }

        void       Start();
        void       Advance(); // TODO: RIP?
        void       Rewind();
        void       Pause();   // Can only be called in response to PreCheckAdvance/PreCheckRewind event.
        void       Resume();  // Can only be called if Pause was called successfully OR loading from a SaveState.
        ISaveState Save();
        void       SubmitDiscard(IDiscardDecision decision);
        void       SubmitPostDiscard(IPostDiscardDecision decision);
        void       SubmitResultCommand(IResultCommand command);
        void       SubmitOverride(OverrideState key, object value);
    }

    public static class GameStateFactory
    {
        public static IGameState CreateNewGame()                                 { return new GameStateImpl(new GameSettingsImpl()); }
        public static IGameState CreateNewGame(IGameSettings settings)           { return new GameStateImpl(settings); }
        public static IGameState CreateNewGame(IExtraSettings extra)             { return new GameStateImpl(extra); }
        public static IGameState LoadGame(ISaveState save)                       { return new GameStateImpl(save); }
        public static IGameState LoadGame(ISaveState save, IExtraSettings extra) { return new GameStateImpl(save, extra); }
    }

    public interface ISaveState : IComparable<ISaveState>
    {
        IGameSettings               Settings       { get; }
        IDictionary<string, string> Tags           { get; }
        Round                       Round          { get; }
        bool                        Lapped         { get; }
        int                         Player1Score   { get; }
        int                         Player2Score   { get; }
        int                         Player3Score   { get; }
        int                         Player4Score   { get; }
        int                         TilesRemaining { get; }

        string     Marshal();
        ISaveState Clone();
    }

    public static class SaveStateFactory
    {
        public static ISaveState Unmarshal(string state) { return new SaveStateImpl(state); }
    }

    public static class BoardTemplateFactory
    {
        public static IBoardTemplate BuildTemplate(
            IReadOnlyList<ITile> wall,
            IReadOnlyList<ITile> hand1,
            IReadOnlyList<ITile> hand2,
            IReadOnlyList<ITile> hand3,
            IReadOnlyList<ITile> hand4,
            IReadOnlyList<ITile> discards1,
            IReadOnlyList<ITile> discards2,
            IReadOnlyList<ITile> discards3,
            IReadOnlyList<ITile> discards4,
            IReadOnlyList<IMeld> melds,
            int discardCount)
        {
            return new BoardTemplateImpl(wall, hand1, hand2, hand3, hand4, discards1, discards2, discards3, discards4, melds, discardCount);
        }
    }
}
