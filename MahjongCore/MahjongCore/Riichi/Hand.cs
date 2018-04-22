// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi
{
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
