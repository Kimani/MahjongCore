// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;

namespace MahjongCore.Riichi.Helpers
{
    public class MeldHelpers
    {
        public static void IterateTiles(IMeld meld, Action<TileType> callback)
        {
            for (int i = 0; i < meld.State.GetTileCount(); ++i)
            {
                callback(meld.Tiles[i].Type);
            }
        }

        public static bool IterateTilesAND(IMeld meld, Func<TileType, bool> callback, bool noneResult = true)
        {
            for (int i = 0; i < meld.State.GetTileCount(); ++i)
            {
                if (!callback(meld.Tiles[i].Type))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IterateTilesOR(IMeld meld, Func<TileType, bool> callback, bool noneResult = true)
        {
            bool result = noneResult;
            for (int i = 0; i < meld.State.GetTileCount(); ++i)
            {
                if (callback(meld.Tiles[i].Type))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
