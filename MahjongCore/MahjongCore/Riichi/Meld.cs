// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Impl;
using System;

namespace MahjongCore.Riichi
{
    public enum MeldType
    {
        [MeldTileCount(0), MeldCalled(false)] None,
        [MeldTileCount(3), MeldCalled(true)]  Chii,
        [MeldTileCount(3), MeldCalled(true)]  Pon,
        [MeldTileCount(4), MeldCalled(true)]  Kan
    }

    #region MeldState
        public enum MeldState
        {
            [MeldTypeAttribute(MeldType.None), MeldOpen(false), MeldFlippedTileCount(0), MeldCode(0), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  None,
            [MeldTypeAttribute(MeldType.Chii), MeldOpen(true),  MeldFlippedTileCount(0), MeldCode(1), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  Chii,
            [MeldTypeAttribute(MeldType.Pon),  MeldOpen(true),  MeldFlippedTileCount(0), MeldCode(2), MeldSimpleFu(2),  MeldNonSimpleFu(4)]  Pon,
            [MeldTypeAttribute(MeldType.Kan),  MeldOpen(true),  MeldFlippedTileCount(0), MeldCode(3), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanOpen,
            [MeldTypeAttribute(MeldType.Kan),  MeldOpen(false), MeldFlippedTileCount(2), MeldCode(4), MeldSimpleFu(16), MeldNonSimpleFu(32)] KanConcealed,
            [MeldTypeAttribute(MeldType.Kan),  MeldOpen(true),  MeldFlippedTileCount(0), MeldCode(5), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanPromoted
        }

        public static class MeldStateExtensionMethods
        {
            public static MeldType GetMeldType(this MeldState ms)                  { return EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms); }
            public static bool     IsOpen(this MeldState ms)                       { return EnumAttributes.GetAttributeValue<MeldOpen, bool>(ms); }
            public static int      GetFlippedTileCount(this MeldState ms)          { return EnumAttributes.GetAttributeValue<MeldFlippedTileCount, int>(ms); }
            public static int      GetMeldCode(this MeldState ms)                  { return EnumAttributes.GetAttributeValue<MeldCode, int>(ms); }
            public static int      GetMeldSimpleFu(this MeldState ms)              { return EnumAttributes.GetAttributeValue<MeldSimpleFu, int>(ms); }
            public static int      GetMeldNonSimpleFu(this MeldState ms)           { return EnumAttributes.GetAttributeValue<MeldNonSimpleFu, int>(ms); }
            public static bool     TryGetMeldState(string value, out MeldState ms) { return EnumHelper.TryGetEnumByCode<MeldState, MeldCode>(value, out ms); }

            public static bool IsCalled(this MeldState ms)
            {
                var meldType = EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms);
                return EnumAttributes.GetAttributeValue<MeldCalled, bool>(meldType);
            }

            public static int GetTileCount(this MeldState ms)
            {
                var meldType = EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms);
                return EnumAttributes.GetAttributeValue<MeldTileCount, int>(meldType);
            }

            public static MeldState TryGetMeldState(string value)
            {
                CommonHelpers.Check(EnumHelper.TryGetEnumByCode<MeldState, MeldCode>(value, out MeldState state), ("Failed to parse MeldState: " + value));
                return state;
            }
        }
    #endregion

    public enum CalledDirection
    {
        None,
        Left,
        Across,
        Right
    }

    public interface IMeld : ICloneable, IComparable<IMeld>
    {
        Player          Owner        { get; }
        Player          Target       { get; }
        MeldState       State        { get; }
        CalledDirection Direction    { get; }
        ITile[]         Tiles        { get; }
        ITile           CalledTile   { get; }
        int             RedDoraCount { get; }

        void  Promote(TileType kanTile, int kanTileSlot);
        ITile GetLowestTile();
        bool  ContainsSourceTile(int slot, Player source);
    }

