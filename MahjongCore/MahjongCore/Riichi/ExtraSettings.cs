// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public interface IExtraSettings : ICloneable, IComparable<IExtraSettings>
    {
        IList<TileType> RestrictDiscardTiles  { get; set; }
        bool            DisableAnyDiscard     { get; set; } // Not allowed to discard at all. Only tsumo/abortive draw/kan/etc.
        bool            DisableCall           { get; set; } // Disables pon/chii/open kan by non-CPU players.
        bool            DisableCalling        { get; set; } // Disables for non-CPU players ANY kind of call on a tile, including ron.
        bool            DisableCallPass       { get; set; } // Not allowed to pass if a call is available (pon/chii/open kan).
        bool            DisableCPUWin         { get; set; } // CPU is not allowed to ron.
        bool            DisableCPUCalling     { get; set; } // CPU is not allowed to pon/chii/open kan.
        bool            DisablePlainDiscard   { get; set; } // Not allowed to discard normally. Can only reach/kan/abortive draw/tsumo.
        bool            DisableRonPass        { get; set; } // Not allowed to pass if a ron is available.
        bool            DisableReach          { get; set; }
        bool            DisableRed5           { get; set; }
        bool            DisableNonReach       { get; set; } // Can only reach.
        bool            DisableAbortiveDraw   { get; set; } // Can't abortive draw.
        int?            OverrideDiceRoll      { get; set; }
    }

    public static class ExtraSettingsFactory
    {
        public static IExtraSettings BuildExtraSettings() { return new ExtraSettingsImpl(); }
    }
}
