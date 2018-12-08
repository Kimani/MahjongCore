// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class StandardCandidateHand : CandidateHand
    {
        internal TileImpl   PairTile = new TileImpl(); // If this has the winning tile flag, then one of them is the winning tile.
        internal MeldImpl[] Melds    = new MeldImpl[] { new MeldImpl(), new MeldImpl(), new MeldImpl(), new MeldImpl() };

        internal override CandidateHand Clone() { return new StandardCandidateHand(this); }

        internal StandardCandidateHand(TileType pairTile, bool winningTile)
        {
            PairTile.Type = pairTile;
            PairTile.WinningTile = winningTile;
        }

        internal StandardCandidateHand(StandardCandidateHand hand)
        {
            PairTile.Set(hand.PairTile);
            for (int i = 0; i < Melds.Length; ++i)
            {
                Melds[i].Set(hand.Melds[i]);
            }
        }

        internal void IterateMelds(Action<IMeld> callback)
        {
            foreach (MeldImpl meld in Melds)
            {
                if (!meld.State.IsCalled()) { break; }
                callback(meld);
            }
        }

        internal bool IterateMeldsAND(Func<IMeld, bool> callback)
        {
            foreach (MeldImpl meld in Melds)
            {
                if (!meld.State.IsCalled()) { break; }
                if (!callback(meld))        { return false; }
            }
            return true;
        }

        internal override bool Evaluate(IHand hand, bool ron)
        {
            ResetValues();

            // Go through all the Yakuman hands. If we get a result, break here.
            // Note that we don't evaluate Paa Renchan here. This is because Paa Renchan isn't a yaku, IE you can't like,
            // just get any 4 melds and a pair and get paa renchan. So we don't use it to evaluate if you have a hand or not.
            Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.ChuurenPoutou,
                                                            Riichi.Yaku.Suuankou,
                                                            Riichi.Yaku.Daisangen,
                                                            Riichi.Yaku.Shousuushii,
                                                            Riichi.Yaku.Daisuushii,
                                                            Riichi.Yaku.Suukantsu,
                                                            Riichi.Yaku.Ryuuiisou,
                                                            Riichi.Yaku.Chinroutou,
                                                            Riichi.Yaku.Tsuuiisou,
                                                            Riichi.Yaku.Chiihou,
                                                            Riichi.Yaku.Renhou,
                                                            Riichi.Yaku.Suurenkou,
                                                            Riichi.Yaku.HyakumanGoku,
                                                            Riichi.Yaku.BeniKujaku,
                                                            Riichi.Yaku.AoNoDoumon,
                                                            Riichi.Yaku.UupinKaihou,
                                                            Riichi.Yaku.IipinRaoyui,
                                                            Riichi.Yaku.RyansouChankan,
                                                            Riichi.Yaku.KachouFuugetsu,
                                                            Riichi.Yaku.Tenhou,
                                                            Riichi.Yaku.IisouSuushun,
                                                            Riichi.Yaku.Shousharin,
                                                            Riichi.Yaku.Shouchikurin,
                                                            Riichi.Yaku.Shousuurin });

            if (Han < 0)
            {
                Yakuman = Math.Abs(Han);
                AdjustForPaaRenchan(hand, ron);
                AdjustForDoubleYakumanSetting(hand);
                return true;
            }

            Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.KinkeiDokuritsu,
                                                            Riichi.Yaku.NagashiMangan });
            int unNaturalHan = 0;
            if (Han == 0)
            {
                Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.Riichi,
                                                                Riichi.Yaku.DoubleRiichi,
                                                                Riichi.Yaku.OpenRiichi,
                                                                Riichi.Yaku.Pinfu,
                                                                Riichi.Yaku.Iipeikou,
                                                                Riichi.Yaku.MenzenTsumo,
                                                                Riichi.Yaku.Ippatsu,
                                                                Riichi.Yaku.Uumensai,
                                                                Riichi.Yaku.SanshokuDoujun,
                                                                Riichi.Yaku.Ittsuu,
                                                                Riichi.Yaku.Ryanpeikou,
                                                                Riichi.Yaku.Toitoi,
                                                                Riichi.Yaku.Honroutou,
                                                                Riichi.Yaku.IisouSanjun,
                                                                Riichi.Yaku.Sanrenkou,
                                                                Riichi.Yaku.Sanankou,
                                                                Riichi.Yaku.SanshokuDoukou,
                                                                Riichi.Yaku.Sankantsu,
                                                                Riichi.Yaku.Tanyao,
                                                                Riichi.Yaku.OtakazeSankou,
                                                                Riichi.Yaku.Kanburi,
                                                                Riichi.Yaku.TsubameGaeshi,
                                                                Riichi.Yaku.Chun,
                                                                Riichi.Yaku.Haku,
                                                                Riichi.Yaku.Hatsu,
                                                                Riichi.Yaku.Ton,
                                                                Riichi.Yaku.DoubleTon,
                                                                Riichi.Yaku.Nan,
                                                                Riichi.Yaku.DoubleNan,
                                                                Riichi.Yaku.Sha,
                                                                Riichi.Yaku.DoubleSha,
                                                                Riichi.Yaku.Pei,
                                                                Riichi.Yaku.DoublePei,
                                                                Riichi.Yaku.Chanta,
                                                                Riichi.Yaku.Honitsu,
                                                                Riichi.Yaku.Junchan,
                                                                Riichi.Yaku.Shousangen,
                                                                Riichi.Yaku.Chinitsu });

                unNaturalHan += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.RinshanKaihou,
                                                                         Riichi.Yaku.HaiteiRaoyue,
                                                                         Riichi.Yaku.HouteiRaoyui,
                                                                         Riichi.Yaku.Chankan });
            }
            Han += unNaturalHan;

            // Break for natural wins...
            if (hand.Parent.Settings.GetSetting<bool>(GameOption.NaturalWins) && (Han == unNaturalHan))
            {
                return false;
            }

            // Break if we don't have enough yaku.
            int hanShibari = ((hand.Parent.Bonus >= 5) && hand.Parent.Settings.GetSetting<bool>(GameOption.RyanhanShibari)) ? 2 : 1;
            if (Han < hanShibari)
            {
                return false;
            }

            // Check dora.
            int doraCount = hand.Parent.DoraCount;
            ITile[] doraIndicators = hand.Parent.DoraIndicators;
            for (int iDora = 0; iDora < doraCount; ++iDora)
            {
                TileType doraTile = doraIndicators[iDora].Type.GetDoraTile();
                for (int iClosed = 0; iClosed < hand.ActiveTileCount; ++iClosed)
                {
                    if (hand.ActiveHand[iClosed].Type.IsEqual(doraTile))
                    {
                        Dora++;
                    }
                }

                foreach (IMeld meld in hand.Melds)
                {
                    for (int mTile = meld.State.GetTileCount() - 1; mTile >= 0; --mTile)
                    {
                        if (meld.Tiles[mTile].Type.IsEqual(doraTile))
                        {
                            Dora++;
                        }
                    }
                }
            }

            // Add ura dora.
            bool riichi = Yaku.Contains(Riichi.Yaku.Riichi) ||
                          Yaku.Contains(Riichi.Yaku.DoubleRiichi) ||
                          Yaku.Contains(Riichi.Yaku.OpenRiichi);

            if (riichi)
            {
                ITile[] uraDoraIndicators = hand.Parent.UraDoraIndicators;
                for (int iDora = 0; iDora < doraCount; ++iDora)
                {
                    TileType doraTile = uraDoraIndicators[iDora].Type.GetDoraTile();
                    for (int iClosed = 0; iClosed < hand.ActiveTileCount; ++iClosed)
                    {
                        if (hand.ActiveHand[iClosed].Type.IsEqual(doraTile))
                        {
                            UraDora++;
                        }
                    }
                }
            }

            // Add red dora.
            for (int iClosed = 0; iClosed < hand.ActiveTileCount; ++iClosed)
            {
                if (hand.ActiveHand[iClosed].Type.IsRedDora())
                {
                    RedDora++;
                }
            }

            foreach (IMeld m in hand.Melds)
            {
                for (int mTile = m.State.GetTileCount() - 1; mTile >= 0; --mTile)
                {
                    if (m.Tiles[mTile].Type.IsRedDora())
                    {
                        RedDora++;
                    }
                }
            }

            Han += Dora + UraDora + RedDora;
            AdjustForPaaRenchan(hand, ron);
            AdjustForDoubleYakumanSetting(hand);
            UpdateYakumanCount();

            // Don't need to process fu if we're mangan or better.
            if ((Han >= 5) || (Han < 0))
            {
                return true;
            }

            // Process the fu. Starts at 20.
            Fu = 20;

            // Menzen Kafu. Closed ron is 10.
            if (ron && hand.Closed)
            {
                Fu += 10;
            }

            if (!Yaku.Contains(Riichi.Yaku.Pinfu))
            {
                // Win for non-pinfu tsumo is 2.
                if (!ron)
                {
                    Fu += 2;
                }

                // Check the melds.
                //                       Closed   Open
                // Kan Terminals/Honors    32      16
                //              Simples    16       8
                // Pon Terminals/Honors     8       4
                //              Simples     4       2

                // Open melds + Closed kans.
                foreach (IMeld m in hand.Melds)
                {
                    // If this is a chii or not a call, TerminalFu and NonTerminalFu are both zero.
                    Fu += m.Tiles[0].Type.IsTerminalOrHonor() ? m.State.GetMeldNonSimpleFu() : m.State.GetMeldSimpleFu();
                }

                // Closed melds.
                for (int i = 0; i < (4 - hand.MeldCount); ++i)
                {
                    IMeld closedMeld = Melds[i];
                    Fu += 2 * (closedMeld.Tiles[0].Type.IsTerminalOrHonor() ? closedMeld.State.GetMeldNonSimpleFu() : closedMeld.State.GetMeldSimpleFu());
                }

                // Check the pair. If it's a yakuhai tile then it's +2.
                if (PairTile.Type.IsDragon())
                {
                    Fu += 2;
                }
                else
                {
                    // We get 2 fu if it's the round wind.
                    if (PairTile.Type.IsEqual(TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.Round)))
                    {
                        Fu += 2;
                    }

                    // We get 2 fu if it's the seat wind.
                    if (PairTile.Type.IsEqual(TileTypeExtensionMethods.GetSeatWindTile(hand.Player, hand.Parent.Dealer)))
                    {
                        Fu += 2;
                    }
                }

                // Check the wait.
                if (PairTile.WinningTile)
                {
                    Fu += 2; // Tanki machi
                }
                else
                {
                    // Since everything was sorted... we check the melds. If we find that the winning tile was a
                    // center tile (kanchan machi) or a sequential wait on the side of a 1-2-3 or 7-8-9 (ie the 3
                    // or 7) (penchan machi) then we add 2 fu.
                    for (int i = 0; i < (4 - hand.MeldCount); ++i)
                    {
                        IMeld m = Melds[i];
                        if ((m.Tiles[0].WinningTile && (m.Tiles[0].Type.GetValue() == 7)) ||
                            (m.Tiles[2].WinningTile && (m.Tiles[0].Type.GetValue() == 3)) ||
                            m.Tiles[1].WinningTile)
                        {
                            Fu += 2;
                        }
                    }
                }

                // If we have no fu yet, and we have at least one open meld, then we have open pinfu, which is +2.
                if ((Fu == 20) && (hand.MeldCount != 0))
                {
                    Fu += 2;
                }

                // Round up to the next ten.
                float flFu = ((float)Fu) / 10.0f;
                Fu = (int)(Math.Ceiling(flFu) * 10.0f);
            }

            // Done!
            return true;
        }

        /**
         * Adds this CandidateHand to the given bucket. In the case that there are other permutations of the current hand,
         * it adds all permutations to the bucket. These permutations come from different possibilities of what was the winning tile.
         * Also sets the winning tile flag on the different tiles for the different permutations. (There is at least 1 permutation...)
         */
        internal override void ExpandAndInsert(List<CandidateHand> chBucket, IHand hand)
        {
            // We'll do this the inefficient way that ends up wasting memory. We shall just
            // clone this guy before we insert for all the instances of the winning tile we find.
            // Do it for all the melds and then do it for the pair.
            int calledMeldCount = hand.MeldCount;
            TileType winningTile = hand.ActiveHand[hand.ActiveTileCount - 1].Type;
            for (int iMeld = 0; iMeld < (4 - calledMeldCount); ++iMeld)
            {
                // Note that if we have a pon, we only need to look at one of them. Don't need to
                // generate branches for all three possibilities here.
                for (int iTile = 0; iTile < ((Melds[iMeld].State == MeldState.Pon) ? 1 : 3); ++iTile)
                {
                    if (Melds[iMeld].Tiles[iTile].Type.IsEqual(winningTile))
                    {
                        StandardCandidateHand dupHand = (StandardCandidateHand)Clone();
                        dupHand.Melds[iMeld].TilesRaw[iTile].WinningTile = true;
                        chBucket.Add(dupHand);
                    }
                }
            }

            if (PairTile.Type.IsEqual(winningTile))
            {
                StandardCandidateHand dupHand = (StandardCandidateHand)Clone();
                dupHand.PairTile.WinningTile = true;
                chBucket.Add(dupHand);
            }
        }
    }
}
