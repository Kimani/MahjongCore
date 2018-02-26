// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;

namespace MahjongCore.Riichi
{
    public enum PostDiscardDecisionType
    {
        Nothing,
        Call,
        Ron
    }

    public interface IPostDiscardDecision
    {
        PostDiscardDecisionType DecisionToMake { get; }
        CallOption              CallToMake     { get; }
    }

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

    public interface IDiscardDecision
    {
        DiscardDecisionType DecisionToMake { get; }
        TileType            Tile           { get; }
        int                 Slot           { get; } // Slot must be valid for a slot in the hand matching Tile when Tile is valid.
    }
}
