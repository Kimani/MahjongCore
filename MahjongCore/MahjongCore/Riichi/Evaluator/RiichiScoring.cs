// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;

namespace MahjongCore.Riichi.Evaluator
{
    public class TsumoScore
    {
        public int ScoreHi = 0;
        public int ScoreLo = 0;
    }

    public class RiichiScoring
    {
        /**
         * Determine the winning score based solely on the provided parameters.
         * Note that winStreak should be the winstreak we should look at. Add +1 to this beforehand to see if this would give you enough streak for paa renchan.
         */
        public static void GetWinningScore(Hand winningPlayerHand,
                                           Player targetPlayer,
                                           Player targetSekininBarai1,
                                           Player targetSekininBarai2,
                                           int han,
                                           int fu,
                                           int pool,
                                           WinResults scoreResults)
        {
            // Check for paa renchan. If we have a yakuman hand already, just add to the yakumans.
            int paaRenchanValue = Yaku.PaaRenchan.Evaluate(winningPlayerHand, null, false);
            int finalHan = (han > 0) ? han : (han + paaRenchanValue);

            // Get the raw values and get the winning score.
            GameState state = winningPlayerHand.Parent;
            GameSettings settings = state.Settings;

            GetWinningScoreRaw(state.Settings,
                               winningPlayerHand.Player,
                               state.CurrentDealer,
                               targetPlayer,
                               targetSekininBarai1,
                               targetSekininBarai2,
                               state.WaremePlayer,
                               finalHan,
                               fu,
                               pool,
                               state.Bonus,
                               scoreResults);
        }

        public static void GetWinningScoreRaw(GameSettings settings,
                                              Player winningPlayer,
                                              Player currentDealer,
                                              Player targetPlayer,
                                              Player targetSekininBarai1,
                                              Player targetSekininBarai2,
                                              Player waremePlayer,
                                              int finalHan,
                                              int fu,
                                              int poolPoints,
                                              int bonusCount,
                                              WinResults scoreResults)
        {
            bool isWinnerDealer = winningPlayer == currentDealer;
            bool isWinnerWareme = winningPlayer == waremePlayer;
            scoreResults.ResetScores();

            // Process Tsumo or Ron. If there are sekinin barai players then determine the scoring like a Ron.
            //GameState state = winningPlayerHand.Parent;
            if ((targetPlayer == Player.Multiple) && (targetSekininBarai1 == Player.None) && (targetSekininBarai2 == Player.None))
            {
                TsumoScore ts = new TsumoScore();
                GetScoreTsumo(settings, finalHan, fu, isWinnerDealer, ts, out scoreResults.Limit);

                foreach (Player p in PlayerExtensionMethods.Players)
                {
                    int pScore = -((winningPlayer != p) ? ((p == currentDealer) ? ts.ScoreHi : ts.ScoreLo) : 0);
                    pScore *= ((isWinnerWareme) || (p == waremePlayer)) ? 2 : 1;
                    pScore -= (winningPlayer != p) ? (bonusCount * 100) : 0;

                    // Make sure the winning player's score is zero at this juncture.
                    Global.Assert((p != winningPlayer) || (pScore == 0));
                    scoreResults.SetPlayerScore(p, pScore);
                }

                // Get the display scores.
                scoreResults.ScoreHi = ts.ScoreHi;
                scoreResults.ScoreLo = ts.ScoreLo;
                if (waremePlayer == winningPlayer)
                {
                    scoreResults.ScoreHi *= 2;
                    scoreResults.ScoreLo *= 2;
                }
                scoreResults.ScoreHi += (bonusCount * 100);
                scoreResults.ScoreLo += (bonusCount * 100);
            }
            else
            {
                // Determine Ron point deltas. Behave properly for sekinin barai. Can split up to three ways, potentially.
                // Note that only the person who paid in will need to pay the homba payments. The liable player will pay half
                // of the non-homba score. Also take into account warame. If this is actually a tsumo, then we're doing
                // sekinin barai, so don't count one extra person for the split.
                int ronScore = GetScoreRon(settings, finalHan, fu, isWinnerDealer, out scoreResults.Limit);
                int ronPaymentSplit = ((targetPlayer == Player.Multiple) ? 0 : 1) + ((targetSekininBarai1 != Player.None) ? 1 : 0) + ((targetSekininBarai2 != Player.None) ? 1 : 0);
                int rawRonPayment = ronScore / ronPaymentSplit;

                foreach (Player p in PlayerExtensionMethods.Players)
                {
                    int pScore = -(((targetPlayer == p) ? rawRonPayment : 0) +
                                 ((targetSekininBarai1 == p) ? rawRonPayment : 0) +
                                 ((targetSekininBarai2 == p) ? rawRonPayment : 0));
                    pScore = (pScore / 100) * 100;
                    pScore *= ((isWinnerWareme) || (p == waremePlayer)) ? 2 : 1;
                    pScore -= (targetPlayer == p) ? (bonusCount * 300) : 0;

                    // Make sure the winning player's score is zero at this juncture.
                    Global.Assert((p != winningPlayer) || (pScore == 0));
                    scoreResults.SetPlayerScore(p, pScore);
                }

                // Get the display scores.
                scoreResults.ScoreHi = ronScore;
                if (isWinnerWareme || (waremePlayer == targetPlayer))
                {
                    scoreResults.ScoreHi *= 2;
                }
                scoreResults.ScoreHi += (bonusCount * 300);
            }

            // Tally up all the points and give them to the winning player.
            scoreResults.SetPlayerScore(winningPlayer, -(scoreResults.Player1Delta +
                                                         scoreResults.Player2Delta +
                                                         scoreResults.Player3Delta +
                                                         scoreResults.Player4Delta));
            scoreResults.SetPlayerPool(winningPlayer, poolPoints);
        }

