// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Riichi.Impl;

namespace MahjongCore.Riichi.Helpers
{
    public class TileHelpers
    {
        public static int WALL_LENGTH           = 17;
        public static int WALL_HEIGHT           = 2;
        public static int WALL_COUNT            = 4;
        public static int MAX_MELD_SIZE         = 4;
        public static int MAX_MELD_COUNT        = 4;
        public static int TOTAL_TILE_COUNT      = WALL_LENGTH * WALL_HEIGHT * WALL_COUNT; // 136
        public static int DEAD_WALL_TILE_COUNT  = 14;
        public static int MAIN_WALL_TILE_COUNT  = TOTAL_TILE_COUNT - DEAD_WALL_TILE_COUNT; // 122
        public static int WALL_QUARTER_COUNT    = TOTAL_TILE_COUNT / 4;
        public static int HAND_SIZE             = 14;
        public static int MAX_DISCARD_PILE_SIZE = 32;

        private static TileType[] TileSource = new TileType[TOTAL_TILE_COUNT];

        public static TileType BuildTile(int skyValue)
        {
            TileType t = TileType.None;
            foreach (TileType tt in System.Enum.GetValues(typeof(TileType)))
            {
                if (tt.GetSkyValue() == skyValue)
                {
                    t = tt;
                    break;
                }
            }
            return t;
        }

        public static TileType BuildTile(Suit suit, int value, bool redDora = false)
        {
            return (suit == Suit.East)                                   ? TileType.East :
                   (suit == Suit.West)                                   ? TileType.West :
                   (suit == Suit.North)                                  ? TileType.North :
                   (suit == Suit.South)                                  ? TileType.South :
                   (suit == Suit.Chun)                                   ? TileType.Chun :
                   (suit == Suit.Haku)                                   ? TileType.Haku :
                   (suit == Suit.Hatsu)                                  ? TileType.Hatsu :
                   ((suit != Suit.None) && (value >= 1) && (value <= 9)) ? FindTile(suit, value, redDora) :
                                                                           TileType.None;
        }

        public static int ClampTile(int slot)
        {
            // Takes in a tile number based that can be < 0 or > TOTAL_TILE_COUNT and will wrap it around.
            int tTile = slot;
            while (tTile < 0)
            {
                tTile += TOTAL_TILE_COUNT;
            }
            return tTile % TOTAL_TILE_COUNT;
        }

        public static ITile[] GetRandomBoard(ITile[] existingArray, RedDora redDoraSetting)
        {
            CommonHelpers.Check(((existingArray == null) || existingArray is TileImpl[]), "Supplied tile array must consist of inbox ITile implementations.");
            CommonHelpers.Check(((existingArray == null) || (existingArray.Length == TOTAL_TILE_COUNT)), "Supplied tile array must be " + TOTAL_TILE_COUNT + " tiles.");

            TileImpl[] targetArray = existingArray as TileImpl[];
            if (targetArray == null)
            {
                targetArray = new TileImpl[TOTAL_TILE_COUNT];
                for (int i = 0; i < TOTAL_TILE_COUNT; ++i)
                {
                    targetArray[i] = new TileImpl();
                    targetArray[i].Slot = i;
                    targetArray[i].Location = Location.Wall;
                }
            }

            // Build a list with all the tiles in it. One red dora for each suit.
            int counter = 0;
            for (byte i = 0; i <= 36; ++i)
            {
                TileType tt = TileTypeExtensionMethods.GetTile(i);
                int count = 4;
                if (tt.GetValue() == 5)
                {
                    Suit s = tt.GetSuit();
                    int redDoraCount = (s == Suit.Characters) ? redDoraSetting.GetRedDoraManzu() :
                                       (s == Suit.Circles)    ? redDoraSetting.GetRedDoraPinzu() :
                                       (s == Suit.Bamboo)     ? redDoraSetting.GetRedDoraSouzu() : 0;
                    count = tt.IsRedDora() ? redDoraCount : (4 - redDoraCount);
                }

                for (int j = 0; j < count; ++j)
                {
                    TileSource[counter++] = tt;
                }
            }

            // Randomize board by picking a random tiles out of TileSource until it is depleted.
            for (int i = 0; i < 136; ++i)
            {
                int slot = Global.RandomRange(0, (136 - i));
                TileType pickedTile = TileSource[slot];
                TileSource[slot] = TileSource[136 - i - 1];

                targetArray[i].Type = pickedTile;
            }

            if (Global.AssertHandler != null)
            {
                bool passed = true;
                for (int i = 0; i <= 36; ++i)
                {
                    TileType tt = TileTypeExtensionMethods.GetTile(i);
                    int count = 4;
                    if (tt.GetValue() == 5)
                    {
                        Suit s = tt.GetSuit();
                        int redDoraCount = (s == Suit.Characters) ? redDoraSetting.GetRedDoraManzu() :
                                           (s == Suit.Circles)    ? redDoraSetting.GetRedDoraPinzu() :
                                           (s == Suit.Bamboo)     ? redDoraSetting.GetRedDoraSouzu() : 0;
                        count = tt.IsRedDora() ? redDoraCount : (4 - redDoraCount);
                    }

                    int seen = 0;
                    for (int j = 0; j < 136; ++j)
                    {
                        if (targetArray[j].Type.GetSkyValue() == i)
                        {
                            ++seen;
                        }
                    }
                    Global.Assert(seen == count);
                }
                Global.Assert(passed);
            }
            return targetArray;
        }

        private static TileType FindTile(Suit suit, int value, bool redDora)
        {
            TileType t = TileType.None;
            foreach (TileType tt in System.Enum.GetValues(typeof(TileType)))
            {
                if ((tt.GetSuit() == suit) && (tt.GetValue() == value))
                {
                    t = tt;
                    break;
                }
            }
            return t;
        }
    }
}
