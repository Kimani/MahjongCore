// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;

namespace MahjongCore.Riichi.Helpers
{
    public class MeldHelpers
    {
        public static void IterateTiles(IMeld meld, Action<TileType> callback) { for (int i = 0; i < meld.State.GetTileCount(); ++i) { callback(meld.Tiles[i].Type); } }
        public static void IterateTiles(IMeld meld, Action<ITile> callback)    { for (int i = 0; i < meld.State.GetTileCount(); ++i) { callback(meld.Tiles[i]); } }

        public static bool IterateTilesAND(IMeld meld, Func<TileType, bool> callback)
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

        public static bool IterateTilesAND(IMeld meld, Func<ITile, bool> callback)
        {
            for (int i = 0; i < meld.State.GetTileCount(); ++i)
            {
                if (!callback(meld.Tiles[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IterateTilesOR(IMeld meld, Func<TileType, bool> callback)
        {
            bool result = false;
            for (int i = 0; !result && (i < meld.State.GetTileCount()); ++i)
            {
                result = callback(meld.Tiles[i].Type);
            }
            return result;
        }

        public static bool IterateTilesOR(IMeld meld, Func<ITile, bool> callback)
        {
            bool result = false;
            for (int i = 0; !result && (i < meld.State.GetTileCount()); ++i)
            {
                result = callback(meld.Tiles[i]);
            }
            return result;
        }
    }
}
