// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using MahjongCore.Riichi.Impl.AI;

namespace MahjongCore.Riichi
{
    public enum InboxAIType
    {
        Random
    }

    public interface IPlayerAI
    {
        // Gets called when a new round begins. Does not get called when loading a game from a saved game state.
        void RoundStarted(Round round);

        // Make a decision when it's your turn. Decide to discard normally, reach, tsumo, make a kan, or make an abortive draw.
        // You are told whether or not your draw came from the wall, the dead wall after a kan, or via calling pon/chii/open kan.
        // If you are in reach and you decide to discard, the selected tile is ignored. Discarding a suufurendan tile regardless of
        // What IDiscardDecision.Decision is set to will return in an abortive draw.
        IDiscardDecision GetDiscardDecision(DiscardInfo info);

        // Make a decision based on the most recently discard tile by another players. Should decide to chii, pon, open kan, ron, or chankan.
        IPostDiscardDecision GetPostDiscardDecision(PostDiscardInfo info);
    }

    public static class InboxAIFactory
    {
        public static IPlayerAI BuildAI(InboxAIType type)
        {
            IPlayerAI ai = null;
            switch(type)
            {
                case InboxAIType.Random: ai = new AIRandom();
                                         break;
                default:                 throw new Exception("Not supported!");
            }
            return ai;
        }
    }
}
