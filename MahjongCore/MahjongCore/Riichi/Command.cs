// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;

namespace MahjongCore.Riichi
{
    #region CommandType
        public enum CommandType
        {
            [CommandMeldState(MeldState.None)]         None,
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

        public static class TileCommandTypeExtensionMethods
        {
            public static MeldState GetState(this CommandType t) { return EnumAttributes.GetAttributeValue<CommandMeldState, MeldState>(t); }
        }
    #endregion

    public interface ICommand : ICloneable
    {
        CommandType Command { get; }
        ITile       Tile    { get; }
        TileType    TileB   { get; }
        TileType    TileC   { get; }
    }
}
