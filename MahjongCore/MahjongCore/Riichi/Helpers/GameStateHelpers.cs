// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using System;

namespace MahjongCore.Riichi.Helpers
{
    public static class GameStateHelpers
    {
        public static int GetOffset(Player dealer, int roll)
        {
            int offset = (dealer == Player.Player1) ? 0 :               // bottom wall, right side, top tile
                         (dealer == Player.Player2) ? 102 :             // right wall, bottom side, top tile
                         (dealer == Player.Player3) ? 68 :              // top wall, left side, top tile
                                                      34;               // left wall, top side, top tile
            offset = TileHelpers.ClampTile(offset - ((roll - 1) * 34)); // Pick the wall.
            return offset + (roll * 2);                                 // Offset for the dead wall. *2 cause there's 2 tiles on top of each other...
        }

        public static int GetDoraIndicatorTileIndex(IGameState state, int doraIndex)
        {
            CommonHelpers.Check((doraIndex < 5), "Dora indicator out of range.");
            int deadWallOffset = (doraIndex == 0) ? 6 :
                                 (doraIndex == 1) ? 8 :
                                 (doraIndex == 2) ? 10 :
                                 (doraIndex == 3) ? 12 :
                                                    14;
            return TileHelpers.ClampTile(state.Offset - deadWallOffset);
        }

        public static IHand GetHand(IGameState state, Player p)
        {
            CommonHelpers.Check(p.IsPlayer(), "Requested player is not a player.");
            return (p == Player.Player1) ? state.Player1Hand :
                   (p == Player.Player2) ? state.Player2Hand :
                   (p == Player.Player3) ? state.Player3Hand :
                                           state.Player4Hand;
        }

        public static void IterateHands(IGameState state, Action<IHand> callback)
        {
            callback(state.Player1Hand);
            callback(state.Player2Hand);
            callback(state.Player3Hand);
            callback(state.Player4Hand);
        }
    }
}
