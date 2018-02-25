// [Ready Design Corps] - [Mahjong SKY Client] - Copyright 2017

using System;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi;

namespace MahjongCore.Riichi.Attributes
{
    public class CommandMeldState : Attribute, IAttribute<MeldState>
    {
        public MeldState Value { set; get; }

        public CommandMeldState(MeldState v)
        {
            Value = v;
        }
    }
}
