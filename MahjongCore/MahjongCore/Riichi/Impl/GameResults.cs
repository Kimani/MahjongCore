// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Evaluator;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public class RankOrder : IComparable<RankOrder>
    {
        public Player Seat;
        public int    Points;
        public int    SeatOrder;
        public int    YakitoriDelta;
        public float  Score;

        public RankOrder(Player seat, int points, int initSeatOrder, int yakitoriDelta)
        {
            Seat = seat;
            Points = points;
            SeatOrder = initSeatOrder;
            YakitoriDelta = yakitoriDelta;
            Score = 0.0f;
        }

        public int CompareTo(RankOrder o)
        {
            return (Points > o.Points)       ? -1 :
                   (Points < o.Points)       ? 1 :
                   (SeatOrder < o.SeatOrder) ? -1 :
                                               1;
        }
    }

    public class GameResults
    {
        public int       FinalPointsPlayer1;
        public int       FinalPointsPlayer2;
        public int       FinalPointsPlayer3;
        public int       FinalPointsPlayer4;
        public int       YakitoriDeltaPlayer1;
        public int       YakitoriDeltaPlayer2;
        public int       YakitoriDeltaPlayer3;
        public int       YakitoriDeltaPlayer4;
        public Placement Player1FinalPlacement;
        public Placement Player2FinalPlacement;
        public Placement Player3FinalPlacement;
        public Placement Player4FinalPlacement;
        public float     FinalScorePlayer1;
        public float     FinalScorePlayer2;
        public float     FinalScorePlayer3;
        public float     FinalScorePlayer4;

        public GameResults(GameSettings settings,
                           Player startingDealer,
                           int pointsP1,
                           int pointsP2,
                           int pointsP3,
                           int pointsP4,
                           int extraWinnerPoints,
                           bool yakitoriP1,
                           bool yakitoriP2,
                           bool yakitoriP3,
                           bool yakitoriP4)
        {
            int finalScoreP1 = pointsP1;
            int finalScoreP2 = pointsP2;
            int finalScoreP3 = pointsP3;
            int finalScoreP4 = pointsP4;

            // Figure out the yakitori deltas.
            int yakitoriPool = 0;
            int yakitoriDelta1 = 0;
            int yakitoriDelta2 = 0;
            int yakitoriDelta3 = 0;
            int yakitoriDelta4 = 0;
            Yakitori yakitoriSetting = settings.GetSetting<Yakitori>(GameOption.YakitoriOption);

            if (yakitoriSetting != Yakitori.Yakitori_Disabled)
            {
                int yakitoriPenalty = yakitoriSetting.GetDelta();
                if (yakitoriP1)
                {
                    yakitoriDelta1 -= yakitoriPenalty;
                    finalScoreP1 -= yakitoriPenalty;
                    yakitoriPool += yakitoriPenalty;
                }

                if (yakitoriP2)
                {
                    yakitoriDelta2 -= yakitoriPenalty;
                    finalScoreP2 -= yakitoriPenalty;
                    yakitoriPool += yakitoriPenalty;
                }

                if (yakitoriP3)
                {
                    yakitoriDelta3 -= yakitoriPenalty;
                    finalScoreP3 -= yakitoriPenalty;
                    yakitoriPool += yakitoriPenalty;
                }

                if (yakitoriP4)
                {
                    yakitoriDelta4 -= yakitoriPenalty;
                    finalScoreP4 -= yakitoriPenalty;
                    yakitoriPool += yakitoriPenalty;
                }
            }

            // Use knowledge of the first dealer to figure out final rankings.
            List<RankOrder> ranks = new List<RankOrder>();
            ranks.Add(new RankOrder(Player.Player1, pointsP1, (1 - startingDealer.GetZeroIndex()) + (((1 - startingDealer.GetZeroIndex()) < 0) ? 4 : 0), yakitoriDelta1));
            ranks.Add(new RankOrder(Player.Player2, pointsP2, (2 - startingDealer.GetZeroIndex()) + (((2 - startingDealer.GetZeroIndex()) < 0) ? 4 : 0), yakitoriDelta2));
            ranks.Add(new RankOrder(Player.Player3, pointsP3, (3 - startingDealer.GetZeroIndex()) + (((3 - startingDealer.GetZeroIndex()) < 0) ? 4 : 0), yakitoriDelta3));
            ranks.Add(new RankOrder(Player.Player4, pointsP4, (4 - startingDealer.GetZeroIndex()) + (((4 - startingDealer.GetZeroIndex()) < 0) ? 4 : 0), yakitoriDelta4));
            ranks.Sort();

            // Give extra points (like remaining reach sticks) to the winner.
            ranks[0].Points += extraWinnerPoints;

            // Give the Yakitori pool to the winner if applicable.
            if ((yakitoriPool > 0) && (EnumAttributes.GetAttributeValue<Place1Value, int>(yakitoriSetting) > 0))
            {
                ranks[0].Points += yakitoriPool;
                ranks[0].YakitoriDelta += yakitoriPool;
            }

            // Get the final scores.
            float[] scores = new float[4];
            RiichiScoring.GetScores(settings, ranks[0].Points, ranks[1].Points, ranks[2].Points, ranks[3].Points, scores);
            ranks[0].Score = scores[0];
            ranks[1].Score = scores[1];
            ranks[2].Score = scores[2];
            ranks[3].Score = scores[3];

            // Generate a GameResults and submit it to GameComplete.
            SetPlayerData(ranks[0], Placement.Place1);
            SetPlayerData(ranks[1], Placement.Place2);
            SetPlayerData(ranks[2], Placement.Place3);
            SetPlayerData(ranks[3], Placement.Place4);
        }

        public int GetPoints(Player p)
        {
            RiichiGlobal.Assert(p.IsPlayer());
            return (p == Player.Player1) ? FinalPointsPlayer1 :
                   (p == Player.Player2) ? FinalPointsPlayer2 :
                   (p == Player.Player3) ? FinalPointsPlayer3 :
                                           FinalPointsPlayer4;
        }

        public float GetScore(Player p)
        {
            RiichiGlobal.Assert(p.IsPlayer());
            return (p == Player.Player1) ? FinalScorePlayer1 :
                   (p == Player.Player2) ? FinalScorePlayer2 :
                   (p == Player.Player3) ? FinalScorePlayer3 :
                                           FinalScorePlayer4;
        }

        public int GetPoints(Placement p)
        {
            return (Player1FinalPlacement == p) ? FinalPointsPlayer1 :
                   (Player2FinalPlacement == p) ? FinalPointsPlayer2 :
                   (Player3FinalPlacement == p) ? FinalPointsPlayer3 :
                                                  FinalPointsPlayer4;
        }

        public float GetScore(Placement p)
        {
            return (Player1FinalPlacement == p) ? FinalScorePlayer1 :
                   (Player2FinalPlacement == p) ? FinalScorePlayer2 :
                   (Player3FinalPlacement == p) ? FinalScorePlayer3 :
                                                  FinalScorePlayer4;
        }

        public Player GetPlayer(Placement p)
        {
            return (Player1FinalPlacement == p) ? Player.Player1 :
                   (Player2FinalPlacement == p) ? Player.Player2 :
                   (Player3FinalPlacement == p) ? Player.Player3 :
                                                  Player.Player4;
        }

        private void SetPlayerData(RankOrder data, Placement place)
        {
            if      (data.Seat == Player.Player1) { Player1FinalPlacement = place; FinalScorePlayer1 = data.Score; YakitoriDeltaPlayer1 = data.YakitoriDelta; FinalPointsPlayer1 = data.Points; }
            else if (data.Seat == Player.Player2) { Player2FinalPlacement = place; FinalScorePlayer2 = data.Score; YakitoriDeltaPlayer2 = data.YakitoriDelta; FinalPointsPlayer2 = data.Points; }
            else if (data.Seat == Player.Player3) { Player3FinalPlacement = place; FinalScorePlayer3 = data.Score; YakitoriDeltaPlayer3 = data.YakitoriDelta; FinalPointsPlayer3 = data.Points; }
            else                                  { Player4FinalPlacement = place; FinalScorePlayer4 = data.Score; YakitoriDeltaPlayer4 = data.YakitoriDelta; FinalPointsPlayer4 = data.Points; }
        }
    }
}
