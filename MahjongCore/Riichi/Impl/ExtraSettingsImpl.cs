﻿// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class ExtraSettingsImpl : IExtraSettings
    {
        // IExtraSettings
        public IList<TileType> RestrictDiscardTiles  { get; set; } = new List<TileType>();
        public bool            DisableAnyDiscard     { get; set; } = false;
        public bool            DisableCall           { get; set; } = false;
        public bool            DisableCalling        { get; set; } = false;
        public bool            DisableCallPass       { get; set; } = false;
        public bool            DisableCPUWin         { get; set; } = false;
        public bool            DisableCPUCalling     { get; set; } = false;
        public bool            DisablePlainDiscard   { get; set; } = false;
        public bool            DisableRonPass        { get; set; } = false;
        public bool            DisableReach          { get; set; } = false;
        public bool            DisableRed5           { get; set; } = true;
        public bool            DisableNonReach       { get; set; } = false;
        public bool            DisableAbortiveDraw   { get; set; } = false;
        public int?            OverrideDiceRoll      { get; set; } = null;

        public void Reset()
        {
            RestrictDiscardTiles.Clear();
            DisableAnyDiscard = false;
            DisableCall = false;
            DisableCalling  = false;
            DisableCallPass  = false;
            DisableCPUWin = false;
            DisableCPUCalling  = false;
            DisablePlainDiscard  = false;
            DisableRonPass  = false;
            DisableReach  = false;
            DisableRed5 = true;
            DisableNonReach = false;
            DisableAbortiveDraw  = false;
            OverrideDiceRoll = null;
        }

        // ICloneable
        object ICloneable.Clone()
        {
            ExtraSettingsImpl extra = new ExtraSettingsImpl
            {
                DisableAnyDiscard    = DisableAnyDiscard,
                DisableCall         = DisableCall,
                DisableCalling      = DisableCalling,
                DisableCallPass     = DisableCallPass,
                DisableCPUWin       = DisableCPUWin,
                DisableCPUCalling   = DisableCPUCalling,
                DisablePlainDiscard = DisablePlainDiscard,
                DisableRonPass      = DisableRonPass,
                DisableReach        = DisableReach,
                DisableRed5         = DisableRed5,
                DisableAbortiveDraw = DisableAbortiveDraw,
                OverrideDiceRoll    = OverrideDiceRoll
            };
            (extra.RestrictDiscardTiles as List<TileType>).AddRange(RestrictDiscardTiles);
            return extra;
        }

        // IComparable<IExtraSettings>
        public int CompareTo(IExtraSettings other)
        {
            int value = DisableAnyDiscard.CompareTo(other.DisableAnyDiscard); if (value != 0) { return value; }
            value = DisableCall.CompareTo(other.DisableCall);                 if (value != 0) { return value; }
            value = DisableCalling.CompareTo(other.DisableCalling);           if (value != 0) { return value; }
            value = DisableCallPass.CompareTo(other.DisableCallPass);         if (value != 0) { return value; }
            value = DisableCPUWin.CompareTo(other.DisableCPUWin);             if (value != 0) { return value; }
            value = DisableCPUCalling.CompareTo(other.DisableCPUCalling);     if (value != 0) { return value; }
            value = DisablePlainDiscard.CompareTo(other.DisablePlainDiscard); if (value != 0) { return value; }
            value = DisableRonPass.CompareTo(other.DisableRonPass);           if (value != 0) { return value; }
            value = DisableReach.CompareTo(other.DisableReach);               if (value != 0) { return value; }
            value = DisableRed5.CompareTo(other.DisableRed5);                 if (value != 0) { return value; }
            value = DisableNonReach.CompareTo(other.DisableNonReach);         if (value != 0) { return value; }
            value = DisableAbortiveDraw.CompareTo(other.DisableAbortiveDraw); if (value != 0) { return value; }

            if      ((OverrideDiceRoll == null) != (other.OverrideDiceRoll == null)) { return GetHashCode() - other.GetHashCode(); }
            else if ((OverrideDiceRoll != null) && (other.OverrideDiceRoll != null))
            {
                value = OverrideDiceRoll.Value.CompareTo(other.OverrideDiceRoll.Value);
                if (value != 0)
                {
                    return value;
                }
            }

            value = RestrictDiscardTiles.Count - other.RestrictDiscardTiles.Count; if (value != 0) { return value; }
            foreach (TileType tt in RestrictDiscardTiles)
            {
                if (!other.RestrictDiscardTiles.Contains(tt))
                {
                    return GetHashCode() - other.GetHashCode();
                }
            }
            return 0;
        }
    }
}
