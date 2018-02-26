// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Impl
{
    internal class DiscardDecision
    {
        public Decision DecisionToMake = Decision.Invalid;
        public TileType Tile = TileType.None;    // Should be one of the RiichiTiles tiles. See remarks on Decision.
        public int Slot = -1;               // Slot must be valid for a slot in the hand matching Tile when Tile is valid.

        public bool Validate(Hand hand)
        {
            bool fValid = false;
            switch (DecisionToMake)
            {
                case Decision.PromotedKan:       // Make sure this wasn't proceeded by a chii or pon.
                                                 fValid = (hand.Parent.PrevAction != GameAction.Chii) && (hand.Parent.PrevAction != GameAction.Pon);

                                                 // Ensure that we have the tile in rHand. Once we find it
                                                 // fValid will become true and we'll be done here.
                                                 if (fValid)
                                                 {
                                                     fValid = false;
                                                     for (int i = 0; !fValid && (i < hand.ActiveTileCount); ++i)
                                                     {
                                                         fValid = Tile.IsEqual(hand.ActiveHand[i]);
                                                     }
                                                 }

                                                 // Make sure we have a pon of this type too.
                                                 if (fValid)
                                                 {
                                                     fValid = false;
                                                     for (int i = 0; !fValid && (i < 4); ++i)
                                                     {
                                                         fValid = (hand.OpenMeld[i].State == MeldState.Pon) && hand.OpenMeld[i].Tiles[0].Tile.IsEqual(Tile);
                                                     }
                                                 }
                                                 break;

                case Decision.Discard:           fValid = (Tile != TileType.None) && (!hand.IsInReach() || Tile.IsEqual(hand.ActiveHand[hand.ActiveTileCount - 1]));
                                                 break;

                case Decision.RiichiDiscard:     fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.Riichi) && hand.IsTempai() && hand.IsClosed();
                                                 break;

                case Decision.OpenRiichiDiscard: fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) && hand.IsTempai() && hand.IsClosed();
                                                 break;

                case Decision.ClosedKan:         fValid = hand.CanClosedKanWithTile(Tile);
                                                 break;

                case Decision.Tsumo:             fValid = (hand.WinningHandCache != null);
                                                 break;

                case Decision.AbortiveDraw:      fValid = hand.CanKyuushuuKyuuhai();
                                                 break;

                default:                         break; // fValid should remain false.
            }
            return fValid;
        }
    }
}
