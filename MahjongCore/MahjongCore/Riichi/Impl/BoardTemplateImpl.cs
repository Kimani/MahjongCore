// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class BoardTemplateImpl : IBoardTemplate
    {
        // IBoardTemplate
        public IReadOnlyList<ITile> Wall         { get; private set; }
        public IReadOnlyList<ITile> Hand1        { get; private set; }
        public IReadOnlyList<ITile> Hand2        { get; private set; }
        public IReadOnlyList<ITile> Hand3        { get; private set; }
        public IReadOnlyList<ITile> Hand4        { get; private set; }
        public IReadOnlyList<ITile> Discards1    { get; private set; }
        public IReadOnlyList<ITile> Discards2    { get; private set; }
        public IReadOnlyList<ITile> Discards3    { get; private set; }
        public IReadOnlyList<ITile> Discards4    { get; private set; }
        public IReadOnlyList<IMeld> Melds        { get; private set; }
        public int                  DiscardCount { get; private set; }

        // BoardTemplateImpl
        public BoardTemplateImpl(
            IReadOnlyList<ITile> wall,
            IReadOnlyList<ITile> hand1,
            IReadOnlyList<ITile> hand2,
            IReadOnlyList<ITile> hand3,
            IReadOnlyList<ITile> hand4,
            IReadOnlyList<ITile> discards1,
            IReadOnlyList<ITile> discards2,
            IReadOnlyList<ITile> discards3,
            IReadOnlyList<ITile> discards4,
            IReadOnlyList<IMeld> melds,
            int discardCount)
        {
            Wall = CommonHelpers.SafeCopy(wall);
            Hand1 = CommonHelpers.SafeCopy(hand1);
            Hand2 = CommonHelpers.SafeCopy(hand2);
            Hand3 = CommonHelpers.SafeCopy(hand3);
            Hand4 = CommonHelpers.SafeCopy(hand4);
            Discards1 = CommonHelpers.SafeCopy(discards1);
            Discards2 = CommonHelpers.SafeCopy(discards2);
            Discards3 = CommonHelpers.SafeCopy(discards3);
            Discards4 = CommonHelpers.SafeCopy(discards4);
            Melds = CommonHelpers.SafeCopy(melds);
            DiscardCount = discardCount;
        }

        public void VerifyOrThrow(IGameState referenceState)
        {
            // Verify tiles are valid.
            VerifyTilesAreValid(Wall);
            VerifyTilesAreValid(Hand1);
            VerifyTilesAreValid(Hand2);
            VerifyTilesAreValid(Hand3);
            VerifyTilesAreValid(Hand4);
            VerifyTilesAreValid(Discards1);
            VerifyTilesAreValid(Discards2);
            VerifyTilesAreValid(Discards3);
            VerifyTilesAreValid(Discards4);

            // Verify that there aren't too many defined wall tiles.
            {
                var tileCounts = new Dictionary<TileType, int>();
                AddTilesToCount(tileCounts, Wall);
                VerifyOrThrowTileCounts(tileCounts, referenceState.Settings);
            }

            // Verify that there aren't too many defined hand, discard, and call tiles. Also include tiles in the wall that haven't been drawn yet.
            {
                var tileCounts = new Dictionary<TileType, int>();
                AddTilesToCount(tileCounts, Hand1);
                AddTilesToCount(tileCounts, Hand2);
                AddTilesToCount(tileCounts, Hand3);
                AddTilesToCount(tileCounts, Hand4);
                AddTilesToCount(tileCounts, Discards1);
                AddTilesToCount(tileCounts, Discards2);
                AddTilesToCount(tileCounts, Discards3);
                AddTilesToCount(tileCounts, Discards4);

                // Include tiles in the wall that haven't been drawn yet.
                int initialOffset = GameStateHelpers.GetOffset(referenceState.Dealer, referenceState.Roll);
                int offsetLoInclusive = TileHelpers.ClampTile(initialOffset + (13 * 4) + DiscardCount - Melds.Count);
                int offsetHiInclusive = TileHelpers.ClampTile(initialOffset - 1);
                if (offsetHiInclusive < offsetLoInclusive) { offsetHiInclusive += TileHelpers.TOTAL_TILE_COUNT; }

                CommonHelpers.Iterate(Wall, (ITile tile, int i) =>
                {
                    int tileSlot = (tile.Slot < offsetLoInclusive) ? (tile.Slot + TileHelpers.TOTAL_TILE_COUNT) : tile.Slot;
                    if ((tileSlot >= offsetLoInclusive) && (tileSlot <= offsetHiInclusive)) { AddTileToCount(tileCounts, tile.Type); }
                });

                CommonHelpers.Iterate(Melds, (IMeld meld, int i) => { MeldHelpers.IterateTiles(meld, (TileType meldTile) => { AddTileToCount(tileCounts, meldTile); }); });
                VerifyOrThrowTileCounts(tileCounts, referenceState.Settings);
            }

            // Verify that there aren't too many tiles per person (hand + call tiles).
            {
                var playerTileCounts = new Dictionary<Player, int>();
                AddPlayerToCount(playerTileCounts, Player.Player1, Hand1.Count);
                AddPlayerToCount(playerTileCounts, Player.Player2, Hand2.Count);
                AddPlayerToCount(playerTileCounts, Player.Player3, Hand3.Count);
                AddPlayerToCount(playerTileCounts, Player.Player4, Hand4.Count);
                CommonHelpers.Iterate(Melds, (IMeld meld, int i) =>
                {
                    CommonHelpers.Check(meld.State.IsCalled(), "Tried to specify a meld that isn't actually a meld (state is none)");
                    CommonHelpers.Check(meld.Owner.IsPlayer(), "Specified meld isn't even for a player?!");
                    AddPlayerToCount(playerTileCounts, meld.Owner, 3);
                });

                CommonHelpers.Check((playerTileCounts[Player.Player1] <= 13), "Too many tiles specified for Player1: " + playerTileCounts[Player.Player1]);
                CommonHelpers.Check((playerTileCounts[Player.Player2] <= 13), "Too many tiles specified for Player2: " + playerTileCounts[Player.Player2]);
                CommonHelpers.Check((playerTileCounts[Player.Player3] <= 13), "Too many tiles specified for Player3: " + playerTileCounts[Player.Player3]);
                CommonHelpers.Check((playerTileCounts[Player.Player4] <= 13), "Too many tiles specified for Player4: " + playerTileCounts[Player.Player4]);
            }

            // TODO: Verify that there aren't too many tiles defined too late in the wall for tiles defined in hands/calls/discards to be possible.
            // TODO: Verify that there aren't too many tiles defined in a player's haipai that would make early calls impossible.
            // TODO: Verify that there aren't any closed kans that happen between other calls that would not be possible (no draws in between calls).
        }

        private void AddTilesToCount(Dictionary<TileType, int> tileCounts, IReadOnlyList<ITile> tileList)
        {
            CommonHelpers.Iterate(Wall, (ITile tile, int i) =>
            {
                CommonHelpers.Check(tile.Type.IsTile(), ("Defined a tile that isn't actually a tile? Slot: " + tile.Slot));
                AddTileToCount(tileCounts, tile.Type);
            });
        }

        private void AddTileToCount(Dictionary<TileType, int> tileCounts, TileType tileType)
        {
            if (tileCounts.ContainsKey(tileType)) { tileCounts[tileType] = tileCounts[tileType]++; }
            else                                  { tileCounts[tileType] = 1; }
        }

        private void AddPlayerToCount(Dictionary<Player, int> playerCounts, Player player, int count = 1)
        {
            if (playerCounts.ContainsKey(player)) { playerCounts[player] = playerCounts[player] += count; }
            else                                  { playerCounts[player] = count; }
        }

        private void VerifyTilesAreValid(IReadOnlyList<ITile> tiles)
        {
            CommonHelpers.Iterate(tiles, (ITile tile, int i) =>
            {
                CommonHelpers.Check(tile.Type.IsTile(), "Specified tile isn't a tile");
            });
        }

        private void VerifyOrThrowTileCounts(Dictionary<TileType, int> tileCounts, IGameSettings settings)
        {
            var redDoraSetting = settings.GetSetting<RedDora>(GameOption.RedDoraOption);
            foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
            {
                if (tileType.IsTile() && tileCounts.ContainsKey(tileType))
                {
                    int maximumTileCount = 4;
                    if (tileType.GetValue() == 5)
                    {
                        Suit s = tileType.GetSuit();
                        int redDoraCount = (s == Suit.Characters) ? redDoraSetting.GetRedDoraManzu() :
                                            (s == Suit.Circles)    ? redDoraSetting.GetRedDoraPinzu() :
                                            (s == Suit.Bamboo)     ? redDoraSetting.GetRedDoraSouzu() : 0;
                        maximumTileCount = tileType.IsRedDora() ? redDoraCount : (4 - redDoraCount);
                    }
                    CommonHelpers.Check(
                        (tileCounts[tileType] <= maximumTileCount),
                        ("Too many of the same tile defined for the wall! Tile: " + tileType + " MaxCount: " + maximumTileCount + " Defined Count: "+ tileCounts[tileType]));
                }
            }
        }
    }
}