    public static class MeldFactory
    {
        public static IMeld BuildChii(Player target, ITile tileCalled, TileType tileLo, TileType tileHi, int slotLo, int slotHi)
        {
            if (Global.CanAssert)
            {
                Global.Assert(target.IsPlayer());
                Global.Assert(tileCalled.Type.IsTile());
                Global.Assert(tileCalled.Type.GetSuit() == tileLo.GetSuit());
                Global.Assert(tileCalled.Type.GetSuit() == tileHi.GetSuit());

                int loValue = tileLo.GetValue();
                int hiValue = tileHi.GetValue();
                int calledValue = tileCalled.Type.GetValue();
                Global.Assert(loValue < hiValue);
                Global.Assert(((loValue + 1) == hiValue) || ((loValue + 2) == hiValue));
                Global.Assert(((loValue + 2) == hiValue) ? (calledValue == (loValue + 1)) :
                                                           ((calledValue == (loValue - 1)) || calledValue == (hiValue + 1)));
            }

            MeldImpl meld = new MeldImpl
            {
                Owner = target.GetNext(),
                Target = target,
                State = MeldState.Chii,
                Direction = CalledDirection.Left
            };

            TileImpl tile1 = meld.TilesRaw[0];
            tile1.Type = tileCalled.Type;
            tile1.Location = Location.Call;
            tile1.Ancillary = target;
            tile1.Called = true;
            tile1.Slot = tileCalled.Slot;

            TileImpl tile2 = meld.TilesRaw[1];
            tile2.Type = tileLo;
            tile2.Location = Location.Call;
            tile2.Ancillary = meld.Owner;
            tile2.Slot = slotLo;

            TileImpl tile3 = meld.TilesRaw[2];
            tile3.Type = tileHi;
            tile3.Location = Location.Call;
            tile3.Ancillary = meld.Owner;
            tile3.Slot = slotHi;
            return meld;
        }

        public static IMeld BuildPon(Player target,
                                     Player caller,
                                     ITile tileCalled,
                                     TileType tileA,
                                     TileType tileB,
                                     int slotA,
                                     int slotB)
        {
            if (Global.CanAssert)
            {
                Global.Assert(target.IsPlayer());
                Global.Assert(caller.IsPlayer());
                Global.Assert(target != caller);
                Global.Assert(tileCalled.Type.IsTile());
                Global.Assert(tileCalled.Type.GetValue() == tileA.GetValue());
                Global.Assert(tileCalled.Type.GetValue() == tileB.GetValue());
                Global.Assert(tileCalled.Type.GetSuit() == tileA.GetSuit());
                Global.Assert(tileCalled.Type.GetSuit() == tileB.GetSuit());
            }

            MeldImpl meld = new MeldImpl
            {
                Owner = caller,
                Target = target,
                State = MeldState.Pon,
                Direction = caller.GetTargetPlayerDirection(target)
            };

            TileImpl tileImplCalled, tileImplA, tileImplB;
            if (meld.Direction == CalledDirection.Left) { tileImplCalled = meld.TilesRaw[0]; tileImplA = meld.TilesRaw[1]; tileImplB = meld.TilesRaw[2]; }
            else if (meld.Direction == CalledDirection.Across) { tileImplCalled = meld.TilesRaw[1]; tileImplA = meld.TilesRaw[0]; tileImplB = meld.TilesRaw[2]; }
            else { tileImplCalled = meld.TilesRaw[2]; tileImplA = meld.TilesRaw[0]; tileImplB = meld.TilesRaw[1]; }

            tileImplCalled.Type = tileCalled.Type;
            tileImplCalled.Location = Location.Call;
            tileImplCalled.Ancillary = target;
            tileImplCalled.Called = true;
            tileImplCalled.Slot = tileCalled.Slot;

            tileImplA.Type = tileA;
            tileImplA.Location = Location.Call;
            tileImplA.Ancillary = caller;
            tileImplA.Slot = slotA;

            tileImplB.Type = tileB;
            tileImplB.Location = Location.Call;
            tileImplB.Ancillary = caller;
            tileImplB.Slot = slotB;
            return meld;
        }

