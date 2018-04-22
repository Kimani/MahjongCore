// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi
{
    public enum Location
    {
        None,
        Wall,
        Hand,
        DiscardPile,
        Call
    }

    public interface ITile
    {
        TileType Type        { get; }
        Location Location    { get; }
        bool     Ghost       { get; } // Ex: True if a called tile from a discard pile or a drawn tile from a wall.
        bool     Called      { get; } // Ex: Called tile either from a discard pile or the called tile from a meld.
        bool     Reach       { get; } // Ex: True if this tile is from a discard pile and is reached or open reached.
        bool     OpenReach   { get; } // Ex: True if this tile is from a discard pile and is open reached.
        bool     WinningTile { get; } // Ex: Tile in a candidate hand that is the winning tile Tsumo or Ron is called upon.
        Player   Caller      { get; } // Ex: A discard pile ITile.Caller would point to the player that called that tile.
        Player   Callee      { get; } // Ex: ITile.Source in a called meld would point to the player that sourced the tile.
        int      Slot        { get; } // Ex: Slot index from Source hand or discard pile. Could also refer to wall index.
    }

    public static class TileFactory
    {
        public static ITile BuildTile(TileType type) { return new ExtendedTile(type); }
    }
}
