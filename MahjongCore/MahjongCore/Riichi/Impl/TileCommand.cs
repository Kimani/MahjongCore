// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Common.Attributes;

namespace MahjongCore.Riichi
{
    public static class TileCommandTypeExtensionMethods
    {
        public static MeldState GetState(this TileCommand.Type t) { return EnumAttributes.GetAttributeValue<CommandMeldState, MeldState>(t); }
    }

    public class TileCommand : ICloneable
    {
        public enum Type
        {
            [CommandMeldState(MeldState.None)]         Tile,
            [CommandMeldState(MeldState.Pon)]          Pon,
            [CommandMeldState(MeldState.Chii)]         Chii,
            [CommandMeldState(MeldState.KanOpen)]      OpenKan,
            [CommandMeldState(MeldState.KanPromoted)]  PromotedKan,
            [CommandMeldState(MeldState.KanConcealed)] ClosedKan,
            [CommandMeldState(MeldState.None)]         CallPass,
            [CommandMeldState(MeldState.None)]         RonPass,
            [CommandMeldState(MeldState.None)]         TsumoPass
        }

        public  Type         CommandType { get; private set; }
        public  ExtendedTile TilePrimary { get; private set; }
        private TileType     TileB = TileType.None;
        private TileType     TileC = TileType.None;

        public TileCommand(Type t, ExtendedTile tile) : this(t, tile, TileType.None, TileType.None) { }
        public TileCommand(TileCommand tc)            : this(tc.CommandType, (ExtendedTile)tc.TilePrimary.Clone(), tc.TileB, tc.TileC) { }
        public override String ToString()             { return "(TileCommand: " + CommandType + " Tile " + TilePrimary + ")"; }

        public TileCommand(Type t)
        {
            RiichiGlobal.Assert((t == Type.CallPass) || (t == Type.RonPass) || (t == Type.TsumoPass) || (t == Type.PromotedKan) || (t == Type.ClosedKan));
            CommandType = t;
            TilePrimary = new ExtendedTile(TileType.None);
        }

        public TileCommand(Type t, ExtendedTile tilePrimary, TileType tileB, TileType tileC)
        {
            CommandType = t;
            TilePrimary = tilePrimary;
            TileB = tileB;
            TileC = tileC;
        }
 
        public void PromotePrimaryTileToRed()
        {
            RiichiGlobal.Assert(((TilePrimary.Tile.GetValue() == 5) && (CommandType == Type.Tile)), "Promoting a tile to red that isn't a regular 5!!");
            TilePrimary = new ExtendedTile(TilePrimary.Tile.GetRedDoraVersion());
        }

        // ICloneable
        public object Clone() { return new TileCommand(this); }
    }
}
