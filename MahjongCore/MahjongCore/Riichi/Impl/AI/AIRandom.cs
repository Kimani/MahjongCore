// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl.AI
{
    public class AIRandom : IPlayerAI
    {
        DiscardDecisionImpl stashedDecision = new DiscardDecisionImpl();
        PostDiscardDecisionImpl stashedPostDecision = new PostDiscardDecisionImpl();

        public void RoundStarted(Round round) { }

        public IDiscardDecision GetDiscardDecision(IDiscardInfo info)
        {
            stashedDecision.Reset();

            if (info.CanTsumo)
            {
                stashedDecision.Decision = DiscardDecisionType.Tsumo;
            }
            else if (info.Hand.CouldKyuushuuKyuuhai)
            {
                stashedDecision.Decision = DiscardDecisionType.AbortiveDraw;
            }
            else if (info.CanReach)
            {
                int riichiSlot = -1;
                ITile[] activeHand = info.Hand.ActiveHand;

                for (int i = 0; i < activeHand.Length; ++i)
                {
                    IList<TileType> waits = info.Hand.GetWaitsForDiscard(i);
                    if ((waits == null) || (waits.Count > 0))
                    {
                        riichiSlot = i;
                        break;
                    }
                }

                stashedDecision.Decision = DiscardDecisionType.RiichiDiscard;
                stashedDecision.Tile = activeHand[riichiSlot];
            }
            else if (info.ClosedKanTiles.Count > 0)
            {
                stashedDecision.Decision = DiscardDecisionType.ClosedKan;
                stashedDecision.Tile = info.Hand.ActiveHand[info.Hand.GetTileSlot(info.ClosedKanTiles[0], false)];
            }
            else if (info.PromotedKanTiles.Count > 0)
            {
                stashedDecision.Decision = DiscardDecisionType.PromotedKan;
                stashedDecision.Tile = info.Hand.ActiveHand[info.Hand.GetTileSlot(info.PromotedKanTiles[0], false)];
            }
            else
            {
                stashedDecision.Decision = DiscardDecisionType.Discard;
                do
                {
                    stashedDecision.Tile = info.Hand.ActiveHand[Global.RandomRange(0, info.Hand.ActiveTileCount)];
                }
                while (info.RestrictedTiles.Contains(stashedDecision.Tile.Type));
            }
            return stashedDecision;
        }

        public IPostDiscardDecision GetPostDiscardDecision(IPostDiscardInfo info)
        {
            stashedPostDecision.Reset();

            // Ron if the option is available.
            if (info.CanRon)
            {
                stashedPostDecision.Decision = PostDiscardDecisionType.Ron;
            }
            else if (info.Calls.Count > 0)
            {
                // Make a call if available. Prioritize open kan.
                stashedPostDecision.Decision = PostDiscardDecisionType.Call;
                stashedPostDecision.Call = info.Calls[0];
                for (int i = 1; i < info.Calls.Count; ++i)
                {
                    IMeld call = info.Calls[i];
                    if (call.State == MeldState.KanOpen)
                    {
                        stashedPostDecision.Call = call;
                        break;
                    }
                }
            }
            else
            {
                stashedPostDecision.Decision = PostDiscardDecisionType.Nothing;
            }
            return stashedPostDecision;
        }
    }
}
