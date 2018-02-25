// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Helpers;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Evaluator
{
    public class SevenPairsCandidateHand : CandidateHand
    {
        public ExtendedTile[] PairTiles = new ExtendedTile[7] { new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile() };

        public override bool Evaluate(Hand hand, bool fRon)
        {
            ResetValues();

            // Check for yakuman hands.
            Han += EvaluateYakuList(hand, fRon, new Yaku[] { Riichi.Yaku.Chinroutou,
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

            if (!hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && (Han < -1)) { Han = -1; }
            if (Han < 0)                                                                        { return true; }

            // Go through all the yaku.
            Han += EvaluateYakuList(hand, fRon, new Yaku[] { Riichi.Yaku.Chiitoitsu,
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
            TileType[] doraIndicators = hand.Parent.DoraIndicators;
            for (int iDora = 0; iDora < doraCount; ++iDora)
            {
                TileType doraTile = doraIndicators[iDora].GetDoraTile();
                for (int iPair = 0; iPair < 7; ++iPair)
                {
                    if (PairTiles[iPair].Tile.IsEqual(doraTile))
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
                TileType[] uraDoraIndicators = hand.Parent.UraDoraIndicators;
                for (int iDora = 0; iDora < doraCount; ++iDora)
                {
                    TileType doraTile = uraDoraIndicators[iDora].GetDoraTile();
                    for (int iPair = 0; iPair < 7; ++iPair)
                    {
                        if (PairTiles[iPair].Tile.IsEqual(doraTile))
                        {
                            UraDora += 2;
                        }
                    }
                }
            }

            // Check for red dora. Will need to look at base hand for this, since our representation only shows one of the tiles of the pair.
            RiichiGlobal.Assert(hand.ActiveTileCount == TileHelpers.HAND_SIZE);
            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.IsRedDora())
                {
                    RedDora += 1;
                }
            }
            Han += Dora + UraDora + RedDora;

            // Set the fu to 25 or 50.
            Fu = hand.Parent.Settings.GetSetting<bool>(GameOption.Chiitoi50Fu) ? 50 : 25;

            // Done!
            return true;
        }

        public override void ExpandAndInsert(List<CandidateHand> chBucket, Hand hand)
        {
            TileType winningTile = hand.ActiveHand[TileHelpers.HAND_SIZE - 1];
            RiichiGlobal.Assert(winningTile.IsTile());

            for (int i = 0; i < 7; ++i)
            {
                if (PairTiles[i].Tile.IsEqual(winningTile))
                {
                    PairTiles[i].WinningTile = true;
                    break;
                }
            }
            chBucket.Add(this);
        }
    }
}
