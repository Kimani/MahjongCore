// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public interface IHand
    {
        IGameState         Parent                { get; }
        Player             Player                { get; }
        Wind               Seat                  { get; }
        ITile[]            ActiveHand            { get; }
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
        bool               CouldIppatsu          { get; }
        bool               CouldDoubleReach      { get; }
        bool               CouldKyuushuuKyuuhai  { get; }
        bool               CouldSuufurendan      { get; }
        IList<TileType>    Waits                 { get; }
        IList<TileCommand> DrawsAndKans          { get; }
        IList<ITile>       Discards              { get; }
        IList<IMeld>       AvailableCalls        { get; }

        int             GetTileSlot(TileType tile, bool matchRed);
        void            MoveTileToEnd(TileType targetTile);
        void            ReplaceTiles(List<TileType> tilesRemove, List<TileType> tilesAdd);
        IList<TileType> GetWaitsForDiscard(int slot);
    }
}
