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

    internal class GameResultImpl : IGameResult
    {
        // IGameResult
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

        // GameResultImpl
        public GameResultImpl(IGameSettings settings,
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

            // Generate a GameResult and submit it to GameComplete.
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

    internal class WinResultImpl : IWinResult
    {
        // IWinResult
        public Player         WinningPlayer    { get { return _WinningPlayer; } }
        public ICandidateHand WinningHand      { get { return _WinningHand; } }
        public WinType        Action           { get { return _Action; } }
        public int            ScoreHi          { get { return _ScoreHi; } }
        public int            ScoreLo          { get { return _ScoreLo; } }
        public int            Player1Delta     { get { return _Player1Delta; } }
        public int            Player2Delta     { get { return _Player2Delta; } }
        public int            Player3Delta     { get { return _Player3Delta; } }
        public int            Player4Delta     { get { return _Player4Delta; } }
        public int            Player1PoolDelta { get { return _Player1PoolDelta; } }
        public int            Player2PoolDelta { get { return _Player2PoolDelta; } }
        public int            Player3PoolDelta { get { return _Player3PoolDelta; } }
        public int            Player4PoolDelta { get { return _Player4PoolDelta; } }
        public bool           Limit            { get { return _Limit; } }

        // WinResultImpl
        private Player         _WinningPlayer;
        private ICandidateHand _WinningHand;
        private WinType        _Action;
        private int            _ScoreHi;
        private int            _ScoreLo;
        private int            _Player1Delta;
        private int            _Player2Delta;
        private int            _Player3Delta;
        private int            _Player4Delta;
        private int            _Player1PoolDelta;
        private int            _Player2PoolDelta;
        private int            _Player3PoolDelta;
        private int            _Player4PoolDelta;
        private bool           _Limit;

        internal WinResultImpl()                                                                    { }
        internal WinResultImpl(IGameState s, Player w, Player t, Player r, WinType a, int b, int p) { Populate(s, w, t, r, a, b, p); }

        internal void Reset()
        {
            _WinningPlayer = Player.None;
            _WinningHand = null;
            _Action = WinType.None;
            _ScoreHi = 0;
            _ScoreLo = 0;
            _Player1Delta = 0;
            _Player2Delta = 0;
            _Player3Delta = 0;
            _Player4Delta = 0;
            _Player1PoolDelta = 0;
            _Player2PoolDelta = 0;
            _Player3PoolDelta = 0;
            _Player4PoolDelta = 0;
            _Limit = false;
        }

        internal int GetPlayerDelta(Player p)
        {
            Global.Assert(p.IsPlayer());
            return (p == Player.Player1) ? _Player1Delta :
                   (p == Player.Player2) ? _Player2Delta :
                   (p == Player.Player3) ? _Player3Delta :
                                           _Player4Delta;
        }

        internal int GetPlayerPoolDelta(Player p)
        {
            Global.Assert(p.IsPlayer());
            return (p == Player.Player1) ? _Player1PoolDelta :
                   (p == Player.Player2) ? _Player2PoolDelta :
                   (p == Player.Player3) ? _Player3PoolDelta :
                                           _Player4PoolDelta;
        }

        internal void SetPlayerScore(Player p, int score)
        {
            Global.Assert(p.IsPlayer());
            if      (p == Player.Player1) { _Player1Delta = score; }
            else if (p == Player.Player2) { _Player2Delta = score; }
            else if (p == Player.Player3) { _Player3Delta = score; }
            else if (p == Player.Player4) { _Player4Delta = score; }
        }

        internal void SetPlayerPool(Player p, int pool)
        {
            Global.Assert(p.IsPlayer());
            if      (p == Player.Player1) { _Player1PoolDelta = pool; }
            else if (p == Player.Player2) { _Player2PoolDelta = pool; }
            else if (p == Player.Player3) { _Player3PoolDelta = pool; }
            else if (p == Player.Player4) { _Player4PoolDelta = pool; }
        }

        internal void PopulateDraw(bool player1Tempai, bool player2Tempai, bool player3Tempai, bool player4Tempai)
        {
            Reset();
            _WinningPlayer = Player.Multiple;
            _Action = WinType.Draw;

            // Draw game! Determine score outputs. No warame in this situation because the math doesn't work.
            int tempaiCount = (player1Tempai ? 1 : 0) + (player2Tempai ? 1 : 0) + (player3Tempai ? 1 : 0) + (player4Tempai ? 1 : 0);

            if ((tempaiCount == 1) || (tempaiCount == 2) || (tempaiCount == 3))
            {
                int deltaGainPoints = 3000 / tempaiCount;
                int deltaLosePoints = -(3000 / (4 - tempaiCount));

                _Player1Delta = (player1Tempai ? deltaGainPoints : deltaLosePoints);
                _Player2Delta = (player2Tempai ? deltaGainPoints : deltaLosePoints);
                _Player3Delta = (player3Tempai ? deltaGainPoints : deltaLosePoints);
                _Player4Delta = (player4Tempai ? deltaGainPoints : deltaLosePoints);
            }
        }

        internal void Populate(IGameState state, Player winner, Player target, Player recentOpenKan, WinType action, int bonus, int pool)
        {
            Reset();
            _WinningPlayer = winner;
            _Action = action;

            GameStateImpl stateImpl = state as GameStateImpl;
            HandImpl hand = stateImpl.GetHand(winner) as HandImpl;
            if ((action == WinType.Ron) || (action == WinType.Tsumo))
            {
                _WinningHand = hand.WinningHandCache;

                // Can only have sekinin barai in multi-win scenarios if there's a ron. So don't calculate if this is a tsumo.
                Player sekininBarai1 = Player.None;
                Player sekininBarai2 = Player.None;
                if (action == WinType.Ron)
                {
                    CheckSekininBarai(state, true, _WinningHand, hand, recentOpenKan, out sekininBarai1, out sekininBarai2);
                }

                // Calculate the scores.
                RiichiScoring.GetWinningScore(state.Settings,
                                              hand.Player,
                                              state.Dealer,
                                              target,
                                              sekininBarai1,
                                              sekininBarai2,
                                              state.Wareme,
                                              _WinningHand.Han,
                                              _WinningHand.Fu,
                                              bonus,
                                              pool,
                                              out _ScoreHi,          out _ScoreLo,
                                              out _Player1Delta,     out _Player2Delta,     out _Player3Delta,     out _Player4Delta,
                                              out _Player1PoolDelta, out _Player2PoolDelta, out _Player3PoolDelta, out _Player4PoolDelta,
                                              out _Limit);
            }
        }

        private void CheckSekininBarai(IGameState state, bool ron, ICandidateHand winningHand, IHand hand, Player recentOpenKan, out Player targetPlayer1, out Player targetPlayer2)
        {
            targetPlayer1 = Player.None;
            targetPlayer2 = Player.None;

            if (state.Settings.GetSetting<bool>(GameOption.SekininBaraiRinshan) &&
                !ron &&
                (recentOpenKan != Player.None) &&
                (winningHand != null) &&
                (winningHand.Yaku.Contains(Yaku.RinshanKaihou) || winningHand.Yaku.Contains(Yaku.UupinKaihou)))
            {
                targetPlayer1 = recentOpenKan;
            }

            if (state.Settings.GetSetting<bool>(GameOption.SekininBaraiDaisangen) && winningHand.Yaku.Contains(Yaku.Daisangen))
            {
                int dragonCallCount = 0;
                foreach (IMeld meld in hand.Melds) { if ((meld.State != MeldState.None) && meld.Tiles[0].Type.IsDragon()) { dragonCallCount++; } }
                if (dragonCallCount == 3)
                {
                    IMeld thirdDragonMeld = ((hand.Melds[4].State != MeldState.None) && hand.Melds[4].Tiles[0].Type.IsDragon()) ? hand.Melds[4] : hand.Melds[3];
                    if (thirdDragonMeld.State != MeldState.KanConcealed)
                    {
                        if (targetPlayer1 == Player.None)
                        {
                            targetPlayer1 = thirdDragonMeld.Target;
                        }
                        else if (targetPlayer1 != thirdDragonMeld.Target)
                        {
                            targetPlayer2 = thirdDragonMeld.Target;
                        }
                    }
                }
            }

            if ((state.Settings.GetSetting<bool>(GameOption.SekininBaraiDaisuushii) && winningHand.Yaku.Contains(Yaku.Daisuushii)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiTsuuiisou) && winningHand.Yaku.Contains(Yaku.Tsuuiisou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiSuukantsu) && winningHand.Yaku.Contains(Yaku.Suukantsu) && (hand.Melds[3].State == MeldState.KanOpen)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiChinroutou) && winningHand.Yaku.Contains(Yaku.Chinroutou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiRyuuiisou) && winningHand.Yaku.Contains(Yaku.Ryuuiisou)) ||
                (state.Settings.GetSetting<bool>(GameOption.SekininBaraiIisouSuushun) && winningHand.Yaku.Contains(Yaku.IisouSuushun)))
            {
                Player result = GetFourthCallSekininBaraiPlayer(winningHand, hand);
                if (targetPlayer1 == Player.None)
                {
                    targetPlayer1 = result;
                }
                else if (targetPlayer1 != result)
                {
                    targetPlayer2 = result;
                }
            }
        }

        private Player GetFourthCallSekininBaraiPlayer(ICandidateHand winningHand, IHand hand)
        {
            IMeld fourthMeld = hand.Melds[3];
            return ((hand.MeldCount == 4) &&
                    ((fourthMeld.State == MeldState.Pon) ||
                     (fourthMeld.State == MeldState.KanPromoted) ||
                     (fourthMeld.State == MeldState.KanOpen))) ? fourthMeld.Target : Player.None;
        }
    }
}
