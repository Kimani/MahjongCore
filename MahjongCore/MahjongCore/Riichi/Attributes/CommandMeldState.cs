// [Ready Design Corps] - [Mahjong SKY Client] - Copyright 2017

using MahjongCore.Common.Attributes;
using System;

namespace MahjongCore.Riichi.Attributes
{
    public class CommandMeldState : Attribute, IAttribute<MeldState>
    {
        public MeldState Value               { get; set; }
        public CommandMeldState(MeldState v) { Value = v; }
    }
}