        public static int GetScoreRon(GameSettings settings, int han, int fu, bool dealer, out bool limit)
        {
            limit = false;

            if (han < 0)
            {
                limit = true;
                return Math.Abs((dealer ? 48000 : 32000) * han);
            }

            if (han >= 13)
            {
                limit = true;
                return (dealer ? 48000 : 32000);              // Kazoe-Yakuman
            }
            if (han >= 11)
            {
                limit = true;
                return (dealer ? 36000 : 24000);              // Sanbaiman
            }
            if (han >= 8)
            {
                limit = true;
                return (dealer ? 24000 : 16000);              // Baiman
            }
            if (han >= 6)
            {
                limit = true;
                return (dealer ? 18000 : 12000);              // Haneman
            }
            if (han >= 5)
            {
                limit = true;
                return (dealer ? 12000 : 8000);               // Mangan
            }

            int basicpoint = fu * (int)Math.Pow(2, (2 + han));
            if (basicpoint >= 2000)
            {
                limit = true;
                basicpoint = 2000;
            }

            int points = basicpoint * (dealer ? 6 : 4);

            // Round up to the next hundred
            int smallpoints = (points / 100) * 100;
            if (points - smallpoints > 0)
            {
                points = smallpoints + 100;
            }

            // Process kiriage mangan.
            if (settings.GetSetting<bool>(GameOption.KiriageMangan))
            {
                if (((han == 3) && (fu >= 80)) || ((han == 4) && (fu >= 30)))
                {
                    limit = true;
                    points = (dealer ? 12000 : 8000);
                }
            }

            // Return the result!
            return points;
        }

