// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class DiscardInfoImpl : DiscardInfo
    {
        public IHand           Hand               { get; internal set; }
        public IList<TileType> PromotedKanTiles   { get; } = new List<TileType>();
        public IList<TileType> ClosedKanTiles     { get; } = new List<TileType>();
        public IList<TileType> RestrictedTiles    { get; } = new List<TileType>();
        public TileType        SuufurendanTile    { get; internal set; }
        public TileSource      Source             { get; internal set; }
        public bool            CanNormalDiscard   { get; internal set; }
        public bool            CanKyuushuuKyuuhai { get; internal set; }
        public bool            CanTsumo           { get; internal set; }
        public bool            CanReach           { get; internal set; }
    }

    internal class PostDiscardInfoImpl : PostDiscardInfo
    {
        public IHand        Hand          { get; internal set; }
        public IHand        TargetPlayer  { get; internal set; }
        public IList<IMeld> Calls         { get; } = new List<IMeld>();
        public ITile        DiscardedTile { get; internal set; }
        public bool         CanRon        { get; internal set; }
        public bool         CanChankanRon { get; internal set; }
    }

    internal class DiscardDecisionImpl : IDiscardDecision
    {
        // IDiscardDecision
        public DiscardDecisionType Decision { get; internal set; }
        public ITile               Tile     { get; internal set; }

        // DiscardDecisionImpl
        internal DiscardDecisionImpl(DiscardDecisionType decision, ITile tile)
        {
            Decision = decision;
            Tile = tile;
        }

        internal void Reset()
        {
            Decision = DiscardDecisionType.Invalid;
            Tile = null;
        }

        internal bool Validate(IHand hand)
        {
            HandImpl handImpl = hand as HandImpl;
            GameStateImpl stateImpl = hand.Parent as GameStateImpl;

            bool valid = true;
            if ((handImpl != null) && (stateImpl != null))
            {
                valid = false;
                switch (Decision)
                {
                    case DiscardDecisionType.PromotedKan:       // Make sure this wasn't proceeded by a chii or pon.
                                                                valid = (Tile != null) && 
                                                                        (stateImpl.PrevAction != GameAction.Chii) && 
                                                                        (stateImpl.PrevAction != GameAction.Pon);

                                                                // Ensure that we have the tile in hand.
                                                                if (valid)
                                                                {
                                                                    valid = false;
                                                                    for (int i = 0; !valid && (i < hand.ActiveTileCount); ++i)
                                                                    {
                                                                        valid = Tile.Type.IsEqual(hand.ActiveHand[i].Type, true);
                                                                    }
                                                                }

                                                                // Make sure we have a pon of this type too.
                                                                if (valid)
                                                                {
                                                                    valid = false;
                                                                    for (int i = 0; i < hand.OpenMeldCount; ++i)
                                                                    {
                                                                         if ((hand.Melds[i].State == MeldState.Pon) &&
                                                                             hand.Melds[i].CalledTile.Type.IsEqual(Tile.Type))
                                                                         {
                                                                             valid = true;
                                                                             break;
                                                                         }
                                                                    }
                                                                }
                                                                break;

                    case DiscardDecisionType.Discard:           valid = (Tile != null) && 
                                                                        (Tile.Type != TileType.None) &&
                                                                        (!hand.InReach || Tile.Type.IsEqual(hand.ActiveHand[hand.ActiveTileCount - 1].Type));
                                                                break;

                    case DiscardDecisionType.RiichiDiscard:     valid = hand.Parent.Settings.GetSetting<bool>(GameOption.Riichi) && hand.Tempai && hand.Closed;
                                                                break;

                    case DiscardDecisionType.OpenRiichiDiscard: valid = hand.Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) && hand.Tempai && hand.Closed;
                                                                break;

                    case DiscardDecisionType.ClosedKan:         valid = (Tile != null) && handImpl.CanClosedKanWithTile(Tile.Type);
                                                                break;

                    case DiscardDecisionType.Tsumo:             valid = (handImpl.WinningHandCache != null);
                                                                break;

                    case DiscardDecisionType.AbortiveDraw:      valid = hand.CouldKyuushuuKyuuhai;
                                                                break;

                    default:                                    break; // fValid should remain false.
                }
            }
            return valid;
        }
    }

    internal class PostDiscardDecisionImpl : IPostDiscardDecision
    {
        // IPostDiscardDecision
        public Player                  Player   { get; internal set; }
        public PostDiscardDecisionType Decision { get; internal set; }
        public IMeld                   Call     { get; internal set; }

        // PostDiscardDecisionImpl
        public PostDiscardDecisionImpl(Player player, PostDiscardDecisionType decision, IMeld call)
        {
            Player = player;
            Decision = decision;
            Call = call;
        }

        public void Reset()
        {
            Player = Player.None;
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
}
