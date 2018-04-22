// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.AI;
using MahjongCore.Riichi.Attributes;
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

            public static bool IsPlayer(this Player p)                 { return EnumAttributes.GetAttributeValue<IsSinglePlayer, bool>(p); }
            public static int  GetZeroIndex(this Player p)             { return EnumAttributes.GetAttributeValue<ZeroIndex, int>(p); }
            public static int  GetPlayerValue(this Player p)           { return EnumAttributes.GetAttributeValue<PlayerValue, int>(p); }
            public static bool TryGetPlayer(string text, out Player p) { return EnumHelper.TryGetEnumByCode<Player, PlayerValue>(text, out p); }

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

    public interface DiscardInfo
    {
        bool           InReach            { get; }
        bool           CanNormalDiscard   { get; }
        bool           CanKyuushuuKyuuhai { get; }
        bool           CanTsumo           { get; }
        bool           CanReach           { get; }
        bool           CanOpenReach       { get; }
        List<TileType> RestrictedTiles    { get; }
        TileType       SuufurendanTile    { get; }
        KanOptions     KanOptions         { get; }
    }

    public abstract class HandClearedEventArgs         : EventArgs { public abstract Player      Player      { get; } }
    public abstract class HandSortedEventArgs          : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract bool        InitialSort { get; } }
    public abstract class HandPickingTileArgs          : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract int         Count       { get; } }
    public abstract class HandTileAddedArgs            : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract int         Count       { get; } }
    public abstract class HandAbortiveDrawArgs         : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; }
                                                                     public abstract int         HandSlot    { get; } }
    public abstract class HandDiscardArgs              : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; }
                                                                     public abstract int         HandSlot    { get; } }
    public abstract class HandReachArgs                : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; }
                                                                     public abstract int         HandSlot    { get; }
                                                                     public abstract bool        OpenReach   { get; } }
    public abstract class HandKanArgs                  : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; }
                                                                     public abstract KanType     Type        { get; } }
    public abstract class HandCallArgs                 : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract CallOption  Call        { get; } }
    public abstract class HandRonArgs                  : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract IWinResults Results     { get; } }
    public abstract class HandTsumoArgs                : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract IWinResults Results     { get; } }
    public abstract class MultiWinArgs                 : EventArgs { public abstract IWinResults Win1        { get; }
                                                                     public abstract IWinResults Win2        { get; }
                                                                     public abstract IWinResults Win3        { get; }
                                                                     public abstract IWinResults Win4        { get; } }
    public abstract class ExhaustiveDrawArgs           : EventArgs { public abstract IWinResults Results     { get; } }
    public abstract class DiscardDecisionRequestedArgs : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract DiscardInfo Info        { get; } }
    public abstract class TilePickedArgs               : EventArgs { public abstract int[]       Slots       { get; }
                                                                     public abstract TileSource  Source      { get; } }
    public abstract class GameCompleteArgs             : EventArgs { public abstract GameResults Results     { get; } }
    public abstract class DiscardUndoneArgs            : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; } }
    public abstract class WinUndoneArgs                : EventArgs { public abstract Player      Player      { get; } }
    public abstract class TilePickUndoneArgs           : EventArgs { public abstract Player      Player      { get; }
                                                                     public abstract TileType    Tile        { get; } }

    public interface IGameState
    {
        event EventHandler<HandClearedEventArgs>         HandCleared;
        event EventHandler<HandSortedEventArgs>          HandSorted;
        event EventHandler<HandPickingTileArgs>          HandPickingTile;
        event EventHandler<HandTileAddedArgs>            HandTileAdded;
        event EventHandler<HandAbortiveDrawArgs>         HandAbortiveDraw;
        event EventHandler<HandDiscardArgs>              HandDiscard;
        event EventHandler<HandReachArgs>                HandReach;
        event EventHandler<HandKanArgs>                  HandKan;
        event EventHandler<HandCallArgs>                 HandCall;
        event EventHandler<HandRonArgs>                  HandRon;
        event EventHandler<HandTsumoArgs>                HandTsumo;
        event EventHandler<MultiWinArgs>                 MultiWin;
        event EventHandler<ExhaustiveDrawArgs>           ExhaustiveDraw;
        event EventHandler<DiscardDecisionRequestedArgs> DiscardDecisionRequested;
        event EventHandler                               PostDiscardDecisionsRequested;
        event EventHandler                               PostKanDecisionsRequested;
        event EventHandler                               DiceRolled;
        event EventHandler                               MoveDeadWall;
        event EventHandler                               DoraIndicatorFlipped;
        event EventHandler                               PreCheckAdvance;
        event EventHandler<TilePickedArgs>               TilePicked;
        event EventHandler<GameCompleteArgs>             GameComplete;
        event EventHandler                               TableCleared;
        event EventHandler                               PreCheckRewind;
        event EventHandler<DiscardUndoneArgs>            DiscardUndone;
        event EventHandler<WinUndoneArgs>                WinUndone;
        event EventHandler<TilePickUndoneArgs>           TilePickUndone;

        TileType[]       Wall               { get; }
        TileType[]       DoraIndicators     { get; }
        TileType[]       UraDoraIndicators  { get; }
        Round            CurrentRound       { get; }
        Player           CurrentDealer      { get; }
        Player           CurrentPlayer      { get; }
        Player           WaremePlayer       { get; }
        PlayState        CurrentState       { get; }
        GameSettings     Settings           { get; }
        TutorialSettings TutorialSettings   { get; }
        IHand            Player1Hand        { get; }
        IHand            Player2Hand        { get; }
        IHand            Player3Hand        { get; }
        IHand            Player4Hand        { get; }
        bool             CurrentRoundLapped { get; }
        int              Offset             { get; }
        int              TilesRemaining     { get; }
        int              Bonus              { get; }
        int              Pool               { get; }
        int              DoraCount          { get; }
        int              Roll               { get; }
        IPlayerAI        Player1AI          { get; set; }
        IPlayerAI        Player2AI          { get; set; }
        IPlayerAI        Player3AI          { get; set; }
        IPlayerAI        Player4AI          { get; set; }

        void       Advance();
        void       Rewind();
        void       Pause();  // Can only be called PreCheckAdvance/PreCheckRewind event.
        void       Resume(); // Can only be called if Pause was called successfully.
        ISaveState Save();
        void       SubmitDiscardDecision(IDiscardDecision decision);
        void       SubmitPostDiscardDecision(Player p, IPostDiscardDecision decision); // TODO: figure this out!?
    }

    public static class GameStateFactory
    {
        public static IGameState CreateNewGame()                                                   { return new GameStateImpl(new GameSettings()); }
        public static IGameState CreateNewGame(GameSettings customSettings)                        { return new GameStateImpl(customSettings); }
        public static IGameState CreateNewGame(TutorialSettings tutorialSettings)                  { return new GameStateImpl(tutorialSettings); }
        public static IGameState LoadGame(ISaveState saveState)                                    { return new GameStateImpl(saveState); }
        public static IGameState LoadGame(ISaveState saveState, TutorialSettings tutorialSettings) { return new GameStateImpl(saveState, tutorialSettings); }
    }

    public interface ISaveStatePlayer
    {
        string Name  { get; }
        int    Score { get; }
    }
    
    public interface ISaveState : IComparable<ISaveState>
    {
        GameSettings     Settings       { get; }
        ISaveStatePlayer Player1        { get; }
        ISaveStatePlayer Player2        { get; }
        ISaveStatePlayer Player3        { get; }
        ISaveStatePlayer Player4        { get; }
        Round            Round          { get; }
        IList<string>    Tags           { get; }
        int              TilesRemaining { get; }

        string     Marshall();
        ISaveState Clone();
    }

    public static class SaveStateFactory
    {
        public static ISaveState Unmarshall(string save) { return SaveStateImpl.LoadFromString(save); }
    }
}