        public static void GetScoreTsumo(GameSettings settings, int han, int fu, bool dealer, TsumoScore ts, out bool limit)
        {
            limit = false;

            int basicpoint = 0;
            if (han < 0)
            {
                limit = true;
                basicpoint = Math.Abs(8000 * han);
            }
            else if (han >= 13)
            {
                limit = true;
                basicpoint = 8000;
            }
            else if (han >= 11)
            {
                limit = true;
                basicpoint = 6000;
            }
            else if (han >= 8)
            {
                limit = true;
                basicpoint = 4000;
            }
            else if (han >= 6)
            {
                limit = true;
                basicpoint = 3000;
            }
            else if (han >= 5)
            {
                limit = true;
                basicpoint = 2000;
            }
            else
            {
                basicpoint = fu * (int)Math.Pow(2, (2 + han));
                if (basicpoint >= 2000)
                {
                    limit = true;
                    basicpoint = 2000;
                }

                if (settings.GetSetting<bool>(GameOption.KiriageMangan))
                {
                    if (((han == 3) && (fu >= 80)) || ((han == 4) && (fu >= 30)))
                    {
                        limit = true;
                        basicpoint = 2000;
                    }
                }
            }

            // Determine final point score!
            if (dealer)
            {
                ts.ScoreHi = 2 * basicpoint; // All 3 players pay this.
                ts.ScoreLo = 2 * basicpoint;
            }
            else
            {
                ts.ScoreHi = 2 * basicpoint;
                ts.ScoreLo = basicpoint;
            }

            // Round em!
            {
                int smallpoints = (ts.ScoreHi / 100) * 100;
                if (ts.ScoreHi - smallpoints > 0)
                {
                    ts.ScoreHi = smallpoints + 100;
                }
            }

            {
                int smallpoints = (ts.ScoreLo / 100) * 100;
                if (ts.ScoreLo - smallpoints > 0)
                {
                    ts.ScoreLo = smallpoints + 100;
                }
            }
        }

