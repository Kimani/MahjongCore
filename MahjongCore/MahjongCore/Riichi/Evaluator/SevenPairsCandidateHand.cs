// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Helpers;
using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    internal class SevenPairsCandidateHand : CandidateHand
    {
        internal TileImpl[] PairTiles = new TileImpl[7] { new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl() };

        protected override bool Evaluate(IHand hand, bool ron)
        {
            ResetValues();

            // Check for yakuman hands.
            Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.Chinroutou,
                                                            Riichi.Yaku.Tsuuiisou,
                                                            Riichi.Yaku.Daichisei,
                                                            Riichi.Yaku.Daisharin,
                                                            Riichi.Yaku.Daichikurin,
                                                            Riichi.Yaku.Daisuurin,
                                                            Riichi.Yaku.Chiihou,
                                                            Riichi.Yaku.Renhou,
                                                            Riichi.Yaku.AoNoDoumon,
                                                            Riichi.Yaku.IipinRaoyui,
                                                            Riichi.Yaku.RyansouChankan,
                                                            Riichi.Yaku.Tenhou,
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

            // Go through all the yaku.
            Han += EvaluateYakuList(hand, ron, new Yaku[] { Riichi.Yaku.Chiitoitsu,
                                                            Riichi.Yaku.Riichi,
                                                            Riichi.Yaku.DoubleRiichi,
                                                            Riichi.Yaku.OpenRiichi,
                                                            Riichi.Yaku.Tanyao,
                                                            Riichi.Yaku.MenzenTsumo,
                                                            Riichi.Yaku.Chinitsu,
                                                            Riichi.Yaku.Honitsu,
                                                            Riichi.Yaku.Honroutou,
                                                            Riichi.Yaku.Ippatsu,
                                                            Riichi.Yaku.HaiteiRaoyue,
                                                            Riichi.Yaku.HouteiRaoyui,
                                                            Riichi.Yaku.Chankan,
                                                            Riichi.Yaku.Kanburi,
                                                            Riichi.Yaku.TsubameGaeshi });

            // Check for any dora.
            int doraCount = hand.Parent.DoraCount;
            ITile[] doraIndicators = hand.Parent.DoraIndicators;
            for (int iDora = 0; iDora < doraCount; ++iDora)
            {
                TileType doraTile = doraIndicators[iDora].Type.GetDoraTile();
                for (int iPair = 0; iPair < 7; ++iPair)
                {
                    if (PairTiles[iPair].Type.IsEqual(doraTile))
                    {
                        Dora += 2;
                    }
                }
            }

            // Check uradora.
            bool fRiichi = Yaku.Contains(Riichi.Yaku.Riichi) ||
                           Yaku.Contains(Riichi.Yaku.DoubleRiichi) ||
                           Yaku.Contains(Riichi.Yaku.OpenRiichi);

            if (fRiichi && hand.Parent.Settings.GetSetting<bool>(GameOption.UraDora))
            {
                ITile[] uraDoraIndicators = hand.Parent.UraDoraIndicators;
                for (int iDora = 0; iDora < doraCount; ++iDora)
                {
                    TileType doraTile = uraDoraIndicators[iDora].Type.GetDoraTile();
                    for (int iPair = 0; iPair < 7; ++iPair)
                    {
                        if (PairTiles[iPair].Type.IsEqual(doraTile))
                        {
                            UraDora += 2;
                        }
                    }
                }
            }

            // Check for red dora. Will need to look at base hand for this, since our representation only shows one of the tiles of the pair.
            Global.Assert(hand.ActiveTileCount == TileHelpers.HAND_SIZE);
            foreach (ITile tt in hand.ActiveHand)
            {
                if (tt.Type.IsRedDora())
                {
                    RedDora += 1;
                }
            }
            Han += Dora + UraDora + RedDora;
            Fu = hand.Parent.Settings.GetSetting<bool>(GameOption.Chiitoi50Fu) ? 50 : 25;

            AdjustForPaaRenchan(hand, ron);
            AdjustForDoubleYakumanSetting(hand);
            return true;
        }

        internal override void ExpandAndInsert(List<CandidateHand> chBucket, IHand hand)
        {
            ITile winningTile = hand.ActiveHand[TileHelpers.HAND_SIZE - 1];
            Global.Assert(winningTile.Type.IsTile());

            for (int i = 0; i < 7; ++i)
            {
                if (PairTiles[i].Type.IsEqual(winningTile.Type))
                {
                    PairTiles[i].WinningTile = true;
                    break;
                }
            }
            chBucket.Add(this);
        }

        internal bool IteratePairs(Func<TileType, bool> callback)
        {
            foreach (TileImpl pairTile in PairTiles)
            {
                if (!callback(pairTile.Type)) { return false; }
            }
            return true;
        }
    }
}
