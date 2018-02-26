// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public enum MeldType
    {
        None,
        Chii,
        Pon,
        Kan
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
            [MeldTypeAttribute(MeldType.None), MeldTileCount(0), MeldOpen(false), MeldFlippedTileCount(0), MeldCalled(false), MeldCode(0), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  None,
            [MeldTypeAttribute(MeldType.Chii), MeldTileCount(3), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(1), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  Chii,
            [MeldTypeAttribute(MeldType.Pon),  MeldTileCount(3), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(2), MeldSimpleFu(2),  MeldNonSimpleFu(4)]  Pon,
            [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(3), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanOpen,
            [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(false), MeldFlippedTileCount(2), MeldCalled(true),  MeldCode(4), MeldSimpleFu(16), MeldNonSimpleFu(32)] KanConcealed,
            [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(5), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanPromoted
        }

        public static class MeldStateExtensionMethods
        {
            public static MeldType GetMeldType(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms); }
            public static bool     IsOpen(this MeldState ms)              { return EnumAttributes.GetAttributeValue<MeldOpen, bool>(ms); }
            public static bool     IsCalled(this MeldState ms)            { return EnumAttributes.GetAttributeValue<MeldCalled, bool>(ms); }
            public static int      GetTileCount(this MeldState ms)        { return EnumAttributes.GetAttributeValue<MeldTileCount, int>(ms); }
            public static int      GetFlippedTileCount(this MeldState ms) { return EnumAttributes.GetAttributeValue<MeldFlippedTileCount, int>(ms); }
            public static int      GetMeldCode(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldCode, int>(ms); }
            public static int      GetMeldSimpleFu(this MeldState ms)     { return EnumAttributes.GetAttributeValue<MeldSimpleFu, int>(ms); }
            public static int      GetMeldNonSimpleFu(this MeldState ms)  { return EnumAttributes.GetAttributeValue<MeldNonSimpleFu, int>(ms); }

            public static bool TryGetMeldState(string value, out MeldState ms)
            {
                ms = MeldState.None;
                bool found = false;
                int code;
                if (int.TryParse(value, out code))
                {
                    foreach (MeldState stateTest in Enum.GetValues(typeof(MeldState)))
                    {
                        if (stateTest.GetMeldCode() == code)
                        {
                            found = true;
                            ms = stateTest;
                            break;
                        }
                    }
                }
                return found;
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

    public interface IMeld : ICloneable
    {
        MeldState      State             { get; }
        ExtendedTile[] Tiles             { get; }
        TileType       CalledTile        { get; }
        int            SourceDiscardSlot { get; set; }
        int            RedDoraCount      { get; }

        bool Equals(IMeld meld);
    }

    public interface IHand
    {
        Player             Player                { get; }
        TileType           Seat                  { get; }
        TileType[]         ActiveHand            { get; }
        IMeld[]            Melds                 { get; }
        int                Score                 { get; }
        int                ActiveTileCount       { get; }
        int                Streak                { get; }
        int                MeldCount             { get; }
        int                OpenMeldCount         { get; }
        int                MeldedTileCount       { get; }
        int                KanCount              { get; }
        bool               Dealer                { get; }
        bool               Open                  { get; }
        bool               Closed                { get; }
        bool               Tempai                { get; }
        bool               InReach               { get; }
        bool               InDoubleReach         { get; }
        bool               InOpenReach           { get; }
        bool               Furiten               { get; }
        bool               Yakitori              { get; }
        bool               HasFullHand           { get; }
        bool               CanIppatsu            { get; }
        bool               CanDoubleReach        { get; }
        bool               CanKyuushuuKyuuhai    { get; }
        bool               CanSuufurendan        { get; }
        List<TileType>     Waits                 { get; }
        Stack<TileCommand> DrawsAndKans          { get; }
        List<ExtendedTile> Discards              { get; }
        List<CallOption>   AvailableCalls        { get; }

        int  GetTileSlot(TileType tile, bool matchRed);
        void MoveTileToEnd(TileType targetTile);
        void ReplaceTiles(List<TileType> tilesRemove, List<TileType> tilesAdd);
    }
}
