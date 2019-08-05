// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Riichi.Helpers;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class DiscardInfoImpl : IDiscardInfo
    {
        // IDiscardInfo
        public IHand           Hand               { get; internal set; }
        public IList<TileType> PromotedKanTiles   { get { return PromotedKanTilesRaw; } }
        public IList<TileType> ClosedKanTiles     { get { return ClosedKanTilesRaw; } }
        public IList<TileType> RestrictedTiles    { get { return RestrictedTilesRaw; } }
        public TileType        SuufurendanTile    { get; internal set; } = TileType.None;
        public TileSource      Source             { get; internal set; } = TileSource.Wall;
        public bool            CanNormalDiscard   { get; internal set; } = false;
        public bool            CanTsumo           { get; internal set; } = false;
        public bool            CanReach           { get; internal set; } = false;

        // DiscardInfoImpl
        internal List<TileType> PromotedKanTilesRaw { get; private set; } = new List<TileType>();
        internal List<TileType> ClosedKanTilesRaw   { get; private set; } = new List<TileType>();
        internal List<TileType> RestrictedTilesRaw  { get; private set; } = new List<TileType>();

        internal void Reset()
        {
            Hand = null;
            PromotedKanTilesRaw.Clear();
            ClosedKanTilesRaw.Clear();
            RestrictedTilesRaw.Clear();
            SuufurendanTile = TileType.None;
            Source = TileSource.Wall;
            CanNormalDiscard = false;
            CanTsumo  = false;
            CanReach  = false;
        }

        internal void Populate(HandImpl hand)
        {
            CommonHelpers.Check(hand.Parent is GameStateImpl, "Non inbox GameState not supported for populating DiscardInfo at this time.");
            GameStateImpl state = hand.Parent as GameStateImpl;
            IExtraSettings extraSettings = state.ExtraSettings;

            Reset();
            Hand             = hand;
            SuufurendanTile  = hand.GetSuufurendanTile();
            CanNormalDiscard = !extraSettings.DisableAnyDiscard && !extraSettings.DisablePlainDiscard && !extraSettings.DisableNonReach;
            CanTsumo         = hand.CanTsumo();
            CanReach         = hand.CanReach() && (state.TilesRemaining >= 4);
            Source           = (state.PreviousAction == GameAction.PickedFromWall)      ? TileSource.Wall :
                               (state.PreviousAction == GameAction.ReplacementTilePick) ? TileSource.DeadWall :
                                                                                          TileSource.Call;

            TileType kuikae = hand.GetKuikaeTile();
            if (kuikae != TileType.None)
            {
                RestrictedTilesRaw.AddUnique(kuikae);
            }

            foreach (TileType tt in extraSettings.RestrictDiscardTiles)
            {
                RestrictedTilesRaw.AddUnique(tt);
            }

            bool disableNonReachTiles = extraSettings.DisablePlainDiscard || extraSettings.DisableNonReach;

            if (extraSettings.DisableAnyDiscard || (disableNonReachTiles && hand.Open))
            {
                foreach (TileImpl tile in hand.ActiveHandRaw)
                {
                    RestrictedTilesRaw.AddUnique(tile.Type);
                }
            }
            else if (disableNonReachTiles)
            {
                for (int i = 0; i < hand.TileCount; ++i)
                {
                    IList<TileType> waits = hand.GetWaitsForDiscard(i);
                    if ((waits == null) || (waits.Count == 0))
                    {
                        RestrictedTilesRaw.AddUnique(hand.ActiveHandRaw[i].Type);
                    }
                }
            }

            hand.GetAvailablePromotedKans(PromotedKanTilesRaw);
            hand.GetAvailableClosedKans(ClosedKanTilesRaw);
        }
    }

    internal class PostDiscardInfoImpl : IPostDiscardInfo
    {
        // IPostDiscardInfo
        public IHand        Hand          { get; internal set; }
        public IHand        TargetPlayer  { get; internal set; }
        public IList<IMeld> Calls         { get { return CallsRaw; } }
        public ITile        DiscardedTile { get; internal set; }
        public bool         CanRon        { get; internal set; } = false;
        public bool         CanChankanRon { get; internal set; } = false;

        // PostDiscardInfoImpl
        internal List<IMeld> CallsRaw { get; private set; } = new List<IMeld>();

        internal void Reset()
        {
            Hand = null;
            TargetPlayer = null;
            CallsRaw.Clear();
            DiscardedTile = null;
            CanRon = false;
            CanChankanRon = false;
        }

        internal void Populate(HandImpl hand)
        {
            CommonHelpers.Check(hand.Parent is GameStateImpl, "Non inbox GameState not supported for populating DiscardInfo at this time.");
            GameStateImpl state = hand.Parent as GameStateImpl;

            Reset();
            Hand = hand;
            TargetPlayer = GameStateHelpers.GetHand(state, state.Current);

            IList<ITile> targetDiscards = TargetPlayer.Discards;
            DiscardedTile = targetDiscards[targetDiscards.Count - 1];

            CanRon = !hand.Furiten && hand.CheckRon();
            CanChankanRon = state.Settings.GetSetting<bool>(GameOption.Chankan) && CanRon && (state.PreviousAction == GameAction.PromotedKan);

            bool fourKanAbortiveDrawIncoming = state.Settings.GetSetting<bool>(GameOption.FourKanDraw) &&
                                               (state.PreviousAction == GameAction.ReplacementTilePick) &&
                                               (state.DoraCount >= 4) &&
                                               !state.GetHand(Player.Player1).FourKans && !state.GetHand(Player.Player2).FourKans &&
                                               !state.GetHand(Player.Player3).FourKans && !state.GetHand(Player.Player4).FourKans;

            List<IMeld> callList = (fourKanAbortiveDrawIncoming ||
                                    (state.TilesRemaining <= 0) ||
                                    (state.PreviousAction == GameAction.PromotedKan) ||
                                    (state.PreviousAction == GameAction.ClosedKan) ||
                                    hand.Reach.IsReach()) ? null : hand.GetCalls();
            if (callList != null)
            {
                CallsRaw.AddRange(callList);
            }
        }
    }

    internal class DiscardDecisionImpl : IDiscardDecision
    {
        // IDiscardDecision
        public DiscardDecisionType Decision { get; internal set; }
        public ITile               Tile     { get; internal set; }

        // DiscardDecisionImpl
        internal DiscardDecisionImpl() { Reset(); }

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
            bool valid = true;
            if ((hand is HandImpl handImpl) && (hand.Parent is GameStateImpl stateImpl))
            {
                valid = false;
                switch (Decision)
                {
                    case DiscardDecisionType.PromotedKan:       // Make sure this wasn't proceeded by a chii or pon.
                                                                valid = (Tile != null) && 
                                                                        (stateImpl.PreviousAction != GameAction.Chii) && 
                                                                        (stateImpl.PreviousAction != GameAction.Pon);

                                                                // Ensure that we have the tile in hand.
                                                                if (valid)
                                                                {
                                                                    valid = false;
                                                                    for (int i = 0; !valid && (i < hand.TileCount); ++i)
                                                                    {
                                                                        valid = Tile.Type.IsEqual(hand.Tiles[i].Type, true);
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
                                                                        (!hand.Reach.IsReach() || Tile.Type.IsEqual(hand.Tiles[hand.TileCount - 1].Type));
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
        public Player                  Player   { get; internal set; } = Player.None;
        public PostDiscardDecisionType Decision { get; internal set; } = PostDiscardDecisionType.Nothing;
        public IMeld                   Call     { get; internal set; } = null;

        // PostDiscardDecisionImpl
        public PostDiscardDecisionImpl() { }

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
