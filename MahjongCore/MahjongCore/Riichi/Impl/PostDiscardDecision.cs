// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Impl
{
    internal class PostDiscardDecision
    {

        public Decision DecisionToMake = Decision.Nothing;
        public CallOption CallToMake = null;

        public bool Validate()
        {
            // Make sure we have a valid call.
            if (DecisionToMake == Decision.Call)
            {
                if (CallToMake == null) { return false; }
                if (CallToMake.Type == MeldState.None) { return false; }
                if (CallToMake.Type == MeldState.KanConcealed) { return false; }
                if (CallToMake.Type == MeldState.KanPromoted) { return false; }
            }
            return true;
        }
    }
}
