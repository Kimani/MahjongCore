// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using System;

namespace MahjongCore.Riichi.Impl
{
    internal class TileImpl : ITile, ICloneable
    {
        // ITile
        public TileType  Type        { get; internal set; } = TileType.None;
        public Location  Location    { get; internal set; } = Location.None;
        public Player    Ancillary   { get; internal set; } = Player.None;
        public ReachType Reach       { get; internal set; } = ReachType.None;
        public bool      Ghost       { get; internal set; } = false;
        public bool      Called      { get; internal set; } = false;
        public bool      WinningTile { get; internal set; } = false;
        public int       Slot        { get; internal set; } = -1;

        // ICloneable
        public object Clone()
        {
            TileImpl tile = new TileImpl();
            tile.Set(this);
            return tile;
        }

        // TileImpl
        private static uint REACH_FLAG      = 0x0040;
        private static uint CALLED_FLAG     = 0x0080;
        private static uint OPEN_REACH_FLAG = 0x0100;
        private static uint CALLER_P1_FLAG  = 0x0200;
        private static uint CALLER_P2_FLAG  = 0x0400;
        private static uint CALLER_P3_FLAG  = 0x0800;
        private static uint CALLER_P4_FLAG  = 0x1000;
        private static uint TILE_MASK       = 0x003F;

        public TileImpl()              { }
        public TileImpl(TileType type) { Type = type; }

        internal void Reset(bool skipConstantFields = false)
        {
            Type = TileType.None;
            Ancillary = Player.None;
            Reach = ReachType.None;
            Ghost = false;
            Called = false;
            WinningTile  = false;

            if (!skipConstantFields)
            {
                Location = Location.None;
                Slot = -1;
            }
        }

        internal void Set(ITile tile)
        {
            Type = tile.Type;
            Location = tile.Location;
            Ancillary = tile.Ancillary;
            Reach = tile.Reach;
            Ghost = tile.Ghost;
            Called = tile.Called;
            WinningTile = tile.WinningTile;
            Slot = tile.Slot;
        }

        internal string GetHexString()
        {
            uint value = (uint)Type.GetSkyValue();
            if (Reach == ReachType.OpenReach)  { value |= OPEN_REACH_FLAG; }
            else if (Reach == ReachType.Reach) { value |= REACH_FLAG; }
            if (Called)                        { value |= CALLED_FLAG; }
            if (Ancillary == Player.Player1)   { value |= CALLER_P1_FLAG; }
            if (Ancillary == Player.Player2)   { value |= CALLER_P2_FLAG; }
            if (Ancillary == Player.Player3)   { value |= CALLER_P3_FLAG; }
            if (Ancillary == Player.Player4)   { value |= CALLER_P4_FLAG; }
            return string.Format("{0:X4", value);
        }

        internal static bool TryGetTile(string value, out TileImpl tile)
        {
            bool found = false;
            tile = null;
            if ((value != null) && (value.Length == 4))
            {
                uint skyValue;
                if (uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out skyValue))
                {
                    tile = new TileImpl();
                    tile.Type = TileTypeExtensionMethods.GetTile((int)(skyValue & TILE_MASK));
                    tile.Ancillary = skyValue.IsFlagSet(CALLER_P1_FLAG) ? Player.Player1 :
                                     skyValue.IsFlagSet(CALLER_P2_FLAG) ? Player.Player2 :
                                     skyValue.IsFlagSet(CALLER_P3_FLAG) ? Player.Player3 :
                                     skyValue.IsFlagSet(CALLER_P4_FLAG) ? Player.Player4 :
                                                                          Player.None;
                    tile.Reach = skyValue.IsFlagSet(OPEN_REACH_FLAG) ? ReachType.OpenReach :
                                 skyValue.IsFlagSet(REACH_FLAG)      ? ReachType.Reach : 
                                                                       ReachType.None;
                    tile.Called = skyValue.IsFlagSet(CALLED_FLAG);
                    found = true;
                }
            }
            return found;
        }

        internal static TileImpl GetTile(string value)
        {
            TileImpl tile;
            if (!TryGetTile(value, out tile))
            {
                throw new Exception("Failed to parse TileImpl: " + value);
            }
            return tile;
        }
    }
}
