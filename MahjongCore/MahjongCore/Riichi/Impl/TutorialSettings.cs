// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    #region CameraPosition
        public enum CameraPosition
        {
            [TextValue("default")]         Default,
            [TextValue("deadwall")]        DeadWall,
            [TextValue("centerpiece")]     Centerpiece,
            [TextValue("reachindicator1")] ReachIndicator1,
            [TextValue("reachindicator2")] ReachIndicator2,
            [TextValue("reachindicator3")] ReachIndicator3,
            [TextValue("reachindicator4")] ReachIndicator4,
            [TextValue("rinshan")]         RinshanTile,
            [TextValue("newdora")]         NewDoraTile,
            [TextValue("discards1")]       Discards1,
            [TextValue("discards2")]       Discards2,
            [TextValue("discards3")]       Discards3,
            [TextValue("discards4")]       Discards4,
            [TextValue("aheadofdeadwall")] AheadOfDeadWall
        }

        public static class CameraPositionExtensionMethods
        {
            public static string GetId(this CameraPosition cp) { return EnumAttributes.GetAttributeValue<TextValue, string>(cp); }

            public static CameraPosition? GetFromId(string id)
            {
                CameraPosition? cp = null;
                foreach (CameraPosition testPos in Enum.GetValues(typeof(CameraPosition)))
                {
                    if (testPos.GetId().Equals(id))
                    {
                        cp = testPos;
                        break;
                    }
                }
                return cp;
            }
        }
    #endregion

    #region CameraZoom
        public enum CameraZoom
        {
            [TextValue(null)]     None,
            [TextValue("normal")] Normal,
            [TextValue("close")]  Close,
            [TextValue("far")]    Far
        }

        public static class CameraZoomExtensionMethods
        {
            public static string GetId(this CameraZoom cz) { return EnumAttributes.GetAttributeValue<TextValue, string>(cz); }

            public static CameraZoom? GetFromId(string id)
            {
                CameraZoom? cz = null;
                foreach (CameraZoom testZoom in Enum.GetValues(typeof(CameraZoom)))
                {
                    if (id.Equals(testZoom.GetId()))
                    {
                        cz = testZoom;
                        break;
                    }
                }
                return cz;
            }
        }
    #endregion

    public class TutorialSettings
    {
        public List<TileType> RestrictDiscardTiles  { get; set; }
        public CameraPosition CameraPosition        { get; set; }
        public CameraZoom     CameraZoom            { get; set; }
        public bool           DisableAnyDiscard     { get; set; }
        public bool           DisableCall           { get; set; }
        public bool           DisableCalling        { get; set; }
        public bool           DisableCallPass       { get; set; }
        public bool           DisableCPUWin         { get; set; }
        public bool           DisableCPUCalling     { get; set; }
        public bool           DisablePlainDiscard   { get; set; }
        public bool           DisableRonPass        { get; set; }
        public bool           DisableReach          { get; set; }
        public bool           DisableRed5           { get; set; }
        public bool           TilesNoRed5           { get; set; }
        public bool           RinshanDeferFlag      { get; set; }
        public bool           ReachOnly             { get; set; }
        public bool           EnableAbortiveDraw    { get; set; }
        public bool           OverrideDiceRoll      { get; set; }
        public bool           Player1CPU            { get; set; }
        public int            OverrideDiceRollValue { get; set; }

        public TutorialSettings()
        {
            RestrictDiscardTiles  = new List<TileType>();
            CameraPosition        = CameraPosition.Default;
            CameraZoom            = CameraZoom.Normal;
            DisableAnyDiscard     = false;
            DisableCall           = false;
            DisableCalling        = false;
            DisableCallPass       = false;
            DisableCPUWin         = false;
            DisableCPUCalling     = false;
            DisablePlainDiscard   = false;
            DisableRonPass        = false;
            DisableReach          = false;
            DisableRed5           = true;
            TilesNoRed5           = false;
            RinshanDeferFlag      = false;
            ReachOnly             = false;
            EnableAbortiveDraw    = false;
            OverrideDiceRoll      = false;
            Player1CPU            = false;
            OverrideDiceRollValue = 0;
        }

        public TutorialSettings Clone()
        {
            TutorialSettings ts = new TutorialSettings();
            ts.CameraPosition = CameraPosition;
            ts.CameraZoom = CameraZoom;
            ts.DisableAnyDiscard = DisableAnyDiscard;
            ts.DisableCall = DisableCall;
            ts.DisableCalling = DisableCalling;
            ts.DisableCallPass = DisableCallPass;
            ts.DisableCPUWin = DisableCPUWin;
            ts.DisableCPUCalling = DisableCPUCalling;
            ts.DisablePlainDiscard = DisablePlainDiscard;
            ts.DisableRonPass = DisableRonPass;
            ts.DisableReach = DisableReach;
            ts.DisableRed5 = DisableRed5;
            ts.TilesNoRed5 = TilesNoRed5;
            ts.RinshanDeferFlag = RinshanDeferFlag;
            ts.ReachOnly = ReachOnly;
            ts.EnableAbortiveDraw = EnableAbortiveDraw;
            ts.OverrideDiceRoll = OverrideDiceRoll;
            ts.Player1CPU = Player1CPU;
            ts.OverrideDiceRollValue = OverrideDiceRollValue;
            ts.RestrictDiscardTiles.AddRange(RestrictDiscardTiles);
            return ts;
        }
    }
}
