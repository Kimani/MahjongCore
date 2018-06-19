// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Impl;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public enum PostDiscardDecisionType
    {
        Nothing,
        Call,
        Ron
    }

    #region DiscardDecisionType
        public enum DiscardDecisionType
        {
            [IsDiscard(false)] Invalid,           // Should never fill out a Decision and leave DecisionToMake as this value.
            [IsDiscard(true)]  Discard,           // Tile should be the value of the tile being discarded.
            [IsDiscard(false)] Tsumo,             // Tile is ignored.
            [IsDiscard(false)] ClosedKan,         // Tile should be one of the tiles of tile of kan is being made.
            [IsDiscard(false)] PromotedKan,       // Tile should be the tile in the active hand that is being added.
            [IsDiscard(true)]  RiichiDiscard,     // Tile should be the value of the tile being discarded.
            [IsDiscard(true)]  OpenRiichiDiscard, // Tile should be the value of the tile being discarded.
            [IsDiscard(false)] AbortiveDraw       // Tile is ignored.
        }

        public static class DiscardDecisionTypeExtensionMethods
        {
            public static bool IsDiscard(this DiscardDecisionType d) { return EnumAttributes.GetAttributeValue<IsDiscard, bool>(d); }
        }
    #endregion

    public interface DiscardInfo
    {
        IHand           Hand               { get; }
        IList<TileType> PromotedKanTiles   { get; }
        IList<TileType> ClosedKanTiles     { get; }
        IList<TileType> RestrictedTiles    { get; }
        TileType        SuufurendanTile    { get; }
        TileSource      Source             { get; }
        bool            CanNormalDiscard   { get; }
        bool            CanKyuushuuKyuuhai { get; }
        bool            CanTsumo           { get; }
        bool            CanReach           { get; }
    }

    public interface PostDiscardInfo
    {
        IHand        Hand          { get; }
        IHand        TargetPlayer  { get; }
        IList<IMeld> Calls         { get; }
        ITile        DiscardedTile { get; }
        bool         CanRon        { get; }
        bool         CanChankanRon { get; }
    }

    public interface IPostDiscardDecision
    {
        Player                  Player   { get; }
        PostDiscardDecisionType Decision { get; }
        IMeld                   Call     { get; }
    }

    public interface IDiscardDecision
    {
        DiscardDecisionType Decision { get; }
        ITile               Tile     { get; }
    }

    public static class DecisionFactory
    {
        public static IDiscardDecision     BuildDiscardDecision(DiscardDecisionType d, ITile t)                   { return new DiscardDecisionImpl(d, t); }
        public static IPostDiscardDecision BuildPostDiscardDecision(Player p, PostDiscardDecisionType d, IMeld m) { return new PostDiscardDecisionImpl(p, d, m); }
    }
}
