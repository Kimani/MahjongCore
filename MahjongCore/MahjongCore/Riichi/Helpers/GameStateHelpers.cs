// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

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
    }
}
