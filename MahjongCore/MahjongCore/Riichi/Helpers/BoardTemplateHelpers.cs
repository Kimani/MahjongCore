// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;

namespace MahjongCore.Riichi.Helpers
{
    public static class BoardTemplateHelpers
    {
        public static IMeld GetNextPlayerMeld(IBoardTemplate template, IGameState state, Player player)
        {
            IHand hand = GameStateHelpers.GetHand(state, player);
            return CommonHelpers.Find(
                template.Melds,
                (IMeld meld) =>
                {
                    if (meld.Owner != player) { return false; }
                    return !HandHelpers.IterateMeldsOR(hand, (IMeld handMeld) => { return (handMeld.CompareTo(meld) == 0); });
                });
        }

        public static ITile GetNextPlayerDiscard(IBoardTemplate template, IGameState state, Player player)
        {
            int nextDiscardSlot = GameStateHelpers.GetHand(state, player).Discards.Count;
            return CommonHelpers.Find(
                template.GetDiscards(player),
                (ITile tile) => { return tile.Slot == nextDiscardSlot; });
        }

        public static IMeld GetMeld(IBoardTemplate template, ITile calledTile, Player targetPlayer)
        {
                return (calledTile != null) ? CommonHelpers.Find(
                                                  template.Melds,
                                                  (IMeld meld) => { return (meld.CalledTile.Slot == calledTile.Slot) &&
                                                                           (meld.Target == targetPlayer); }) :
                                              null;
        }
    }
}
