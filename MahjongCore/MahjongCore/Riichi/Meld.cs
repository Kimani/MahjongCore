// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
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

    public enum KanType
    {
        Open,
        Concealed,
        Promoted
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
            public static MeldType GetMeldType(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms); }
            public static bool     IsOpen(this MeldState ms)              { return EnumAttributes.GetAttributeValue<MeldOpen, bool>(ms); }
            public static bool     IsCalled(this MeldState ms)            { var meldType = EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms);
                                                                            return EnumAttributes.GetAttributeValue<MeldCalled, bool>(meldType); }
            public static int      GetTileCount(this MeldState ms)        { var meldType = EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms);
                                                                            return EnumAttributes.GetAttributeValue<MeldTileCount, int>(meldType); }
            public static int      GetFlippedTileCount(this MeldState ms) { return EnumAttributes.GetAttributeValue<MeldFlippedTileCount, int>(ms); }
            public static int      GetMeldCode(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldCode, int>(ms); }
            public static int      GetMeldSimpleFu(this MeldState ms)     { return EnumAttributes.GetAttributeValue<MeldSimpleFu, int>(ms); }
            public static int      GetMeldNonSimpleFu(this MeldState ms)  { return EnumAttributes.GetAttributeValue<MeldNonSimpleFu, int>(ms); }
            public static bool     TryGetMeldState(string value, out MeldState ms) { return EnumHelper.TryGetEnumByCode<MeldState, MeldCode>(value, out ms); }
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
        MeldState       State             { get; }
        CalledDirection Direction         { get; }
        ITile[]         Tiles             { get; }
        TileType        CalledTile        { get; }
        int             RedDoraCount      { get; }

        void Promote(TileType kanTile, int kanTileSlot);
    }

    public static class MeldFactory
    {
        public static IMeld BuildChii(Player callee, TileType calledTile, TileType tileLo, TileType tileHi, int calledSlot, int slotLo, int slotHi)
        {

        }

        public static IMeld BuildPon(Player caller,
                                     Player callee,
                                     TileType calledTile,
                                     TileType tileA,
                                     TileType tileB,
                                     int calledSlot,
                                     int slotA,
                                     int slotB)
        {

        }

        public static IMeld BuildOpenKan(Player caller,
                                         Player callee,
                                         TileType calledTile,
                                         TileType tileA,
                                         TileType tileB,
                                         TileType tileC,
                                         int calledSlot,
                                         int slotA,
                                         int slotB,
                                         int slotC)
        {

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

        }
    }
}
