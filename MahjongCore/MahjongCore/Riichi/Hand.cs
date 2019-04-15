// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Attributes;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public enum OverrideHand
    {
        [OptionValueType(typeof(int))]         Score,
        [OptionValueType(typeof(IndexedMeld))] Meld,
        [OptionValueType(typeof(ReachType))]   Reach,
        [OptionValueType(typeof(TileType[]))]  Waits,
        [OptionValueType(typeof(int))]         Chombo
    }

    public interface IHand : IComparable<IHand>
    {
        event Action<Player>                   Sorted;
        event Action<Player, ITile[]>          TilesAdded;
        event Action<Player, IMeld>            Called;
        event Action<Player, ITile, ReachType> Reached;
        event Action<Player, ITile>            Discarded;
        event Action<Player, TileType>         DiscardUndone;

        IGameState      Parent               { get; }
        Player          Player               { get; }
        Wind            Seat                 { get; }
        ITile[]         Tiles                { get; }
        IMeld[]         Melds                { get; }
        IList<ITile>    Discards             { get; }
        IList<TileType> Waits                { get; }
        IList<ICommand> DrawsAndKans         { get; }
        ReachType       Reach                { get; }
        int             Score                { get; }
        int             TileCount            { get; }
        int             Streak               { get; }
        int             MeldCount            { get; }
        int             MeldedTileCount      { get; }
        int             KanCount             { get; }
        int             Chombo               { get; }
        bool            Dealer               { get; }
        bool            Open                 { get; }
        bool            Closed               { get; }
        bool            Tempai               { get; }
        bool            Furiten              { get; }
        bool            Yakitori             { get; }
        bool            HasFullHand          { get; }
        bool            CouldIppatsu         { get; }
        bool            CouldDoubleReach     { get; }
        bool            CouldKyuushuuKyuuhai { get; }
        bool            CouldSuufurendan     { get; }

        bool            WouldMakeFuriten(int slot);
        int             GetTileSlot(TileType tile, bool matchRed);
        void            MoveTileToEnd(TileType targetTile);                                // TODO: Replace with SubmitOverride
        void            ReplaceTiles(List<TileType> tilesRemove, List<TileType> tilesAdd); // TODO: Replace with SubmitOverride
        IList<TileType> GetWaitsForDiscard(int slot);
        void            SubmitOverride(OverrideHand key, object value);
    }
}
