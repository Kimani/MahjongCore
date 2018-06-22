// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Impl;
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

    internal class GameResultsImpl : IGameResults
    {
        // IGameResults
        public int       FinalPointsPlayer1    { get; internal set; }
        public int       FinalPointsPlayer2    { get; internal set; }
        public int       FinalPointsPlayer3    { get; internal set; }
        public int       FinalPointsPlayer4    { get; internal set; }
        public int       YakitoriDeltaPlayer1  { get; internal set; }
        public int       YakitoriDeltaPlayer2  { get; internal set; }
        public int       YakitoriDeltaPlayer3  { get; internal set; }
        public int       YakitoriDeltaPlayer4  { get; internal set; }
        public Placement FinalPlacementPlayer1 { get; internal set; }
        public Placement FinalPlacementPlayer2 { get; internal set; }
        public Placement FinalPlacementPlayer3 { get; internal set; }
        public Placement FinalPlacementPlayer4 { get; internal set; }
        public float     FinalScorePlayer1     { get; internal set; }
        public float     FinalScorePlayer2     { get; internal set; }
        public float     FinalScorePlayer3     { get; internal set; }
        public float     FinalScorePlayer4     { get; internal set; }

        public int GetPoints(Player p)
        {
            Global.Assert(p.IsPlayer());
            return (p == Player.Player1) ? FinalPointsPlayer1 :
                   (p == Player.Player2) ? FinalPointsPlayer2 :
                   (p == Player.Player3) ? FinalPointsPlayer3 :
                                           FinalPointsPlayer4;
        }

        public float GetScore(Player p)
        {
            Global.Assert(p.IsPlayer());
            return (p == Player.Player1) ? FinalScorePlayer1 :
                   (p == Player.Player2) ? FinalScorePlayer2 :
                   (p == Player.Player3) ? FinalScorePlayer3 :
                                           FinalScorePlayer4;
        }

        public int GetPoints(Placement p)
        {
            return (FinalPlacementPlayer1 == p) ? FinalPointsPlayer1 :
                   (FinalPlacementPlayer2 == p) ? FinalPointsPlayer2 :
                   (FinalPlacementPlayer3 == p) ? FinalPointsPlayer3 :
                                                  FinalPointsPlayer4;
        }

        public float GetScore(Placement p)
        {
            return (FinalPlacementPlayer1 == p) ? FinalScorePlayer1 :
                   (FinalPlacementPlayer2 == p) ? FinalScorePlayer2 :
                   (FinalPlacementPlayer3 == p) ? FinalScorePlayer3 :
                                                  FinalScorePlayer4;
        }

        public Player GetPlayer(Placement p)
        {
            return (FinalPlacementPlayer1 == p) ? Player.Player1 :
                   (FinalPlacementPlayer2 == p) ? Player.Player2 :
                   (FinalPlacementPlayer3 == p) ? Player.Player3 :
                                                  Player.Player4;
        }

        // GameResultsImpl
        public GameResultsImpl(IGameSettings settings,
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

        private void SetPlayerData(RankOrder data, Placement place)
        {
            if      (data.Seat == Player.Player1) { FinalPlacementPlayer1 = place; FinalScorePlayer1 = data.Score; YakitoriDeltaPlayer1 = data.YakitoriDelta; FinalPointsPlayer1 = data.Points; }
            else if (data.Seat == Player.Player2) { FinalPlacementPlayer2 = place; FinalScorePlayer2 = data.Score; YakitoriDeltaPlayer2 = data.YakitoriDelta; FinalPointsPlayer2 = data.Points; }
            else if (data.Seat == Player.Player3) { FinalPlacementPlayer3 = place; FinalScorePlayer3 = data.Score; YakitoriDeltaPlayer3 = data.YakitoriDelta; FinalPointsPlayer3 = data.Points; }
            else                                  { FinalPlacementPlayer4 = place; FinalScorePlayer4 = data.Score; YakitoriDeltaPlayer4 = data.YakitoriDelta; FinalPointsPlayer4 = data.Points; }
        }
    }

    internal class WinResultsImpl : IWinResults
    {
        // IWinResults
        public Player         WinningPlayer    { get; internal set; }
        public ICandidateHand WinningHand      { get; internal set; }
        public WinType        Action           { get; internal set; }
        public int            ScoreHi          { get; internal set; }
        public int            ScoreLo          { get; internal set; }
        public int            Player1Delta     { get; internal set; }
        public int            Player2Delta     { get; internal set; }
        public int            Player3Delta     { get; internal set; }
        public int            Player4Delta     { get; internal set; }
        public int            Player1PoolDelta { get; internal set; }
        public int            Player2PoolDelta { get; internal set; }
        public int            Player3PoolDelta { get; internal set; }
        public int            Player4PoolDelta { get; internal set; }
        public bool           Limit            { get; internal set; }

        // WinResultsImpl
        public void Reset()
        {
            WinningPlayer = Player.None;
            WinningHand = null;
            Action = WinType.Draw;
            ResetScores();
        }

        public void ResetScores()
        {
            ScoreHi = 0;
            ScoreLo = 0;
            Player1Delta = 0;
            Player2Delta = 0;
            Player3Delta = 0;
            Player4Delta = 0;
            Player1PoolDelta = 0;
            Player2PoolDelta = 0;
            Player3PoolDelta = 0;
            Player4PoolDelta = 0;
            Limit = false;
        }

        public int GetPlayerDelta(Player p)
        {
            return (p == Player.Player1) ? Player1Delta :
                   (p == Player.Player2) ? Player2Delta :
                   (p == Player.Player3) ? Player3Delta :
                                           Player4Delta;
        }

        public void SetPlayerScore(Player p, int score)
        {
            Global.Assert(p.IsPlayer());
            if      (p == Player.Player1) { Player1Delta = score; }
            else if (p == Player.Player2) { Player2Delta = score; }
            else if (p == Player.Player3) { Player3Delta = score; }
            else if (p == Player.Player4) { Player4Delta = score; }
        }

        public void SetPlayerPool(Player p, int pool)
        {
            Global.Assert(p.IsPlayer());
            if      (p == Player.Player1) { Player1PoolDelta = pool; }
            else if (p == Player.Player2) { Player2PoolDelta = pool; }
            else if (p == Player.Player3) { Player3PoolDelta = pool; }
            else if (p == Player.Player4) { Player4PoolDelta = pool; }
        }

        public void Populate(IGameState state, Player winner, Player target, Player recentOpenKan, WinType action, int pool)
        {
            Reset();
            WinningPlayer = winner;
            Action = action;

            GameStateImpl stateImpl = state as GameStateImpl;
            HandImpl hand = stateImpl.GetHand(winner) as HandImpl;
            if ((action == WinType.Ron) || (action == WinType.Tsumo))
            {
                // Mark the win. Advance streak and mark as not yakitori.
                hand.Streak++;
                hand.Yakitori = false;

                WinningHand = hand.WinningHandCache;

                // Can only have sekinin barai in multi-win scenarios if there's a ron. So don't calculate if this is a tsumo.
                Player sekininBarai1 = Player.None;
                Player sekininBarai2 = Player.None;
                if (action != WinType.Tsumo)
                {
                    CheckSekininBarai(state, (action == WinType.Ron), WinningHand, hand, recentOpenKan, out sekininBarai1, out sekininBarai2);
                }

                // Calculate the scores.
                RiichiScoring.GetWinningScore(hand, target, sekininBarai1, sekininBarai2, WinningHand.Han, WinningHand.Fu, pool, this);

                AdjustForPaaRenchan(hand, (action == WinType.Ron));
            }
            else
            {
                hand.Streak = 0;
            }
        }

        private void AdjustForPaaRenchan(IHand hand, bool fRon)
        {
            int paaRenchanValue = Riichi.Yaku.PaaRenchan.Evaluate(hand, WinningHand, fRon);
            if (paaRenchanValue != 0)
            {
                // Paa renchan is already built into the score of WinningHand. We just need to adjust dora and yaku.
                WinningHand.Dora = 0;
                WinningHand.UraDora = 0;
                WinningHand.RedDora = 0;
                WinningHand.CleanNonYakumanYaku();
                WinningHand.Yaku.Add(Yaku.PaaRenchan);
            }
        }

        private void CheckSekininBarai(IGameState state, bool fRon, CandidateHand winningHand, Hand hand, Player recentOpenKan, out Player targetPlayer1, out Player targetPlayer2)
        {
            targetPlayer1 = Player.None;
            targetPlayer2 = Player.None;

            if (state.Settings.GetSetting<bool>(GameOption.SekininBaraiRinshan) &&
                !fRon &&
                (recentOpenKan != Player.None) &&
                (winningHand != null) &&
                (winningHand.Yaku.Contains(Yaku.RinshanKaihou) || winningHand.Yaku.Contains(Yaku.UupinKaihou)))
            {
                targetPlayer1 = recentOpenKan;
            }

            if (state.Settings.GetSetting<bool>(GameOption.SekininBaraiDaisangen) && winningHand.Yaku.Contains(Yaku.Daisangen))
            {
                TileType om3t = hand.OpenMeld[2].Tiles[0].Tile;
                ExtendedTile[] thirdMeld = om3t.IsDragon() ? hand.OpenMeld[2].Tiles : hand.OpenMeld[3].Tiles;
                MeldState thirdMeldState = om3t.IsDragon() ? hand.OpenMeld[2].State : hand.OpenMeld[3].State;

                if (thirdMeld[0].Tile.IsDragon() && (thirdMeldState != MeldState.KanConcealed) && (thirdMeldState != MeldState.None))
                {
                    int playerOffset = thirdMeld[0].Called ? 3 :
                                       thirdMeld[thirdMeldState.GetTileCount() - 1].Called ? 1 :
                                                                                             2;

                    Player newSekininBaraiPlayer = hand.Player.AddOffset(playerOffset);

                    if (targetPlayer1 == Player.None)
                    {
                        targetPlayer1 = newSekininBaraiPlayer;
                    }
                    else
                    {
                        targetPlayer2 = newSekininBaraiPlayer;
                    }
                }
            }

            if ((state.Settings.GetSetting<bool>(GameOption.SekininBaraiDaisuushii) && winningHand.Yaku.Contains(Yaku.Daisuushii)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiTsuuiisou) && winningHand.Yaku.Contains(Yaku.Tsuuiisou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiSuukantsu) && winningHand.Yaku.Contains(Yaku.Suukantsu) && (hand.OpenMeld[3].State == MeldState.KanOpen)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiChinroutou) && winningHand.Yaku.Contains(Yaku.Chinroutou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiRyuuiisou) && winningHand.Yaku.Contains(Yaku.Ryuuiisou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiIisouSuushun) && winningHand.Yaku.Contains(Yaku.IisouSuushun)))
            {
                Player result = GetFourthCallSekininBaraiPlayer(winningHand, hand);
                if (targetPlayer1 == Player.None)
                {
                    targetPlayer1 = result;
                }
                else
                {
                    targetPlayer2 = result;
                }
            }
        }

        private Player GetFourthCallSekininBaraiPlayer(ICandidateHand winningHand, IHand hand)
        {
            Player targetPlayer = Player.None;
            if ((hand.GetCalledMeldCount() == 4) && 
                ((hand.OpenMeld[3].State == MeldState.Pon) ||
                 (hand.OpenMeld[3].State == MeldState.KanPromoted) ||
                 (hand.OpenMeld[3].State == MeldState.KanOpen)))
            {
                int playerOffset = hand.OpenMeld[3].Tiles[0].Called                                         ? 3 :
                                   hand.OpenMeld[3].Tiles[hand.OpenMeld[3].State.GetTileCount() - 1].Called ? 1 :
                                                                                                              2;
                targetPlayer = hand.Player.AddOffset(playerOffset);
            }
            return targetPlayer;
        }
    }
}
