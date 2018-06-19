// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi.Impl
{
    internal class PostDiscardDecision : IPostDiscardDecision
    {
        public PostDiscardDecisionType Decision { get; internal set; } = PostDiscardDecisionType.Nothing;
        public IMeld                   Call     { get; internal set; } = null;

        public PostDiscardDecision(PostDiscardDecisionType decision, IMeld call)
        {
            Decision = decision;
            Call = call;
        }

        public void Reset()
        {
            Decision = PostDiscardDecisionType.Nothing;
            Call = null;
        }

        public bool Validate()
        {
            // Make sure we have a valid call.
            if (Decision == PostDiscardDecisionType.Call)
            {
                if ((Call == null) || (Call.State == MeldState.None) || (Call.State == MeldState.KanConcealed) || (Call.State == MeldState.KanPromoted))
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal class DiscardDecision : IDiscardDecision
    {
        public DiscardDecisionType Decision { get; internal set; } = DiscardDecisionType.Invalid;
        public ITile               Tile     { get; internal set; } = null;

        public DiscardDecision(DiscardDecisionType decision, ITile tile)
        {
            Decision = decision;
            Tile = tile;
        }

        public void Reset()
        {
            Decision = DiscardDecisionType.Invalid;
            Tile = null;
        }

        public bool Validate(Hand hand)
        {
            bool fValid = false;
            switch (Decision)
            {
                case DiscardDecisionType.PromotedKan:       // Make sure this wasn't proceeded by a chii or pon.
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

                case DiscardDecisionType.Discard:           fValid = (Tile != TileType.None) && (!hand.IsInReach() || Tile.IsEqual(hand.ActiveHand[hand.ActiveTileCount - 1]));
                                                            break;

                case DiscardDecisionType.RiichiDiscard:     fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.Riichi) && hand.IsTempai() && hand.IsClosed();
                                                            break;

                case DiscardDecisionType.OpenRiichiDiscard: fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) && hand.IsTempai() && hand.IsClosed();
                                                            break;

                case DiscardDecisionType.ClosedKan:         fValid = hand.CanClosedKanWithTile(Tile);
                                                            break;

                case DiscardDecisionType.Tsumo:             fValid = (hand.WinningHandCache != null);
                                                            break;

                case DiscardDecisionType.AbortiveDraw:      fValid = hand.CanKyuushuuKyuuhai();
                                                            break;

                default:                                    break; // fValid should remain false.
            }
            return fValid;
        }
    }
}
