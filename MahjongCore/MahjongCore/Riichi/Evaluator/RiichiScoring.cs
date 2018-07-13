// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;

namespace MahjongCore.Riichi.Evaluator
{
    public class RiichiScoring
    {
        private static int RoundUpToNextHundred(int input) { return (int)(Math.Ceiling(((double)(input)) / 100.0) * 100); }

        public static void GetWinningScore(IGameSettings settings,
                                           Player winner,
                                           Player dealer,
                                           Player target,
                                           Player pao1,
                                           Player pao2,
                                           Player wareme,
                                           int finalHan,
                                           int fu,
                                           int bonus,
                                           int pool,
                                           out int scoreHi,
                                           out int scoreLo,
                                           out int player1Delta,
                                           out int player2Delta,
                                           out int player3Delta,
                                           out int player4Delta,
                                           out int player1PoolDelta,
                                           out int player2PoolDelta,
                                           out int player3PoolDelta,
                                           out int player4PoolDelta,
                                           out bool limit)
        {
            Global.Assert(winner.IsPlayer());
            Global.Assert(dealer.IsPlayer());

            bool isWinnerWareme = (winner == wareme);
            int[] delta = new int[4];
            int[] poolDelta = new int[4];

            // Process Tsumo or Ron for non-winners. If there are sekinin barai players then determine the scoring like a Ron.
            if ((target == Player.Multiple) && (pao1 == Player.None) && (pao2 == Player.None))
            {
                GetScoreTsumo(settings, finalHan, fu, (winner == dealer), out limit, out scoreHi, out scoreLo);

                foreach (Player p in PlayerHelpers.Players)
                {
                    int score = -((winner != p) ? ((p == dealer) ? scoreHi : scoreLo) : 0);
                    score *= ((isWinnerWareme) || (p == wareme)) ? 2 : 1;
                    score -= (winner != p) ? (bonus * 100) : 0;
                    delta[p.GetZeroIndex()] = score;
                }

                if (isWinnerWareme)
                {
                    scoreHi *= 2;
                    scoreLo *= 2;
                }
                scoreHi += (bonus * 100);
                scoreLo += (bonus * 100);
            }
            else
            {
                // Determine Ron point deltas. Behave properly for sekinin barai. Can split up to three ways, potentially.
                // Note that only the person who paid in will need to pay the homba payments. The liable player will pay half
                // of the non-homba score. Also take into account warame. If this is actually a tsumo, then we're doing
                // sekinin barai, so don't count one extra person for the split.
                int ronScore = GetScoreRon(settings, finalHan, fu, (winner == dealer), out limit);
                int paymentSplit = ((target == Player.Multiple) ? 0 : 1) + ((pao1 != Player.None) ? 1 : 0) + ((pao2 != Player.None) ? 1 : 0);
                int rawPayment = ronScore / paymentSplit;

                foreach (Player p in PlayerHelpers.Players)
                {
                    int score = (((target == p) ? rawPayment : 0) +
                                 ((pao1 == p) ? rawPayment : 0) +
                                 ((pao2 == p) ? rawPayment : 0));
                    score = -RoundUpToNextHundred(score);
                    score *= ((isWinnerWareme) || (p == wareme)) ? 2 : 1;
                    score -= (target == p) ? (bonus * 300) : 0;
                    delta[p.GetZeroIndex()] = score;
                }

                // Get the display scores.
                scoreHi = ronScore;
                if (isWinnerWareme || (wareme == target))
                {
                    scoreHi *= 2;
                }
                scoreHi += (bonus * 300);
                scoreLo = 0;
            }

            // Make sure the winning player's score is zero at this juncture.
            // Tally up all the points and give them to the winning player.
            delta[winner.GetZeroIndex()] = -(delta[0] + delta[1] + delta[2] + delta[3]);
            poolDelta[winner.GetZeroIndex()] = pool;

            player1Delta = delta[0];
            player2Delta = delta[1];
            player3Delta = delta[2];
            player4Delta = delta[3];
            player1PoolDelta = poolDelta[0];
            player2PoolDelta = poolDelta[1];
            player3PoolDelta = poolDelta[2];
            player4PoolDelta = poolDelta[3];
        }

        public static int GetScoreRon(IGameSettings settings, int han, int fu, bool dealer, out bool limit)
        {
            int basicpoint = fu * (int)Math.Pow(2, (2 + han));
            limit = (han >= 5) || (han < 0) || (basicpoint >= 2000);

            if (limit)
            {
                return (han < 0)   ? Math.Abs((dealer ? 48000 : 32000) * han) : // Yakuman
                       (han >= 13) ? (dealer ? 48000 : 32000) :                 // Kazoe-Yakuman
                       (han >= 11) ? (dealer ? 36000 : 24000) :                 // Sanbaiman
                       (han >= 8)  ? (dealer ? 24000 : 16000) :                 // Baiman
                       (han >= 6)  ? (dealer ? 18000 : 12000) :                 // Haneman
                                     (dealer ? 12000 : 8000);                   // Mangan
            }

            int points = RoundUpToNextHundred(basicpoint * (dealer ? 6 : 4));
            if (settings.GetSetting<bool>(GameOption.KiriageMangan))
            {
                if (((han == 3) && (fu >= 80)) || ((han == 4) && (fu >= 30)))
                {
                    limit = true;
                    points = (dealer ? 12000 : 8000);
                }
            }
            return points;
        }

        public static void GetScoreTsumo(IGameSettings settings, int han, int fu, bool dealer, out bool limit, out int scoreHi, out int scoreLo)
        {
            int basicpoint = fu * (int)Math.Pow(2, (2 + han));
            limit = (han >= 5) || (han < 0) || (basicpoint >= 2000);

            if (limit)
            {
                basicpoint = (han < 0) ? Math.Abs(8000 * han) : // Yakuman
                             (han >= 13) ? 8000 :               // Kazoe-Yakuman
                             (han >= 11) ? 6000 :               // Sanbaiman
                             (han >= 8)  ? 4000 :               // Baiman
                             (han >= 6)  ? 3000 :               // Haneman
                                           2000;                // Mangan
            }
            else if (settings.GetSetting<bool>(GameOption.KiriageMangan))
            {
                if (((han == 3) && (fu >= 80)) || ((han == 4) && (fu >= 30)))
                {
                    limit = true;
                    basicpoint = 2000;
                }
            }

            scoreHi = RoundUpToNextHundred(2 * basicpoint);
            scoreLo = RoundUpToNextHundred((dealer ? 2 : 1) * basicpoint);
        }

        public static void GetScores(IGameSettings settings, int points1st, int points2nd, int points3rd, int points4th, float[] scores)
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
                // If we want integer rounded scores, do the rounding now. We don't need to correct the rounding errors at this time though.
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
                    float[] umaDeltas = new float[] { uma.GetScoreDelta(Placement.Place1), uma.GetScoreDelta(Placement.Place2),
                                                      uma.GetScoreDelta(Placement.Place3), uma.GetScoreDelta(Placement.Place4) };

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
