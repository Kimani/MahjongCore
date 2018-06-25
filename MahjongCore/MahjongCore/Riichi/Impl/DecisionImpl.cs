// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class DiscardInfoImpl : IDiscardInfo
    {
        // IDiscardInfo
        public IHand           Hand               { get; internal set; }
        public IList<TileType> PromotedKanTiles   { get; } = new List<TileType>();
        public IList<TileType> ClosedKanTiles     { get; } = new List<TileType>();
        public IList<TileType> RestrictedTiles    { get; } = new List<TileType>();
        public TileType        SuufurendanTile    { get; internal set; } = TileType.None;
        public TileSource      Source             { get; internal set; } = TileSource.Wall;
        public bool            CanNormalDiscard   { get; internal set; } = false;
        public bool            CanKyuushuuKyuuhai { get; internal set; } = false;
        public bool            CanTsumo           { get; internal set; } = false;
        public bool            CanReach           { get; internal set; } = false;

        // DiscardInfoImpl
        internal void Reset()
        {
            Hand = null;
            PromotedKanTiles.Clear();
            ClosedKanTiles.Clear();
            RestrictedTiles.Clear();
            SuufurendanTile = TileType.None;
            Source = TileSource.Wall;
            CanNormalDiscard = false;
            CanKyuushuuKyuuhai = false;
            CanTsumo  = false;
            CanReach  = false;
        }
    }

    internal class PostDiscardInfoImpl : IPostDiscardInfo
    {
        // IPostDiscardInfo
        public IHand        Hand          { get; internal set; }
        public IHand        TargetPlayer  { get; internal set; }
        public IList<IMeld> Calls         { get; } = new List<IMeld>();
        public ITile        DiscardedTile { get; internal set; }
        public bool         CanRon        { get; internal set; } = false;
        public bool         CanChankanRon { get; internal set; } = false;

        // PostDiscardInfoImpl
        internal void Reset()
        {
            Hand = null;
            TargetPlayer = null;
            Calls.Clear();
            DiscardedTile = null;
            CanRon = false;
            CanChankanRon = false;
        }
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
                                                                    for (int i = 0; i < hand.MeldCount; ++i)
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
