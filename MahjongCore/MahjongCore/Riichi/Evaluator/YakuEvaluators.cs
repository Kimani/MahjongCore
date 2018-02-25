// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MahjongCore.Riichi.Evaluator
{
    public class YakuEvaluator
    {
        private static Dictionary<Yaku, Func<Hand, CandidateHand, bool, int>> Evaluators = new Dictionary<Yaku, Func<Hand, CandidateHand, bool, int>>();

        public static int Evaluate(Yaku y, Hand hand, CandidateHand cHand, bool fRon)
        {
            if (Evaluators.Count == 0)
            {
                Initialize();
            }

            var evaluator = Evaluators[y];
            RiichiGlobal.Assert(evaluator != null);
            int han = 0;
            if (evaluator != null)
            {
                han = evaluator.Invoke(hand, cHand, fRon);
            }
            return han;
        }

        private static void Initialize()
        {
            foreach (Yaku y in Enum.GetValues(typeof(Yaku)))
            {
                // http://stackoverflow.com/questions/2933221/can-you-get-a-funct-or-similar-from-a-methodinfo-object
                MethodInfo info = typeof(YakuEvaluator).GetMethod("Evaluate_" + y.ToString());
                var func = Delegate.CreateDelegate(typeof(Func<Hand, CandidateHand, bool, int>), info);
                Evaluators.Add(y, (Func<Hand, CandidateHand, bool, int>)func);
            }
        }

        public static int Evaluate_Riichi(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (hand.IsInReach() && !hand.IsInDoubleReach()) ? Yaku.Riichi.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_DoubleRiichi(Hand hand, CandidateHand cHand, bool fRon)
        {
            return hand.IsInDoubleReach() ? Yaku.DoubleRiichi.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_OpenRiichi(Hand hand, CandidateHand cHand, bool fRon)
        {
            List<ExtendedTile> discards = hand.Discards;

            // Don't look at the first discard - that's for double riichi to do.
            int han = hand.IsInOpenReach() ? Yaku.OpenRiichi.GetHan(true, hand.Parent.Settings) : 0;

            // If we're in open reach... if we're also in double reach, give the normal reach value to the double
            // reach. That's complicated but we'll just minus one. That'll be fine for pretty much every scenario.
            // We cooould just return the value of Yaku.Riichi.GetHan, but that might mess things up.... hrm.
            if ((han > 0) && hand.IsInDoubleReach())
            {
                --han;
            }
            return han;
        }

        public static int Evaluate_Chiitoitsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (cHand is SevenPairsCandidateHand) ? Yaku.Chiitoitsu.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Pinfu(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (hand.IsOpen() || (!hand.Parent.Settings.GetSetting<bool>(GameOption.PinfuTsumo) && !fRon))
            {
                return 0;
            }

            // Make sure each of the melds are runs.
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < 4; ++i)
            {
                if (scHand.Melds[i].State == MeldState.Pon)
                {
                    return 0;
                }
            }

            // Make sure the winning tile isn't the pair.
            if (scHand.PairTile.WinningTile)
            {
                return 0;
            }

            // Make sure the winning tile is one of the edges and isn't like, a 3 of a 1-2-3 or a 7 of a 7-8-9.
            for (int i = 0; i < 4; ++i)
            {
                if ((scHand.Melds[i].Tiles[0].WinningTile && (scHand.Melds[i].Tiles[0].Tile.GetValue() == 7)) ||
                     scHand.Melds[i].Tiles[1].WinningTile ||
                    (scHand.Melds[i].Tiles[2].WinningTile && (scHand.Melds[i].Tiles[2].Tile.GetValue() == 3)))
                {
                    return 0;
                }
            }

            // Make sure the pair is not valued.
            if (scHand.PairTile.Tile.IsEqual(TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.CurrentRound)) ||
                scHand.PairTile.Tile.IsEqual(TileTypeExtensionMethods.GetSeatWindTile(hand.Player, hand.Parent.CurrentDealer)))
            {
                return 0;
            }

            // Success!
            return Yaku.Pinfu.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_Iipeikou(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (hand.IsOpen())
            {
                return 0;
            }

            // Find two melds with the same sequence of tiles. If we have two sequences of tiles that are identical, don't count this.
            // Basically if it's a chii and it starts with the same tile, it's the same.
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            TileType[] sequences = null;
            int sCount = 0;
            int calledMeldCount = hand.GetCalledMeldCount();
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    if (sequences == null)
                    {
                        sequences = new TileType[4];
                    }
                    sequences[sCount++] = scHand.Melds[i].Tiles[0].Tile;
                }
            }

            bool fFound = false;
            if (sCount == 2)
            {
                if (sequences[0].IsEqual(sequences[1]))
                {
                    fFound = true;
                }
            }
            else if (sCount == 3)
            {
                /// Ensure there is pinfu and that this isn't Iisou Sanjun.
                if ((sequences[0].IsEqual(sequences[1]) || sequences[0].IsEqual(sequences[2]) || sequences[1].IsEqual(sequences[2])) &&
                    (!sequences[0].IsEqual(sequences[1]) || !sequences[1].IsEqual(sequences[2])))
                {
                    fFound = true;
                }
            }
            else if (sCount == 4)
            {
                // Make sure we don't have Ryanpeikou. Note that if we have Ryanpeikou, sequences 0-1 and 2-3 should be the same.
                if (sequences[0].IsEqual(sequences[1]) && sequences[2].IsEqual(sequences[3]))
                {
                    return 0;
                }

                // Make sure we don't have Iisou Sanjun.
                if ((sequences[0].IsEqual(sequences[1]) && sequences[1].IsEqual(sequences[2])) ||
                    (sequences[1].IsEqual(sequences[2]) && sequences[2].IsEqual(sequences[3])))
                {
                    return 0;
                }

                if (sequences[0].IsEqual(sequences[1]) ||
                    sequences[1].IsEqual(sequences[2]) ||
                    sequences[2].IsEqual(sequences[3]))
                {
                    fFound = true;
                }
            }
            return fFound ? Yaku.Pinfu.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_SanshokuDoujun(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Get a list of all the sequences.
            TileType[] sequences = new TileType[4];
            int sCount = 0;
            foreach (Meld m in hand.OpenMeld)
            {
                if (m.State == MeldState.Chii)
                {
                    sequences[sCount++] = m.Tiles[0].Tile;
                }
            }

            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    sequences[sCount++] = scHand.Melds[i].Tiles[0].Tile;
                }
            }

            // We're looking for the same sequence in all three suits.
            bool fSuccess = false;
            if (sCount == 3)
            {
                if ((sequences[0].GetValue() == sequences[1].GetValue()) &&
                    (sequences[0].GetValue() == sequences[2].GetValue()))
                {
                    Suit suit0 = sequences[0].GetSuit();
                    Suit suit1 = sequences[1].GetSuit();
                    Suit suit2 = sequences[2].GetSuit();

                    if (suit0 != suit1 && suit0 != suit2 && suit1 != suit2)
                    {
                        fSuccess = true;
                    }
                }
            }
            else if (sCount == 4)
            {
                int[] values = new int[3];
                Suit[] suits = new Suit[3];
                for (int i = 0; !fSuccess && (i < 4); ++i)
                {
                    int setter = 0;
                    for (int j = 0; j < 4; ++j)
                    {
                        if (j == i) { continue; }
                        values[setter] = sequences[j].GetValue();
                        suits[setter] = sequences[j].GetSuit();
                        ++setter;
                    }

                    if ((values[0] == values[1]) &&
                        (values[0] == values[2]) &&
                        (suits[0] != suits[1]) &&
                        (suits[0] != suits[2]) &&
                        (suits[1] != suits[2]))
                    {
                        fSuccess = true;
                    }
                }
            }

            // Done!
            return fSuccess ? Yaku.SanshokuDoujun.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Ittsuu(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Find sequences that are 1-2-3, 4-5-6, and 7-8-9. They need to be of the same suit.
            bool fChar123 = false;
            bool fChar456 = false;
            bool fChar789 = false;
            bool fCirc123 = false;
            bool fCirc456 = false;
            bool fCirc789 = false;
            bool fBam123 = false;
            bool fBam456 = false;
            bool fBam789 = false;

            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State == MeldState.Chii)
                {
                    Suit suit = meld.Tiles[0].Tile.GetSuit();
                    int value = meld.Tiles[0].Tile.GetValue();
                    if (value == 1)
                    {
                        if (suit == Suit.Bamboo)     { fBam123 = true; }
                        if (suit == Suit.Characters) { fChar123 = true; }
                        if (suit == Suit.Circles)    { fCirc123 = true; }
                    }
                    if (value == 4)
                    {
                        if (suit == Suit.Bamboo)     { fBam456 = true; }
                        if (suit == Suit.Characters) { fChar456 = true; }
                        if (suit == Suit.Circles)    { fCirc456 = true; }
                    }
                    if (value == 7)
                    {
                        if (suit == Suit.Bamboo)     { fBam789 = true; }
                        if (suit == Suit.Characters) { fChar789 = true; }
                        if (suit == Suit.Circles)    { fCirc789 = true; }
                    }
                }
            }

            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    Suit suit = scHand.Melds[i].Tiles[0].Tile.GetSuit();
                    int value = scHand.Melds[i].Tiles[0].Tile.GetValue();
                    if (value == 1)
                    {
                        if (suit == Suit.Bamboo)     { fBam123 = true; }
                        if (suit == Suit.Characters) { fChar123 = true; }
                        if (suit == Suit.Circles)    { fCirc123 = true; }
                    }
                    if (value == 4)
                    {
                        if (suit == Suit.Bamboo)     { fBam456 = true; }
                        if (suit == Suit.Characters) { fChar456 = true; }
                        if (suit == Suit.Circles)    { fCirc456 = true; }
                    }
                    if (value == 7)
                    {
                        if (suit == Suit.Bamboo)     { fBam789 = true; }
                        if (suit == Suit.Characters) { fChar789 = true; }
                        if (suit == Suit.Circles)    { fCirc789 = true; }
                    }
                }
            }

            // If we have all three, then we have Ittsuu.
            return ((fBam123 && fBam456 && fBam789) ||
                    (fChar123 && fChar456 && fChar789) ||
                    (fCirc123 && fCirc456 && fCirc789)) ? Yaku.Ittsuu.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Ryanpeikou(Hand hand, CandidateHand cHand, bool fRon)
        {
            // If we have any "open melds" (ie even concealed kans) then we don't have all
            // 4 melds as concealed runs we need to get ryanpeikou.
            if (hand.GetCalledMeldCount() > 0)
            {
                return 0;
            }

            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < 4; ++i)
            {
                if (scHand.Melds[i].State != MeldState.Chii)
                {
                    return 0;
                }
            }

            // Get the first tile of all the melds if they're sequences.
            return (scHand.Melds[0].Tiles[0].Tile.IsEqual(scHand.Melds[1].Tiles[0].Tile) &&
                    scHand.Melds[2].Tiles[0].Tile.IsEqual(scHand.Melds[3].Tiles[0].Tile)) ? Yaku.Ryanpeikou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Toitoi(Hand hand, CandidateHand cHand, bool fRon)
        {
            Meld[] openMeld = hand.OpenMeld;
            if ((openMeld[0].State == MeldState.Chii) ||
                (openMeld[1].State == MeldState.Chii) ||
                (openMeld[2].State == MeldState.Chii) ||
                (openMeld[3].State == MeldState.Chii))
            {
                return 0;
            }

            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    return 0;
                }
            }
            return Yaku.Toitoi.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Sanankou(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Go through all the closed melds. Count how many are sets. If any of them have
            // a winning tile and fRon is true, then it doesn't count. If we get 3+, then we have sanankou.
            // Note that if we have make a meld with a Rinshan Kaihou with an open kan, it should still
            // count since it's still tsumo even though it's scored as a ron.
            int validSets = 0;
            int calledMeldCount = hand.GetCalledMeldCount();
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Pon)
                {
                    if (!fRon || (!scHand.Melds[i].Tiles[0].WinningTile &&
                                  !scHand.Melds[i].Tiles[1].WinningTile &&
                                  !scHand.Melds[i].Tiles[2].WinningTile))
                    {
                        validSets++;
                    }
                }
            }

            // Also go through all the open melds - if any are closed kans, then those count.
            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State == MeldState.KanConcealed)
                {
                    validSets++;
                }
            }
            return (validSets >= 3) ? Yaku.Sanankou.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_SanshokuDoukou(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;

            int sCount = 0;
            TileType[] sets = null;

            // Look through the closed sets.
            int calledMeldCount = hand.GetCalledMeldCount();
            for (int iMeld = 0; iMeld < (4 - calledMeldCount); ++iMeld)
            {
                if ((scHand.Melds[iMeld].State == MeldState.Pon) && !scHand.Melds[iMeld].Tiles[0].Tile.IsHonor())
                {
                    if (sets == null) { sets = new TileType[4]; }
                    sets[sCount++] = scHand.Melds[iMeld].Tiles[0].Tile;
                }
            }

            // Go through the open melds, looking at pons and kans.
            foreach (Meld meld in hand.OpenMeld)
            {
                if (((meld.State == MeldState.Pon) ||
                     (meld.State == MeldState.KanOpen) ||
                     (meld.State == MeldState.KanPromoted) ||
                     (meld.State == MeldState.KanConcealed)) && !meld.Tiles[0].Tile.IsHonor())
                {
                    if (sets == null) { sets = new TileType[4]; }
                    sets[sCount++] = meld.Tiles[0].Tile;
                }
            }

            // See if we enough to make the yaku!
            bool fFound = false;
            if (sCount == 3)
            {
                int value1 = sets[0].GetValue();
                int value2 = sets[1].GetValue();
                int value3 = sets[2].GetValue();
                Suit suit1 = sets[0].GetSuit();
                Suit suit2 = sets[1].GetSuit();
                Suit suit3 = sets[2].GetSuit();

                if ((value1 == value2) && (value1 == value3) && (suit1 != suit2) && (suit1 != suit3) && (suit2 != suit3))
                {
                    fFound = true;
                }
            }
            else if (sCount == 4)
            {
                int[] values = new int[3];
                Suit[] suits = new Suit[3];
                for (int i = 0; i < 4; ++i)
                {
                    int setter = 0;
                    for (int j = 0; j < 4; ++j)
                    {
                        if (i == j) { continue; }
                        values[setter] = sets[j].GetValue();
                        suits[setter] = sets[j].GetSuit();
                        ++setter;
                    }

                    if ((values[0] == values[1]) &&
                        (values[0] == values[2]) &&
                        (suits[0] != suits[1]) &&
                        (suits[0] != suits[2]) &&
                        (suits[1] != suits[2]))
                    {
                        fFound = true;
                    }
                }
            }

            return fFound ? Yaku.SanshokuDoukou.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Sankantsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            int quads = 0;
            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State.GetMeldType() == MeldType.Kan)
                {
                    quads++;
                }
            }
            return (quads == 3) ? Yaku.Sankantsu.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Tanyao(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (!hand.Parent.Settings.GetSetting<bool>(GameOption.Kuitan))
            {
                foreach (Meld meld in hand.OpenMeld)
                {
                    if ((meld.State != MeldState.None) && (meld.State != MeldState.KanConcealed))
                    {
                        return 0;
                    }
                }
            }

            // Look at the open melds. if we have any terminals or honors, return 0.
            foreach (Meld meld in hand.OpenMeld)
            {
                for (int iTile = meld.State.GetTileCount() - 1; iTile >= 0; --iTile)
                {
                    if (meld.Tiles[iTile].Tile.IsTerminalOrHonor())
                    {
                        return 0;
                    }
                }
            }

            // Look at the active hand.
            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.IsTerminalOrHonor())
                {
                    return 0;
                }
            }
            return Yaku.Tanyao.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        private static int Evaluate_DragonYakuhai(Hand hand, StandardCandidateHand scHand, bool fFindChun, bool fFindHaku, bool fFindHatsu, int score, GameOption setting)
        {
            // Tally up if the chun, haku, and hatsu are in use.
            bool fChun = false;
            bool fHaku = false;
            bool fHatsu = false;

            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State != MeldState.None)
                {
                    if (meld.Tiles[0].Tile == TileType.Chun)  { fChun = true; }
                    if (meld.Tiles[0].Tile == TileType.Haku)  { fHaku = true; }
                    if (meld.Tiles[0].Tile == TileType.Hatsu) { fHatsu = true; }
                }
            }

            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Chun))  { fChun = true; }
                if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Haku))  { fHaku = true; }
                if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Hatsu)) { fHatsu = true; }
            }

            if (!fFindChun  && scHand.PairTile.Tile.IsEqual(TileType.Chun))  { fChun = true; }
            if (!fFindHaku  && scHand.PairTile.Tile.IsEqual(TileType.Haku))  { fHaku = true; }
            if (!fFindHatsu && scHand.PairTile.Tile.IsEqual(TileType.Hatsu)) { fHatsu = true; }

            // Unless we're looking for all three, don't return true specifically if we have all three.
            if ((!fFindChun || !fFindHaku || !fFindHatsu) && fChun && fHaku && fHatsu)
            {
                return 0;
            }
            return ((!fChun && fFindChun) ||
                    (!fHaku && fFindHaku) ||
                    (!fHatsu && fFindHatsu)) ? 0 : score;
        }

        public static int Evaluate_Chun(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_DragonYakuhai(hand, (cHand as StandardCandidateHand), true, false, false, 1, GameOption.Chun);
        }

        public static int Evaluate_Haku(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_DragonYakuhai(hand, (cHand as StandardCandidateHand), false, true, false, 1, GameOption.Haku);
        }

        public static int Evaluate_Hatsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_DragonYakuhai(hand, (cHand as StandardCandidateHand), false, false, true, 1, GameOption.Hatsu);
        }

        public static int Evaluate_Shousangen(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_DragonYakuhai(hand, (cHand as StandardCandidateHand), true, true, true, 4, GameOption.Shousangen);
        }

        public static int Evaluate_Daisangen(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_DragonYakuhai(hand, (cHand as StandardCandidateHand), true, true, true, -1, GameOption.Daisangen);
        }

        private static int EvaluateWindYakuhai(Hand hand, StandardCandidateHand scHand, TileType tile, bool fDouble, GameOption yakuhaiSetting, Yaku yaku)
        {
            bool fFound = false;
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State != MeldState.None) && meld.Tiles[0].Tile.IsEqual(tile))
                {
                    fFound = true;
                    break;
                }
            }

            for (int i = 0; !fFound && i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].Tiles[0].Tile.IsEqual(tile))
                {
                    fFound = true;
                }
            }

            if (fFound)
            {
                int nCount = 0;
                if (TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.CurrentRound).IsEqual(tile))
                {
                    ++nCount;
                }

                if (hand.Seat.IsEqual(tile))
                {
                    ++nCount;
                }

                return (nCount == (fDouble ? 2 : 1)) ? yaku.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_Ton(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.East, false, GameOption.Ton, Yaku.Ton);
        }

        public static int Evaluate_DoubleTon(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.East, true, GameOption.DoubleTon, Yaku.DoubleTon);
        }

        public static int Evaluate_Nan(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.South, false, GameOption.Nan, Yaku.Nan);
        }

        public static int Evaluate_DoubleNan(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.South, true, GameOption.DoubleNan, Yaku.DoubleNan);
        }

        public static int Evaluate_Sha(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.West, false, GameOption.Sha, Yaku.Sha);
        }

        public static int Evaluate_DoubleSha(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.West, true, GameOption.DoubleSha, Yaku.DoubleSha);
        }

        public static int Evaluate_Pei(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.North, false, GameOption.Pei, Yaku.Pei);
        }

        public static int Evaluate_DoublePei(Hand hand, CandidateHand cHand, bool fRon)
        {
            return EvaluateWindYakuhai(hand, (cHand as StandardCandidateHand), TileType.North, true, GameOption.DoublePei, Yaku.DoublePei);
        }

        public static int Evaluate_Chanta(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            if (!scHand.PairTile.Tile.IsTerminalOrHonor())
            {
                return 0;
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                // Check calls for terminals. If it's a chii check either end for a 1 or 9. Otherwise it's a pon/kan, so check if it's an honor or terminal.
                if (meld.State == MeldState.Chii)
                {
                    if ((meld.Tiles[0].Tile.GetValue() != 1) && (meld.Tiles[2].Tile.GetValue() != 9))
                    {
                        return 0;
                    }
                }
                else if ((meld.State != MeldState.None) && !meld.Tiles[0].Tile.IsTerminalOrHonor())
                {
                    return 0;
                }
            }

            for (int iMeld = 0; iMeld < (4 - hand.GetCalledMeldCount()); ++iMeld)
            {
                Meld closedMeld = scHand.Melds[iMeld];
                if (((closedMeld.State == MeldState.Chii) && (closedMeld.Tiles[0].Tile.GetValue() != 1) && (closedMeld.Tiles[2].Tile.GetValue() != 9)) ||
                    ((closedMeld.State != MeldState.None) && !closedMeld.Tiles[0].Tile.IsTerminalOrHonor()))
                {
                    return 0;
                }
            }

            return Yaku.Chanta.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Honitsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Look through all the tiles and the melds.
            bool fCircs = false;
            bool fChars = false;
            bool fBambs = false;
            bool fHonors = false;

            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State != MeldState.None)
                {
                    Suit suit = meld.Tiles[0].Tile.GetSuit();
                    if      (suit == Suit.Bamboo)     { fBambs = true; }
                    else if (suit == Suit.Characters) { fChars = true; }
                    else if (suit == Suit.Circles)    { fCircs = true; }
                    else                              { fHonors = true; }
                }
            }

            
            foreach (TileType tile in hand.ActiveHand)
            {
                if (tile != TileType.None)
                {
                    Suit suit = tile.GetSuit();
                    if      (suit == Suit.Bamboo)     { fBambs = true; }
                    else if (suit == Suit.Characters) { fChars = true; }
                    else if (suit == Suit.Circles)    { fCircs = true; }
                    else                              { fHonors = true; }
                }
            }

            // Tally up the suits used.
            int suits = 0;
            if (fCircs) { suits++; }
            if (fChars) { suits++; }
            if (fBambs) { suits++; }
            return (fHonors && (suits == 1)) ? Yaku.Honitsu.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Junchan(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            if (!scHand.PairTile.Tile.IsTerminal())
            {
                return 0;
            }

            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (!(scHand.Melds[i].Tiles[0].Tile.IsTerminal() || scHand.Melds[i].Tiles[1].Tile.IsTerminal() || scHand.Melds[i].Tiles[2].Tile.IsTerminal()))
                {
                    return 0;
                }
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State != MeldState.None) && !(meld.Tiles[0].Tile.IsTerminal() || meld.Tiles[1].Tile.IsTerminal() || meld.Tiles[2].Tile.IsTerminal()))
                {
                    return 0;
                }
            }
            return Yaku.Junchan.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Honroutou(Hand hand, CandidateHand cHand, bool fRon)
        {
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Chii) || ((meld.State != MeldState.None) && !meld.Tiles[0].Tile.IsTerminalOrHonor()))
                {
                    return 0;
                }
            }

            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.IsTile() && !tt.IsTerminalOrHonor())
                {
                    return 0;
                }
            }
            return Yaku.Honroutou.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Chinitsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            Suit suit = hand.ActiveHand[0].GetSuit();

            // Check all the open melds.
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State != MeldState.None) && (meld.Tiles[0].Tile.GetSuit() != suit))
                {
                    return 0;
                }
            }

            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.IsTile() && (tt.GetSuit() != suit))
                {
                    return 0;
                }
            }
            return Yaku.Chinitsu.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_MenzenTsumo(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (!fRon && hand.IsClosed()) ? Yaku.MenzenTsumo.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Ippatsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            return hand.IsIppatsu() ? Yaku.Ippatsu.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_HaiteiRaoyue(Hand hand, CandidateHand cHand, bool fRon)
        {
            return ((hand.Parent.TilesRemaining == 0) && !fRon) ? Yaku.HaiteiRaoyue.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_HouteiRaoyui(Hand hand, CandidateHand cHand, bool fRon)
        {
            return ((hand.Parent.TilesRemaining == 0) && fRon) ? Yaku.HouteiRaoyui.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_RinshanKaihou(Hand hand, CandidateHand cHand, bool fRon)
        {
            return hand.Parent.PlayerDeadWallPick ? Yaku.RinshanKaihou.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Chankan(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (hand.Parent.ChankanFlag) ? Yaku.Chankan.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_NagashiMangan(Hand hand, CandidateHand cHand, bool fRon)
        {
            if ((hand.Parent.TilesRemaining != 0) || (hand.GetCalledMeldCount() > 0))
            {
                return 0;
            }

            foreach (ExtendedTile et in hand.Discards)
            {
                if (et.Called || !et.Tile.IsTerminalOrHonor())
                {
                    return 0;
                }
            }
            return Yaku.NagashiMangan.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_KokushiMusou(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (hand.GetCalledMeldCount() > 0)
            {
                return 0;
            }

            int nCirc1 = 0;
            int nCirc9 = 0;
            int nChar1 = 0;
            int nChar9 = 0;
            int nBamb1 = 0;
            int nBamb9 = 0;
            int nChun = 0;
            int nHaku = 0;
            int nHatsu = 0;
            int nEast = 0;
            int nWest = 0;
            int nNorth = 0;
            int nSouth = 0;

            foreach (TileType tt in hand.ActiveHand)
            {
                if (!tt.IsTerminalOrHonor())
                {
                    return 0;
                }

                if      (tt == TileType.Bamboo1)     { nBamb1++; }
                else if (tt == TileType.Bamboo9)     { nBamb9++; }
                else if (tt == TileType.Characters1) { nChar1++; }
                else if (tt == TileType.Characters9) { nChar9++; }
                else if (tt == TileType.Circles1)    { nCirc1++; }
                else if (tt == TileType.Circles9)    { nCirc9++; }
                else if (tt == TileType.Chun)        { nChun++; }
                else if (tt == TileType.Hatsu)       { nHatsu++; }
                else if (tt == TileType.Haku)        { nHaku++; }
                else if (tt == TileType.East)        { nEast++; }
                else if (tt == TileType.West)        { nWest++; }
                else if (tt == TileType.North)       { nNorth++; }
                else if (tt == TileType.South)       { nSouth++; }
            }

            if ((nBamb1 == 0) || (nBamb9 == 0) || (nCirc1 == 0) || (nCirc9 == 0) || (nChar1 == 0) || (nChar9 == 0) || (nChun == 0) ||
                (nHatsu == 0) || (nHaku == 0) || (nEast == 0) || (nWest == 0) || (nNorth == 0) || (nSouth == 0))
            {
                return 0;
            }

            // Check to see if we have a 13 way wait. If the last tile in the
            // active hand is the same as any other tile, then double yakuman!
            int han = Yaku.KokushiMusou.GetHan(true, hand.Parent.Settings);
            if ((han != 0) && hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman))
            {
                TileType winTile = hand.ActiveHand[13];
                for (int i = 0; i < 13; ++i)
                {
                    if (hand.ActiveHand[i] == winTile)
                    {
                        --han;
                        break;
                    }
                }
            }
            return han;
        }

        public static int Evaluate_ChuurenPoutou(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (hand.GetCalledMeldCount() > 0)
            {
                return 0;
            }

            // Make sure it's all the same suit.
            Suit suit = hand.ActiveHand[0].GetSuit();
            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.GetSuit() != suit)
                {
                    return 0;
                }
            }

            // Make sure we have the correct amount of each value.
            int[] values = new int[10]; // values[0] isn't used.
            foreach (TileType tt in hand.ActiveHand)
            {
                values[tt.GetValue()]++;
            }

            if ((values[1] < 3) || (values[2] < 1) || (values[3] < 1) ||
                (values[4] < 1) || (values[5] < 1) || (values[6] < 1) ||
                (values[7] < 1) || (values[8] < 1) || (values[9] < 3))
            {
                return 0;
            }

            // Success! If the winning tile is the one that makes it go over 1 or 3, then we get a double yakuman.
            int winValueCount = values[hand.ActiveHand[13].GetValue()];
            int han = Yaku.ChuurenPoutou.GetHan(true, hand.Parent.Settings);
            if ((han != -1) && hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && ((winValueCount == 2) || (winValueCount == 4)))
            {
                --han;
            }
            return han;
        }

        public static int Evaluate_Suuankou(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (fRon || hand.IsOpen())
            {
                return 0;
            }

            // Only need to look at closed melds.
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if (scHand.Melds[i].State != MeldState.Pon)
                {
                    return 0;
                }
            }

            // If the wait is on the pair, then it's double yakuman.
            int han = Yaku.Suuankou.GetHan(true, hand.Parent.Settings);
            if ((han != 0) && !hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && scHand.PairTile.WinningTile)
            {
                --han;
            }
            return han;
        }

        private static int Evaluate_Suushii(Hand hand, StandardCandidateHand scHand, bool fCheckPair, GameOption setting, Yaku yaku)
        {
            bool fNorth = false;
            bool fSouth = false;
            bool fEast = false;
            bool fWest = false;

            if (fCheckPair)
            {
                if      (scHand.PairTile.Tile.IsEqual(TileType.North)) { fNorth = true; }
                else if (scHand.PairTile.Tile.IsEqual(TileType.South)) { fSouth = true; }
                else if (scHand.PairTile.Tile.IsEqual(TileType.East))  { fEast = true; }
                else if (scHand.PairTile.Tile.IsEqual(TileType.West))  { fWest = true; }
                else                                                   { return 0; }
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State != MeldState.None)
                {
                    if      (meld.Tiles[0].Tile.IsEqual(TileType.North)) { fNorth = true; }
                    else if (meld.Tiles[0].Tile.IsEqual(TileType.South)) { fSouth = true; }
                    else if (meld.Tiles[0].Tile.IsEqual(TileType.East))  { fEast = true; }
                    else if (meld.Tiles[0].Tile.IsEqual(TileType.West))  { fWest = true; }
                }
            }

            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if      (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.North)) { fNorth = true; }
                else if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.South)) { fSouth = true; }
                else if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.East))  { fEast = true; }
                else if (scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.West))  { fWest = true; }
            }
            return (fNorth && fSouth && fEast && fWest) ? yaku.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Shousuushii(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_Suushii(hand, cHand as StandardCandidateHand, true, GameOption.Shousuushii, Yaku.Shousuushii);
        }

        public static int Evaluate_Daisuushii(Hand hand, CandidateHand cHand, bool fRon)
        {
            return Evaluate_Suushii(hand, cHand as StandardCandidateHand, true, GameOption.Daisuushii, Yaku.Daisuushii);
        }

        public static int Evaluate_Suukantsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            Meld[] openMeld = hand.OpenMeld;
            if ((openMeld[0].State.GetMeldType() == MeldType.Kan) && (openMeld[1].State.GetMeldType() == MeldType.Kan) &&
                (openMeld[2].State.GetMeldType() == MeldType.Kan) && (openMeld[3].State.GetMeldType() == MeldType.Kan))
            {
                int han = Yaku.Suukantsu.GetHan(hand.IsClosed(), hand.Parent.Settings);
                if ((hand.Parent.Settings.GetSetting<bool>(GameOption.FourQuadRinshan) &&
                     hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) &&
                     hand.Parent.PlayerDeadWallPick) ||
                    (hand.Parent.Settings.GetSetting<bool>(GameOption.SuukantsuDoubleYakuman) &&
                     hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman)))
                {
                    --han;
                }
                return han;
            }
            return 0;
        }

        private static bool CheckRyuuiisouMeld(Meld meld)
        {
            if (meld.State == MeldState.Chii)
            {
                Suit suit = meld.Tiles[0].Tile.GetSuit();
                int value = Math.Min(meld.Tiles[0].Tile.GetValue(), meld.Tiles[1].Tile.GetValue());
                if ((suit != Suit.Bamboo) || (value != 2))
                {
                    return false;
                }
            }
            else if (meld.State != MeldState.None)
            {
                if (!meld.Tiles[0].Tile.IsEqual(TileType.Bamboo2) && !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo3) &&
                    !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo4) && !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo6) &&
                    !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo8) && !meld.Tiles[0].Tile.IsEqual(TileType.Hatsu))
                {
                    return false;
                }
            }
            return true;
        }

        public static int Evaluate_Ryuuiisou(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            foreach (Meld meld in hand.OpenMeld) { if (!CheckRyuuiisouMeld(meld)) { return 0; } }
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i) { if (!CheckRyuuiisouMeld(scHand.Melds[i])) { return 0; } }

            if (!scHand.PairTile.Tile.IsEqual(TileType.Bamboo2) && !scHand.PairTile.Tile.IsEqual(TileType.Bamboo3) &&
                !scHand.PairTile.Tile.IsEqual(TileType.Bamboo4) && !scHand.PairTile.Tile.IsEqual(TileType.Bamboo6) &&
                !scHand.PairTile.Tile.IsEqual(TileType.Bamboo8) && !scHand.PairTile.Tile.IsEqual(TileType.Hatsu))
            {
                return 0;
            }
            return Yaku.Ryuuiisou.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Chinroutou(Hand hand, CandidateHand cHand, bool fRon)
        {
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Chii) || ((meld.State != MeldState.None) && !meld.Tiles[0].Tile.IsTerminal()))
                {
                    return 0;
                }
            }

            foreach (TileType tt in hand.ActiveHand)
            {
                if (!tt.IsTerminal())
                {
                    return 0;
                }
            }
            return Yaku.Chinroutou.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Tsuuiisou(Hand hand, CandidateHand cHand, bool fRon)
        {
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Chii) || ((meld.State != MeldState.None) && !meld.Tiles[0].Tile.IsHonor()))
                {
                    return 0;
                }
            }

            foreach (TileType tt in hand.ActiveHand)
            {
                if (!tt.IsHonor())
                {
                    return 0;
                }
            }
            return Yaku.Tsuuiisou.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_Daisharin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return (!spHand.PairTiles[0].Tile.IsEqual(TileType.Circles2) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles3) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles4) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles5) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles6) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles7) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Circles8)) ? 0 : Yaku.Daisharin.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_Daisuurin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return (!spHand.PairTiles[0].Tile.IsEqual(TileType.Characters2) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters3) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters4) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters5) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters6) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters7) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Characters8)) ? 0 : Yaku.Daisuurin.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_Daichikurin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return (!spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo2) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo3) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo4) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo5) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo6) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo7) ||
                    !spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo8)) ? 0 : Yaku.Daichikurin.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_ShiisanBudou(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Ensure that this is the first go around and noone has called.
            List<ExtendedTile> discards = hand.Discards;
            if ((hand.Parent.Player1Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player2Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player3Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player4Hand.GetCalledMeldCount() > 0) ||
                (discards.Count > 0))
            {
                return 0;
            }

            // Get the sorted list.
            TileType[] sortedList = new TileType[14];
            for (int i = 0; i < 14; ++i)
            {
                sortedList[i] = hand.ActiveHand[i];
            }
            Array.Sort(sortedList);

            // Check all adjacent pairs. We should only get one pair of tiles and no pair should be adjacent by 1 or 2.
            bool fPair = false;
            for (int i = 0; i < 13; ++i)
            {
                TileType tileA = sortedList[i];
                TileType tileB = sortedList[i + 1];

                if (tileA.IsEqual(tileB))
                {
                    if (fPair)
                    {
                        return 0;
                    }
                    fPair = true;
                }
                else if (tileA.GetSuit() == tileB.GetSuit())
                {
                    if (Math.Abs(tileA.GetValue() - tileB.GetValue()) < 3)
                    {
                        return 0;
                    }
                }
            }
            return fPair ? Yaku.ShiisanBudou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Chiihou(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (!hand.IsDealer() && (hand.Discards.Count == 0)) ? Yaku.Chiihou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Renhou(Hand hand, CandidateHand cHand, bool fRon)
        {
            // If tsumo, then we don't get this.
            if (!fRon)
            {
                return 0;
            }

            // If anyone has done any calling, or has already discarded, then we don't get this.
            if ((hand.Parent.Player1Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player2Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player3Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player4Hand.GetCalledMeldCount() > 0) ||
                (hand.Discards.Count > 0))
            {
                return 0;
            }

            // Success!
            return Yaku.Renhou.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_Tenhou(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (hand.IsDealer() && (hand.Discards.Count == 0)) ? Yaku.Tenhou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Daichisei(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return (!spHand.PairTiles[0].Tile.IsHonor() ||
                    !spHand.PairTiles[1].Tile.IsHonor() ||
                    !spHand.PairTiles[2].Tile.IsHonor() ||
                    !spHand.PairTiles[3].Tile.IsHonor() ||
                    !spHand.PairTiles[4].Tile.IsHonor() ||
                    !spHand.PairTiles[5].Tile.IsHonor() ||
                    !spHand.PairTiles[6].Tile.IsHonor()) ? 0 : -1;
        }

        public static int Evaluate_Sanrenkou(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            TileType[] triplets = new TileType[4];
            int tCount = 0;
            int calledMeldCount = hand.GetCalledMeldCount();
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if ((scHand.Melds[i].State == MeldState.Pon) ||
                    (scHand.Melds[i].State == MeldState.KanConcealed) ||
                    (scHand.Melds[i].State == MeldState.KanOpen) ||
                    (scHand.Melds[i].State == MeldState.KanPromoted))
                {
                    triplets[tCount++] = scHand.Melds[i].Tiles[0].Tile;
                }
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Pon) ||
                    (meld.State == MeldState.KanConcealed) ||
                    (meld.State == MeldState.KanOpen) ||
                    (meld.State == MeldState.KanPromoted))
                {
                    triplets[tCount++] = meld.Tiles[0].Tile;
                }
            }

            if (tCount >= 3)
            {
                Array.Sort(triplets);
                for (int i = 3; i <= tCount; ++i)
                {
                    TileType tileA = triplets[i - 3];
                    TileType tileB = triplets[i - 2];
                    TileType tileC = triplets[i - 1];
                    Suit suitA = tileA.GetSuit();
                    Suit suitB = tileB.GetSuit();
                    Suit suitC = tileC.GetSuit();
                    if ((suitA == suitB) && (suitB == suitC))
                    {
                        if (tileA.IsNext(tileB) && tileB.IsNext(tileC))
                        {
                            return Yaku.Sanrenkou.GetHan(hand.IsClosed(), hand.Parent.Settings);
                        }
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_IisouSanjun(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;

            // Find two melds with the same sequence of tiles. If we have two sequences of tiles that are identical, don't count this.
            // Basically if it's a chii and it starts with the same tile, it's the same.
            TileType[] sequences = new TileType[4];
            int sCount = 0;
            int calledMeldCount = hand.GetCalledMeldCount();
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    Suit suit = scHand.Melds[i].Tiles[0].Tile.GetSuit();
                    int value = Math.Min(scHand.Melds[i].Tiles[0].Tile.GetValue(),
                                Math.Min(scHand.Melds[i].Tiles[1].Tile.GetValue(),
                                         scHand.Melds[i].Tiles[2].Tile.GetValue()));
                    sequences[sCount++] = TileHelpers.BuildTile(suit, value);
                }
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State == MeldState.Chii)
                {
                    Suit suit = meld.Tiles[0].Tile.GetSuit();
                    int value = Math.Min(meld.Tiles[0].Tile.GetValue(),
                                Math.Min(meld.Tiles[1].Tile.GetValue(),
                                         meld.Tiles[2].Tile.GetValue()));
                    sequences[sCount++] = TileHelpers.BuildTile(suit, value);
                }
            }

            if (sCount >= 3)
            {
                Array.Sort(sequences);
                if ((sequences[0].IsEqual(sequences[1]) && sequences[1].IsEqual(sequences[2]) && !sequences[2].IsEqual(sequences[3])) ||
                    (sequences[1].IsEqual(sequences[2]) && sequences[2].IsEqual(sequences[3]) && !sequences[0].IsEqual(sequences[1])))
                {
                    return Yaku.IisouSanjun.GetHan(hand.IsClosed(), hand.Parent.Settings);
                }
            }

            return 0;
        }

        public static int Evaluate_Suurenkou(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            TileType[] triplets = new TileType[4];
            int tCount = 0;
            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if ((scHand.Melds[i].State.GetMeldType() == MeldType.Pon) || (scHand.Melds[i].State.GetMeldType() == MeldType.Kan))
                {
                    triplets[tCount++] = scHand.Melds[i].Tiles[0].Tile;
                }
            }

            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State.GetMeldType() == MeldType.Pon) || (meld.State.GetMeldType() == MeldType.Kan))
                {
                    triplets[tCount++] = meld.Tiles[0].Tile;
                }
            }

            if (tCount >= 4)
            {
                Array.Sort(triplets);
                Suit suitA = triplets[0].GetSuit();
                Suit suitB = triplets[1].GetSuit();
                Suit suitC = triplets[2].GetSuit();
                Suit suitD = triplets[3].GetSuit();
                if ((suitA == suitB) && (suitB == suitC) && (suitC == suitD))
                {
                    if (triplets[0].IsNext(triplets[1]) && triplets[1].IsNext(triplets[2]) && triplets[2].IsNext(triplets[3]))
                    {
                        return Yaku.Suurenkou.GetHan(hand.IsClosed(), hand.Parent.Settings);
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_HyakumanGoku(Hand hand, CandidateHand cHand, bool fRon)
        {
            int nManzuTotal = 0;

            // Count all the open meld tiles.
            foreach (Meld meld in hand.OpenMeld)
            {
                if (meld.State != MeldState.None)
                {
                    for (int iTile = 0; iTile < meld.State.GetTileCount(); ++iTile)
                    {
                        if (meld.Tiles[iTile].Tile.GetSuit() != Suit.Characters)
                        {
                            return 0;
                        }
                        nManzuTotal += meld.Tiles[iTile].Tile.GetValue();
                    }
                }
            }

            // Count all the tiles in the hand.
            foreach (TileType tt in hand.ActiveHand)
            {
                if (tt.IsTile())
                {
                    if (tt.GetSuit() != Suit.Characters)
                    {
                        return 0;
                    }
                    nManzuTotal += tt.GetValue();
                }
            }
            return (nManzuTotal >= 100) ? Yaku.HyakumanGoku.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_BeniKujaku(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Chii) ||
                    ((meld.State != MeldState.None) &&
                     (!meld.Tiles[0].Tile.IsEqual(TileType.Bamboo1) &&
                      !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo5) &&
                      !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo7) &&
                      !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo9) &&
                      !meld.Tiles[0].Tile.IsEqual(TileType.Chun))))
                {
                    return 0;
                }
            }

            for (int i = 0; i < (4 - hand.GetCalledMeldCount()); ++i)
            {
                if ((scHand.Melds[i].State == MeldState.Chii) ||
                    (!scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Bamboo1) && !scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Bamboo5) &&
                     !scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Bamboo7) && !scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Bamboo9) &&
                     !scHand.Melds[i].Tiles[0].Tile.IsEqual(TileType.Chun)))
                {
                    return 0;
                }
            }

            if (!scHand.PairTile.Tile.IsEqual(TileType.Bamboo1) && !scHand.PairTile.Tile.IsEqual(TileType.Bamboo5) &&
                !scHand.PairTile.Tile.IsEqual(TileType.Bamboo7) && !scHand.PairTile.Tile.IsEqual(TileType.Bamboo9) &&
                !scHand.PairTile.Tile.IsEqual(TileType.Chun))
            {
                return 0;
            }
            return Yaku.BeniKujaku.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_AoNoDoumon(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;

            bool success = false;
            if (spHand != null)
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (!spHand.PairTiles[i].Tile.IsEqual(TileType.Circles2) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.Circles4) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.Circles8) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.North) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.East) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.South) &&
                        !spHand.PairTiles[i].Tile.IsEqual(TileType.West))
                    {
                        return 0;
                    }
                }
                return -1;
            }
            else if (scHand != null)
            {
                foreach (Meld meld in hand.OpenMeld)
                {
                    if ((meld.State == MeldState.Chii) ||
                        ((meld.State != MeldState.None) &&
                            (!meld.Tiles[0].Tile.IsEqual(TileType.Circles2) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.Circles4) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.Circles8) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.North) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.East) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.South) &&
                            !meld.Tiles[0].Tile.IsEqual(TileType.West))))
                    {
                        return 0;
                    }
                }

                for (int iClosedMeld = 0; iClosedMeld < (4 - hand.GetCalledMeldCount()); ++iClosedMeld)
                {
                    if ((scHand.Melds[iClosedMeld].State == MeldState.Chii) ||
                        (!scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Circles2) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Circles4) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Circles8) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.North) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.East) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.South) &&
                            !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.West)))
                    {
                        return 0;
                    }
                }

                if (!scHand.PairTile.Tile.IsEqual(TileType.Circles2) && !scHand.PairTile.Tile.IsEqual(TileType.Circles4) &&
                    !scHand.PairTile.Tile.IsEqual(TileType.Circles8) && !scHand.PairTile.Tile.IsEqual(TileType.North) &&
                    !scHand.PairTile.Tile.IsEqual(TileType.East) && !scHand.PairTile.Tile.IsEqual(TileType.South) &&
                    !scHand.PairTile.Tile.IsEqual(TileType.West))
                {
                    return 0;
                }
            }
            return success ? Yaku.AoNoDoumon.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_ShiisuuPuuta(Hand hand, CandidateHand cHand, bool fRon)
        {
            // Ensure that this is the first go around and noone has called.
            if ((hand.Parent.Player1Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player2Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player3Hand.GetCalledMeldCount() > 0) ||
                (hand.Parent.Player4Hand.GetCalledMeldCount() > 0) ||
                (hand.Discards.Count > 0))
            {
                return 0;
            }

            // Get the sorted list.
            TileType[] sortedList = (TileType[])hand.ActiveHand.Clone();
            Array.Sort(sortedList);

            // Check all tiles. We should get no pairs and no two tiles should be adjacent by 1 or 2.
            for (int i = 0; i < 13; ++i)
            {
                TileType tileA = sortedList[i];
                TileType tileB = sortedList[i + 1];

                if (tileA.GetSuit() == tileB.GetSuit())
                {
                    if (Math.Abs(tileA.GetValue() - tileB.GetValue()) < 3)
                    {
                        return 0;
                    }
                }
            }
            return Yaku.ShiisuuPuuta.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_UupinKaihou(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            if (hand.Parent.PlayerDeadWallPick)
            {
                bool found = false;
                foreach (Meld cMeld in scHand.Melds)
                {
                    if (cMeld.State != MeldState.None)
                    {
                        for (int iTile = 0; iTile < 3; ++iTile)
                        {
                            if (cMeld.Tiles[iTile].WinningTile && cMeld.Tiles[iTile].Tile.IsEqual(TileType.Circles5))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (scHand.PairTile.WinningTile && scHand.PairTile.Tile.IsEqual(TileType.Circles5))
                {
                    found = true;
                }
                return found ? Yaku.UupinKaihou.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_IipinRaoyui(Hand hand, CandidateHand cHand, bool fRon)
        {
            // standards
            if (hand.Parent.TilesRemaining == 0)
            {
                StandardCandidateHand scHand = cHand as StandardCandidateHand;
                SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
                bool found = false;
                if (scHand != null)
                {
                    foreach (Meld meld in scHand.Melds)
                    {
                        if (meld.State != MeldState.None)
                        {
                            for (int iTile = 0; iTile < 3; ++iTile)
                            {
                                if (meld.Tiles[iTile].WinningTile && meld.Tiles[iTile].Tile.IsEqual(TileType.Circles1))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                break;
                            }
                        }
                    }

                    found |= (scHand.PairTile.WinningTile && scHand.PairTile.Tile.IsEqual(TileType.Circles1));
                }
                else if (spHand != null)
                {
                    foreach (ExtendedTile pairTile in spHand.PairTiles)
                    {
                        if (pairTile.WinningTile && pairTile.Tile.IsEqual(TileType.Circles1))
                        {
                            found = true;
                            break;
                        }
                    }
                }
                return found ? Yaku.IipinRaoyui.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_RyansouChankan(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (hand.Parent.ChankanFlag && hand.Parent.NextActionTile.IsEqual(TileType.Bamboo2)) ? Yaku.RyansouChankan.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_KachouFuugetsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            foreach (Meld meld in hand.OpenMeld)
            {
                if ((meld.State == MeldState.Chii) ||
                    ((meld.State != MeldState.None) &&
                     !meld.Tiles[0].Tile.IsEqual(TileType.Circles5) && !meld.Tiles[0].Tile.IsEqual(TileType.Bamboo1) &&
                     !meld.Tiles[0].Tile.IsEqual(TileType.East)     && !meld.Tiles[0].Tile.IsEqual(TileType.Circles1)))
                {
                    return 0;
                }
            }

            for (int iClosedMeld = 0; iClosedMeld < (4 - hand.GetCalledMeldCount()); ++iClosedMeld)
            {
                if ((scHand.Melds[iClosedMeld].State == MeldState.Chii) ||
                    (!scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Circles5) && !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Bamboo1) &&
                     !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.East)     && !scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.Circles1)))
                {
                    return 0;
                }
            }
            return Yaku.KachouFuugetsu.GetHan(hand.IsClosed(), hand.Parent.Settings);
        }

        public static int Evaluate_KinkeiDokuritsu(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            return ((hand.GetOpenMeldCount() == 4) && scHand.PairTile.Tile.IsEqual(TileType.Bamboo1)) ? Yaku.KinkeiDokuritsu.GetHan(false, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_OtakazeSankou(Hand hand, CandidateHand cHand, bool fRon)
        {
            TileType seatWindTile = TileTypeExtensionMethods.GetSeatWindTile(hand.Player, hand.Parent.CurrentDealer);
            if (seatWindTile.IsEqual(TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.CurrentRound)))
            {
                bool fEast = false;
                bool fSouth = false;
                bool fWest = false;
                bool fNorth = false;

                foreach (Meld meld in hand.OpenMeld)
                {
                    if (meld.State != MeldState.None)
                    {
                        if      (meld.Tiles[0].Tile.IsEqual(TileType.East))  { fEast = true; }
                        else if (meld.Tiles[0].Tile.IsEqual(TileType.South)) { fSouth = true; }
                        else if (meld.Tiles[0].Tile.IsEqual(TileType.West))  { fWest = true; }
                        else if (meld.Tiles[0].Tile.IsEqual(TileType.North)) { fNorth = true; }
                    }
                }

                StandardCandidateHand scHand = cHand as StandardCandidateHand;
                for (int iClosedMeld = 0; iClosedMeld < (4 - hand.GetCalledMeldCount()); ++iClosedMeld)
                {
                    if      (scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.East))  { fEast = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.South)) { fSouth = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.West))  { fWest = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Tile.IsEqual(TileType.North)) { fNorth = true; }
                }

                if ((seatWindTile.IsEqual(TileType.East)  && !fEast && fSouth  && fWest  && fNorth) ||
                    (seatWindTile.IsEqual(TileType.South) && fEast  && !fSouth && fWest  && fNorth) ||
                    (seatWindTile.IsEqual(TileType.West)  && fEast  && fSouth  && !fWest && fNorth) ||
                    (seatWindTile.IsEqual(TileType.North) && fEast  && fSouth  && fWest  && !fNorth))
                {
                    return Yaku.OtakazeSankou.GetHan(hand.IsClosed(), hand.Parent.Settings);
                }
            }
            return 0;
        }

        public static int Evaluate_Uumensai(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (hand.IsClosed())
            {
                bool fBamboo = false;
                bool fCircles = false;
                bool fCharacters = false;
                bool fDragon = false;
                bool fWind = false;

                StandardCandidateHand scHand = cHand as StandardCandidateHand;
                foreach (TileType tt in new TileType[] { scHand.Melds[0].Tiles[0].Tile,
                                                         scHand.Melds[1].Tiles[0].Tile,
                                                         scHand.Melds[2].Tiles[0].Tile,
                                                         scHand.Melds[3].Tiles[0].Tile,
                                                         scHand.PairTile.Tile })
                {
                    Suit suit = tt.GetSuit();
                    if      (suit == Suit.Bamboo)     { fBamboo = true; }
                    else if (suit == Suit.Circles)    { fCircles = true; }
                    else if (suit == Suit.Characters) { fBamboo = true; }
                    else if (tt.IsDragon())           { fDragon = true; }
                    else if (tt.IsWind())             { fWind = true; }
                }
                return (fBamboo && fCircles && fCharacters && fDragon && fWind) ? Yaku.Uumensai.GetHan(true, hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_Kanburi(Hand hand, CandidateHand cHand, bool fRon)
        {
            return hand.Parent.KanburiFlag ? Yaku.Kanburi.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_TsubameGaeshi(Hand hand, CandidateHand cHand, bool fRon)
        {
            if (fRon)
            {
                Player playerTarget = hand.Parent.NextActionPlayer;
                if (playerTarget.IsPlayer())
                {
                    List<ExtendedTile> discards = hand.Discards;
                    if (discards.Count > 0 && (discards[discards.Count - 1].Reach || discards[discards.Count - 1].OpenReach))
                    {
                        return Yaku.TsubameGaeshi.GetHan(hand.IsClosed(), hand.Parent.Settings);
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_PaaRenchan(Hand hand, CandidateHand cHand, bool fRon)
        {
            return (hand.Streak >= 8) ? Yaku.PaaRenchan.GetHan(hand.IsClosed(), hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Shousharin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return ((spHand.PairTiles[0].Tile.IsEqual(TileType.Circles1) &&
                     spHand.PairTiles[1].Tile.IsEqual(TileType.Circles2) &&
                     spHand.PairTiles[2].Tile.IsEqual(TileType.Circles3) &&
                     spHand.PairTiles[3].Tile.IsEqual(TileType.Circles4) &&
                     spHand.PairTiles[4].Tile.IsEqual(TileType.Circles5) &&
                     spHand.PairTiles[5].Tile.IsEqual(TileType.Circles6) &&
                     spHand.PairTiles[6].Tile.IsEqual(TileType.Circles7))
                    ||
                    (spHand.PairTiles[0].Tile.IsEqual(TileType.Circles3) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles4) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles5) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles6) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles7) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles8) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Circles9))) ? Yaku.Shousharin.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Shouchikurin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return ((spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo1) &&
                     spHand.PairTiles[1].Tile.IsEqual(TileType.Bamboo2) &&
                     spHand.PairTiles[2].Tile.IsEqual(TileType.Bamboo3) &&
                     spHand.PairTiles[3].Tile.IsEqual(TileType.Bamboo4) &&
                     spHand.PairTiles[4].Tile.IsEqual(TileType.Bamboo5) &&
                     spHand.PairTiles[5].Tile.IsEqual(TileType.Bamboo6) &&
                     spHand.PairTiles[6].Tile.IsEqual(TileType.Bamboo7))
                    ||
                    (spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo3) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo4) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo5) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo6) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo7) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo8) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Bamboo9))) ? Yaku.Shouchikurin.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Shousuurin(Hand hand, CandidateHand cHand, bool fRon)
        {
            SevenPairsCandidateHand spHand = cHand as SevenPairsCandidateHand;
            return ((spHand.PairTiles[0].Tile.IsEqual(TileType.Characters1) &&
                     spHand.PairTiles[1].Tile.IsEqual(TileType.Characters2) &&
                     spHand.PairTiles[2].Tile.IsEqual(TileType.Characters3) &&
                     spHand.PairTiles[3].Tile.IsEqual(TileType.Characters4) &&
                     spHand.PairTiles[4].Tile.IsEqual(TileType.Characters5) &&
                     spHand.PairTiles[5].Tile.IsEqual(TileType.Characters6) &&
                     spHand.PairTiles[6].Tile.IsEqual(TileType.Characters7))
                    ||
                    (spHand.PairTiles[0].Tile.IsEqual(TileType.Characters3) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters4) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters5) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters6) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters7) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters8) &&
                     spHand.PairTiles[0].Tile.IsEqual(TileType.Characters9))) ? Yaku.Shousuurin.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_IisouSuushun(Hand hand, CandidateHand cHand, bool fRon)
        {
            StandardCandidateHand scHand = cHand as StandardCandidateHand;
            int calledMeldCount = hand.GetCalledMeldCount();
            if (calledMeldCount > 0) // Make sure we have at least one call otherwise you might also get Suuankou.
            {
                // All melds should be chiis and they should all start with the same tile.
                TileType startTile = hand.OpenMeld[0].Tiles[0].Tile;

                // Check all the open melds.
                foreach (Meld meld in hand.OpenMeld)
                {
                    if (meld.State != MeldState.None)
                    {
                        TileType t1 = meld.Tiles[0].Tile;
                        TileType t2 = meld.Tiles[1].Tile;
                        TileType t3 = meld.Tiles[2].Tile;
                        TileType smallestTile = (t1.GetValue() < t2.GetValue()) ? ((t1.GetValue() < t3.GetValue()) ? t1 : t3) : ((t2.GetValue() < t3.GetValue()) ? t2 : t3);

                        if (!startTile.IsEqual(smallestTile) || (meld.State != MeldState.Chii))
                        {
                            return 0;
                        }
                    }
                }

                // Check all the closed melds.
                for (int i = 0; i < (4 - calledMeldCount); ++i)
                {
                    if (!startTile.IsEqual(scHand.Melds[i].Tiles[0].Tile) || (scHand.Melds[i].State != MeldState.Chii))
                    {
                        return 0;
                    }
                }

                // Done!
                return Yaku.IisouSuushun.GetHan(hand.IsClosed(), hand.Parent.Settings);
            }
            return 0;
        }
    }
}
