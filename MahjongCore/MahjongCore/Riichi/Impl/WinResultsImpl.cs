// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Evaluator;

namespace MahjongCore.Riichi
{
    internal class WinResults
    {
        public Player        WinningPlayer;
        public CandidateHand WinningHand;
        public GameAction    Action;
        public int           ScoreHi;
        public int           ScoreLo;
        public int           Player1Delta;
        public int           Player2Delta;
        public int           Player3Delta;
        public int           Player4Delta;
        public int           Player1PoolDelta;
        public int           Player2PoolDelta;
        public int           Player3PoolDelta;
        public int           Player4PoolDelta;
        public bool          Limit; // True if mangan or better.

        public void Reset()
        {
            WinningPlayer = Player.None;
            WinningHand = null;
            Action = GameAction.Nothing;
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

        public void Populate(GameState state, Player winner, Player target, Player recentOpenKan, GameAction action, int pool)
        {
            Reset();
            WinningPlayer = winner;
            Action = action;

            Hand hand = state.GetHand(winner);
            if ((action == GameAction.Ron) || (action == GameAction.Tsumo))
            {
                // Mark the win. Advance streak and mark as not yakitori.
                hand.Streak++;
                hand.Yakitori = false;

                WinningHand = hand.WinningHandCache;

                // Can only have sekinin barai in multi-win scenarios if there's a ron. So don't calculate if this is a tsumo.
                Player sekininBarai1 = Player.None;
                Player sekininBarai2 = Player.None;
                if (action != GameAction.Tsumo)
                {
                    CheckSekininBarai(state, (action == GameAction.Ron), WinningHand, hand, recentOpenKan, out sekininBarai1, out sekininBarai2);
                }

                // Calculate the scores.
                RiichiScoring.GetWinningScore(hand, target, sekininBarai1, sekininBarai2, WinningHand.Han, WinningHand.Fu, pool, this);

                AdjustForPaaRenchan(hand, (action == GameAction.Ron));
            }
            else
            {
                hand.Streak = 0;
            }
        }

        private void AdjustForPaaRenchan(Hand hand, bool fRon)
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

        private void CheckSekininBarai(GameState state, bool fRon, CandidateHand winningHand, Hand hand, Player recentOpenKan, out Player targetPlayer1, out Player targetPlayer2)
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

        private Player GetFourthCallSekininBaraiPlayer(CandidateHand winningHand, Hand hand)
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
