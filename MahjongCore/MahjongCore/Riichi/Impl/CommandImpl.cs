// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Riichi.Impl;

namespace MahjongCore.Riichi
{
    internal class CommandImpl : ICommand
    {
        // ICommand
        public CommandType Command { get; internal set; } = CommandType.None;
        public ITile       Tile    { get; internal set; }
        public TileType    TileB   { get; internal set; } = TileType.None;
        public TileType    TileC   { get; internal set; } = TileType.None;

        // ICloneable
        public object Clone() { return new CommandImpl(Command, Tile, TileB, TileC); }

        // IComparable<ICommand>
        public int CompareTo(ICommand other)
        {
            int value = Command.CompareTo(other.Command);     if (value != 0) { return value; }
            value = (Tile as TileImpl).CompareTo(other.Tile); if (value != 0) { return value; }
            value = TileB.CompareTo(other.TileB);             if (value != 0) { return value; }
            value = TileC.CompareTo(other.TileC);             if (value != 0) { return value; }
            return 0;
        }

        // CommandImpl
        public CommandImpl(CommandType ct, ITile tile)    : this(ct, tile, TileType.None, TileType.None)                      { }
        public CommandImpl(CommandType ct, TileType tile) : this(ct, new TileImpl(tile), TileType.None, TileType.None, false) { }
        public override String ToString()                 { return "(TileCommand: " + Command + " Tile " + Tile + ")"; }

        public CommandImpl(CommandType t)
        {
            Global.Assert((t == CommandType.CallPass) ||
                          (t == CommandType.RonPass) ||
                          (t == CommandType.TsumoPass) ||
                          (t == CommandType.PromotedKan) ||
                          (t == CommandType.ClosedKan));
            Command = t;
            Tile = new TileImpl(TileType.None);
        }

        public CommandImpl(CommandType t, ITile tilePrimary, TileType tileB, TileType tileC, bool clonePrimary = true)
        {
            Command = t;
            Tile = (clonePrimary ? (tilePrimary.Clone() as ITile) : tilePrimary);
            TileB = tileB;
            TileC = tileC;
        }

        public void PromotePrimaryTileToRed()
        {
            Global.Assert(((Tile.Type.GetValue() == 5) && (Command == CommandType.Tile)), "Promoting a tile to red that isn't a regular 5!!");
            Tile = new TileImpl(Tile.Type.GetRedDoraVersion());
        }
    }
}
