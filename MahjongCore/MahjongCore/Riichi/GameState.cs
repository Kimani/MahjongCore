// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

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
        }
    #endregion

    public abstract class HandSortedEventArgs      : EventArgs { public abstract Player           Player  { get; } }
    public abstract class HandPickingTileArgs      : EventArgs { public abstract Player           Player  { get; }
                                                                 public abstract int              Count   { get; } }
    public abstract class HandTileAddedArgs        : EventArgs { public abstract ITile[]          Tiles   { get; }
                                                                 public abstract TileSource       Source  { get; } }
    public abstract class HandDiscardArgs          : EventArgs { public abstract ITile            Tile    { get; } }
    public abstract class HandReachArgs            : EventArgs { public abstract ITile            Tile    { get; } }
    public abstract class HandKanArgs              : EventArgs { public abstract IMeld            Meld    { get; } }
    public abstract class HandCallArgs             : EventArgs { public abstract IMeld            Meld    { get; } }
    public abstract class WallTilesPicked          : EventArgs { public abstract ITile[]          Tiles   { get; } }
    public abstract class DiscardRequestedArgs     : EventArgs { public abstract IDiscardInfo     Info    { get; } }
    public abstract class PostDiscardRequstedArgs  : EventArgs { public abstract IPostDiscardInfo Info    { get; } }
    public abstract class HandRonArgs              : EventArgs { public abstract Player           Player  { get; }
                                                                 public abstract IWinResults      Results { get; } }
    public abstract class HandTsumoArgs            : EventArgs { public abstract Player           Player  { get; }
                                                                 public abstract IWinResults      Results { get; } }
    public abstract class MultiWinArgs             : EventArgs { public abstract IWinResults[]    Results { get; } }
    public abstract class ExhaustiveDrawArgs       : EventArgs { public abstract IWinResults      Results { get; } }
    public abstract class AbortiveDrawArgs         : EventArgs { public abstract AbortiveDrawType Type    { get; }
                                                                 public abstract ITile            Tile    { get; } } // Tile might be null if not applicable.
    public abstract class GameCompleteArgs         : EventArgs { public abstract IGameResults     Results { get; } }
    public abstract class DiscardUndoneArgs        : EventArgs { public abstract Player           Player  { get; }
                                                                 public abstract TileType         Tile    { get; } }
    public abstract class WinUndoneArgs            : EventArgs { public abstract Player           Player  { get; } }
    public abstract class TilePickUndoneArgs       : EventArgs { public abstract Player           Player  { get; }
                                                                 public abstract ITile            Tile    { get; } } // Will be the wall tile.
    public abstract class PlayerChomboArgs         : EventArgs { public abstract Player           Player  { get; } }
    public abstract class DoraIndicatorFlippedArgs : EventArgs { public abstract ITile            Tile    { get; } }

    public interface IGameState
    {
        event EventHandler<HandSortedEventArgs>      HandSorted;
        event EventHandler<HandPickingTileArgs>      HandPickingTile;
        event EventHandler<HandTileAddedArgs>        HandTileAdded;
        event EventHandler<HandDiscardArgs>          HandDiscard;
        event EventHandler<HandReachArgs>            HandReach;
        event EventHandler<HandKanArgs>              HandKan;
        event EventHandler<HandCallArgs>             HandCall;
        event EventHandler<HandRonArgs>              HandRon;
        event EventHandler<HandTsumoArgs>            HandTsumo;
        event EventHandler<DiscardRequestedArgs>     DiscardRequested;
        event EventHandler<PostDiscardRequstedArgs>  PostDiscardRequested;
        event EventHandler<MultiWinArgs>             MultiWin;
        event EventHandler<ExhaustiveDrawArgs>       ExhaustiveDraw;
        event EventHandler<AbortiveDrawArgs>         AbortiveDraw;
        event EventHandler<GameCompleteArgs>         GameComplete;
        event EventHandler<PlayerChomboArgs>         Chombo;
        event EventHandler<PostDiscardRequstedArgs>  PostKanRequested;
        event EventHandler<WallTilesPicked>          WallTilesPicked;
        event EventHandler                           DiceRolled;
        event EventHandler                           DeadWallMoved;
        event EventHandler<DoraIndicatorFlippedArgs> DoraIndicatorFlipped;
        event EventHandler                           PreCheckAdvance;
        event EventHandler                           TableCleared;
        event EventHandler                           PreCheckRewind;
        event EventHandler<DiscardUndoneArgs>        DiscardUndone;
        event EventHandler<WinUndoneArgs>            WinUndone;
        event EventHandler<TilePickUndoneArgs>       TilePickUndone;

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
        bool           Lapped             { get; }
        int            Offset             { get; }
        int            TilesRemaining     { get; }
        int            Bonus              { get; }
        int            Pool               { get; }
        int            DoraCount          { get; }
        int            Roll               { get; }

        void       Advance();
        void       Rewind();
        void       Pause();  // Can only be called in response to PreCheckAdvance/PreCheckRewind event.
        void       Resume(); // Can only be called if Pause was called successfully.
        ISaveState Save();
        void       SubmitDiscard(IDiscardDecision decision);
        void       SubmitPostDiscard(IPostDiscardDecision decision);
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

        string     Marshall();
        ISaveState Clone();
    }

    public static class SaveStateFactory
    {
        public static ISaveState Unmarshall(string state) { return new SaveStateImpl(state); }
    }
}
