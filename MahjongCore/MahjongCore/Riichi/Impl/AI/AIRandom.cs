// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl.AI
{
    public class AIRandom : IPlayerAI
    {
        DiscardDecisionImpl stashedDecision = new DiscardDecisionImpl();
        PostDiscardDecisionImpl stashedPostDecision = new PostDiscardDecisionImpl();

        public void Initialize(Player player) { }
        public void StartRound(Round round)   { }

        public IDiscardDecision GetDiscardDecision(IDiscardInfo info)
        {
            if (info.CanTsumo)
            {
                stashedDecision.Decision = DiscardDecisionType.Tsumo;
                stashedDecision.Tile = TileType.None;
            }
            else if (info.CanKyuushuuKyuuhai)
            {
                stashedDecision.Decision = DiscardDecisionType.AbortiveDraw;
                stashedDecision.Tile = TileType.None;
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
                stashedDecision.Slot = riichiSlot;
            }
            else if (info.ClosedKanTiles.Count > 0)
            {
                stashedDecision.Decision = DiscardDecisionType.ClosedKan;
                stashedDecision.Tile = info.ClosedKanTiles[0];
                stashedDecision.Slot = info.Hand.GetTileSlot(stashedDecision.Tile, false);
            }
            else if (info.PromotedKanTiles.Count > 0)
            {
                stashedDecision.Decision = DiscardDecisionType.PromotedKan;
                stashedDecision.Tile = info.PromotedKanTiles[0];
                stashedDecision.Slot = info.Hand.GetTileSlot(stashedDecision.Tile, false);
            }
            else
            {
                stashedDecision.Decision = DiscardDecisionType.Discard;
                do
                {
                    stashedDecision.Slot = Global.RandomRange(0, info.Hand.ActiveTileCount);
                    stashedDecision.Tile = info.Hand.ActiveHand[stashedDecision.Slot];
                }
                while (info.RestrictedTiles.Contains(stashedDecision.Tile));
            }
            return stashedDecision;
        }

        public IPostDiscardDecision GetPostDiscardDecision(IPostDiscardInfo info)
        {
            PostDiscardDecision decision = new PostDiscardDecision();

            // Ron if the option is available.
            if (info.CanRon)
            {
                decision.Decision = PostDiscardDecisionType.Ron;
                decision.Call = null;
            }
            else if (info.Calls.Count > 0)
            {
                // Make a call if available. Prioritize open kan.
                decision.Decision = PostDiscardDecisionType.Call;
                decision.Call = info.Calls[0];
                for (int i = 1; i < info.Calls.Count; ++i)
                {
                    IMeld call = info.Calls[i];
                    if (call.State == MeldState.KanOpen)
                    {
                        decision.Call = call;
                        break;
                    }
                }
            }
            else
            {
                decision.Decision = PostDiscardDecisionType.Nothing;
                decision.Call = null;
            }
            return decision;
        }
    }
}