        public static void GetScores(GameSettings settings, int points1st, int points2nd, int points3rd, int points4th, float[] scores)
        {
            float startingScore = (float)settings.GetSetting<int>(GameOption.StartingPoints) / 1000.0f;
            float rawScore1 = (((float)points1st) / 1000.0f) - startingScore;
            float rawScore2 = (((float)points2nd) / 1000.0f) - startingScore;
            float rawScore3 = (((float)points3rd) / 1000.0f) - startingScore;
            float rawScore4 = (((float)points4th) / 1000.0f) - startingScore;

            // Apply the uma. Don't bother if everyone's tied.
            int firstTieCount = 1 + ((points1st == points2nd) ? 1 : 0) +
                                    ((points1st == points3rd) ? 1 : 0) +
                                    ((points1st == points4th) ? 1 : 0);

            Uma uma = settings.GetSetting<Uma>(GameOption.UmaOption);
            if ((uma != Uma.Uma_None) && (firstTieCount < 4))
            {
                // If we want integer rounded scores, do the rounding now. We don't need to correct
                // the rounding errors at this time though.
                if (settings.GetSetting<bool>(GameOption.IntFinalScores))
                {
                    rawScore1 = (float)Math.Round(rawScore1);
                    rawScore2 = (float)Math.Round(rawScore2);
                    rawScore3 = (float)Math.Round(rawScore3);
                    rawScore4 = (float)Math.Round(rawScore4);
                }

                if (settings.GetSetting<bool>(GameOption.SplitTieUma))
                {
                    // Figure out the clustering of players into split groups.
                    int[] placementSplit = new int[] { 1, 1, 1, 1 };
                    int[] originalScores = new int[] { points1st, points2nd, points3rd, points4th };

                    for (int iPlace = 0; iPlace < 3;)
                    {
                        int placeCount = 1;
                        for (int iCheck = iPlace + 1; iCheck < 4; ++iCheck)
                        {
                            if (originalScores[iPlace] == originalScores[iCheck])
                            {
                                placeCount++;
                                placementSplit[iCheck] = 0;
                            }
                        }

                        placementSplit[iPlace] = placeCount;
                        iPlace += placeCount;
                    }

                    // Figure out the splits, which will exist so long as all four players aren't tied.
                    // The only possible splits are:
                    // - 2/0/1/1
                    // - 2/0/2/0
                    // - 3/0/0/1
                    // - 1/2/0/1
                    // - 1/3/0/0
                    // - 1/1/2/0
                    float[] umaDeltas = new float[] { EnumAttributes.GetAttributeValue<Place1Value, int>(uma),
                                                        EnumAttributes.GetAttributeValue<Place2Value, int>(uma),
                                                        EnumAttributes.GetAttributeValue<Place3Value, int>(uma),
                                                        EnumAttributes.GetAttributeValue<Place4Value, int>(uma) };

                    for (int iPlacementSlot = 0; iPlacementSlot < 4; ++iPlacementSlot)
                    {
                        float placeUmaDelta = 0;
                        for (int iUmaIncorporateOffset = 0; iUmaIncorporateOffset < placementSplit[iPlacementSlot]; ++iUmaIncorporateOffset)
                        {
                            placeUmaDelta += umaDeltas[iPlacementSlot + iUmaIncorporateOffset];
                        }

                        umaDeltas[iPlacementSlot] = (placementSplit[iPlacementSlot] > 0) ? placeUmaDelta / ((float)placementSplit[iPlacementSlot]) : 0.0f;

                        // Make sure the delta corresponds to a multiple of 100 points. It could be 3.3333... This needs to be 3.3.
                        umaDeltas[iPlacementSlot] *= 10.0f;
                        umaDeltas[iPlacementSlot] = (float)Math.Floor(umaDeltas[iPlacementSlot]);
                        umaDeltas[iPlacementSlot] /= 10.0f;
                    }

                    // Alright. Now that we have the actual deltas, we need to apply them.
                    float[] rawScores = new float[] { rawScore1, rawScore2, rawScore3, rawScore4 };
                    for (int iPlayer = 0; iPlayer < 4; ++iPlayer)
                    {
                        for (int iSlot = 0; iSlot < 4; ++iSlot)
                        {
                            if (placementSplit[iSlot] != 0)
                            {
                                scores[iPlayer] = rawScores[iPlayer] + umaDeltas[iSlot];
                                placementSplit[iSlot]--;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    scores[0] = rawScore1 + (float)uma.GetScoreDelta(Placement.Place1);
                    scores[1] = rawScore2 + (float)uma.GetScoreDelta(Placement.Place2);
                    scores[2] = rawScore3 + (float)uma.GetScoreDelta(Placement.Place3);
                    scores[3] = rawScore4 + (float)uma.GetScoreDelta(Placement.Place4);
                }
            }
            else
            {
                scores[0] = rawScore1;
                scores[1] = rawScore2;
                scores[2] = rawScore3;
                scores[3] = rawScore4;
            }

            if (settings.GetSetting<bool>(GameOption.IntFinalScores))
            {
                // We applied uma (or maybe we didn't, that's fine too) and now it's time to correct the scores to handle
                // rounding errors. We will with best effort try to do this while preserving the tie that tied players will have.
                // First, we round again. Also if there's no uma we didn't round in the first place.
                for (int i = 0; i < 4; ++i)
                {
                    scores[i] = (float)Math.Round(scores[i]);
                }

                // Now figure out what score the first place people get. It's possible that multiple people
                // tie for 1st, in which case we want to make sure that they both still end up with the same score.
                // This might end up in losing a point, so everything doesn't add up to 0, but this is fine.
                // It's also possible that 2 players tie for 1st, and the 3rd place player is so close they end up
                // with the same score as the 2 1st place players after all this is done. (ex: the point split
                // 29k/29k/28k/15k) This is okay! The 1st place players will split the uma later and differentiate
                // themselves (unless there is no uma, in which case RIP)

                // Determine what score the first place player should have, or the multiple first place players should split.
                int firstPlaceScore = 0;
                for (int i = 0; i < (4 - firstTieCount); ++i)
                {
                    firstPlaceScore += (int)scores[3 - i];
                }

                // Figure out how much each first place player should get. This division is lossy and this is
                // where we can lose points into the ether. This can't be helped if we're doing integer final scores.
                int splitFirstPlaceScore = -firstPlaceScore / firstTieCount;
                for (int i = 0; i < firstTieCount; ++i)
                {
                    scores[i] = splitFirstPlaceScore;
                }
            }

            // Add the oka if it exists. We'll need to split this as well.
            Oka oka = settings.GetSetting<Oka>(GameOption.OkaOption);
            if (oka == Oka.Oka_None)
            {
                int okaValue = oka.GetDelta() / firstTieCount;
                for (int i = 0; i < firstTieCount; ++i)
                {
                    scores[i] += (float)okaValue;
                }
            }
        }
    }
}