        public static IMeld BuildOpenKan(Player target,
                                         Player caller,
                                         ITile tileCalled,
                                         TileType tileA,
                                         TileType tileB,
                                         TileType tileC,
                                         int slotA,
                                         int slotB,
                                         int slotC)
        {
            if (Global.CanAssert)
            {
                Global.Assert(target.IsPlayer());
                Global.Assert(caller.IsPlayer());
                Global.Assert(target != caller);
                Global.Assert(tileCalled.Type.IsTile());
                Global.Assert(tileCalled.Type.GetValue() == tileA.GetValue());
                Global.Assert(tileCalled.Type.GetValue() == tileB.GetValue());
                Global.Assert(tileCalled.Type.GetValue() == tileC.GetValue());
                Global.Assert(tileCalled.Type.GetSuit() == tileA.GetSuit());
                Global.Assert(tileCalled.Type.GetSuit() == tileB.GetSuit());
                Global.Assert(tileCalled.Type.GetSuit() == tileC.GetSuit());
            }

            MeldImpl meld = new MeldImpl
            {
                Owner = caller,
                Target = target,
                State = MeldState.KanOpen,
                Direction = caller.GetTargetPlayerDirection(target)
            };

            TileImpl tileImplCalled, tileImplA, tileImplB, tileImplC;
            if (meld.Direction == CalledDirection.Left) { tileImplCalled = meld.TilesRaw[0]; tileImplA = meld.TilesRaw[1]; tileImplB = meld.TilesRaw[2]; tileImplC = meld.TilesRaw[3]; }
            else if (meld.Direction == CalledDirection.Across) { tileImplCalled = meld.TilesRaw[1]; tileImplA = meld.TilesRaw[0]; tileImplB = meld.TilesRaw[2]; tileImplC = meld.TilesRaw[3]; }
            else { tileImplCalled = meld.TilesRaw[3]; tileImplA = meld.TilesRaw[0]; tileImplB = meld.TilesRaw[1]; tileImplC = meld.TilesRaw[2]; }

            tileImplCalled.Type = tileCalled.Type;
            tileImplCalled.Location = Location.Call;
            tileImplCalled.Ancillary = target;
            tileImplCalled.Called = true;
            tileImplCalled.Slot = tileCalled.Slot;

            tileImplA.Type = tileA;
            tileImplA.Location = Location.Call;
            tileImplA.Ancillary = caller;
            tileImplA.Slot = slotA;

            tileImplB.Type = tileB;
            tileImplB.Location = Location.Call;
            tileImplB.Ancillary = caller;
            tileImplB.Slot = slotB;

            tileImplC.Type = tileC;
            tileImplC.Location = Location.Call;
            tileImplC.Ancillary = caller;
            tileImplC.Slot = slotC;
            return meld;
        }

        public static IMeld BuildClosedKan(Player caller,
                                           TileType tileA,
                                           TileType tileB,
                                           TileType tileC,
                                           TileType tileD,
                                           int slotA,
                                           int slotB,
                                           int slotC,
                                           int slotD)
        {
            if (Global.CanAssert)
            {
                Global.Assert(caller.IsPlayer());
                Global.Assert(tileA.IsTile());
                Global.Assert(tileA.GetValue() == tileB.GetValue());
                Global.Assert(tileA.GetValue() == tileC.GetValue());
                Global.Assert(tileA.GetValue() == tileD.GetValue());
                Global.Assert(tileA.GetSuit() == tileB.GetSuit());
                Global.Assert(tileA.GetSuit() == tileC.GetSuit());
                Global.Assert(tileA.GetSuit() == tileD.GetSuit());
            }

            MeldImpl meld = new MeldImpl { Owner = caller, State = MeldState.KanConcealed };

            TileImpl tileImplA = meld.TilesRaw[0];
            tileImplA.Type = tileA;
            tileImplA.Location = Location.Call;
            tileImplA.Ancillary = caller;
            tileImplA.Slot = slotA;

            TileImpl tileImplB = meld.TilesRaw[1];
            tileImplB.Type = tileB;
            tileImplB.Location = Location.Call;
            tileImplB.Ancillary = caller;
            tileImplB.Slot = slotB;

            TileImpl tileImplC = meld.TilesRaw[2];
            tileImplC.Type = tileC;
            tileImplC.Location = Location.Call;
            tileImplC.Ancillary = caller;
            tileImplC.Slot = slotC;

            TileImpl tileImplD = meld.TilesRaw[3];
            tileImplD.Type = tileD;
            tileImplD.Location = Location.Call;
            tileImplD.Ancillary = caller;
            tileImplD.Slot = slotD;

            meld.SortMeldTilesForClosedKan();
            return meld;
        }

        public static IMeld BuildMeld(Player owner, MeldState state, ITile tileA, ITile tileB, ITile tileC, ITile tileD)
        {
            MeldImpl meld = new MeldImpl() { Owner = owner, State = state };
            
            // TODO: all these.
            if (state == MeldState.Chii)
            {

            }
            else if (state == MeldState.Pon)
            {

            }
            else if (state == MeldState.KanOpen)
            {

            }
            else if (state == MeldState.KanConcealed)
            {

            }
            else if (state == MeldState.KanPromoted)
            {

            }
            return meld;
        }
    }
}
