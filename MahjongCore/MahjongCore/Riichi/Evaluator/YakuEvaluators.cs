// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Helpers;
using MahjongCore.Riichi.Impl;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MahjongCore.Riichi.Evaluator
{
    public class YakuEvaluator
    {
        private static Dictionary<Yaku, Func<IHand, ICandidateHand, bool, int>> Evaluators = new Dictionary<Yaku, Func<IHand, ICandidateHand, bool, int>>();

        public static int Evaluate(Yaku yaku, IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (Evaluators.Count == 0)
            {
                Initialize();
            }

            var evaluator = Evaluators[yaku];
            Global.Assert(evaluator != null);
            int han = 0;
            if (evaluator != null)
            {
                han = evaluator.Invoke(hand, candidateHand, ron);
            }
            return han;
        }

        private static void Initialize()
        {
            foreach (Yaku y in Enum.GetValues(typeof(Yaku)))
            {
                // http://stackoverflow.com/questions/2933221/can-you-get-a-funct-or-similar-from-a-methodinfo-object
                MethodInfo info = typeof(YakuEvaluator).GetMethod("Evaluate_" + y.ToString());
                var func = Delegate.CreateDelegate(typeof(Func<IHand, ICandidateHand, bool, int>), info);
                Evaluators.Add(y, (Func<IHand, ICandidateHand, bool, int>)func);
            }
        }

        private readonly static TileType[] DaisharinTiles     = new TileType[] { TileType.Circles2,    TileType.Circles3,    TileType.Circles4,    TileType.Circles5,    TileType.Circles6,    TileType.Circles7,    TileType.Circles8, };
        private readonly static TileType[] DaisuurinTiles     = new TileType[] { TileType.Characters2, TileType.Characters3, TileType.Characters4, TileType.Characters5, TileType.Characters6, TileType.Characters7, TileType.Characters8, };
        private readonly static TileType[] DaichikurinTiles   = new TileType[] { TileType.Bamboo2,     TileType.Bamboo3,     TileType.Bamboo4,     TileType.Bamboo5,     TileType.Bamboo6,     TileType.Bamboo7,     TileType.Bamboo8, };
        private readonly static TileType[] Shousharin1Tiles   = new TileType[] { TileType.Circles1,    TileType.Circles2,    TileType.Circles3,    TileType.Circles4,    TileType.Circles5,    TileType.Circles6,   TileType.Circles7, };
        private readonly static TileType[] Shousharin2Tiles   = new TileType[] { TileType.Circles3,    TileType.Circles4,    TileType.Circles5,    TileType.Circles6,    TileType.Circles7,    TileType.Circles8,   TileType.Circles9, };
        private readonly static TileType[] Shousuurin1Tiles   = new TileType[] { TileType.Characters1, TileType.Characters2, TileType.Characters3, TileType.Characters4, TileType.Characters5, TileType.Characters6, TileType.Characters7, };
        private readonly static TileType[] Shousuurin2Tiles   = new TileType[] { TileType.Characters3, TileType.Characters4, TileType.Characters5, TileType.Characters6, TileType.Characters7, TileType.Characters8, TileType.Characters9, };
        private readonly static TileType[] Shouchikurin1Tiles = new TileType[] { TileType.Bamboo1,     TileType.Bamboo2,     TileType.Bamboo3,     TileType.Bamboo4,     TileType.Bamboo5,     TileType.Bamboo6,     TileType.Bamboo7, };
        private readonly static TileType[] Shouchikurin2Tiles = new TileType[] { TileType.Bamboo3,     TileType.Bamboo4,     TileType.Bamboo5,     TileType.Bamboo6,     TileType.Bamboo7,     TileType.Bamboo8,     TileType.Bamboo9, };

        public static int Evaluate_Riichi(        IHand hand, ICandidateHand candidateHand, bool ron) { return ((hand.Reach == ReachType.Reach) && !(hand as HandImpl).IsInDoubleReach()) ? Yaku.Riichi.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_DoubleRiichi(  IHand hand, ICandidateHand candidateHand, bool ron) { return (hand as HandImpl).IsInDoubleReach() ? Yaku.DoubleRiichi.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Chiitoitsu(    IHand hand, ICandidateHand candidateHand, bool ron) { return (candidateHand is SevenPairsCandidateHand) ? Yaku.Chiitoitsu.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Chun(          IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_DragonYakuhai(hand, (candidateHand as StandardCandidateHand), true, false, false, 1, GameOption.Chun); }
        public static int Evaluate_Haku(          IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_DragonYakuhai(hand, (candidateHand as StandardCandidateHand), false, true, false, 1, GameOption.Haku); }
        public static int Evaluate_Hatsu(         IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_DragonYakuhai(hand, (candidateHand as StandardCandidateHand), false, false, true, 1, GameOption.Hatsu); }
        public static int Evaluate_Shousangen(    IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_DragonYakuhai(hand, (candidateHand as StandardCandidateHand), true, true, true, 4, GameOption.Shousangen); }
        public static int Evaluate_Daisangen(     IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_DragonYakuhai(hand, (candidateHand as StandardCandidateHand), true, true, true, -1, GameOption.Daisangen); }
        public static int Evaluate_Ton(           IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.East, false, GameOption.Ton, Yaku.Ton); }
        public static int Evaluate_DoubleTon(     IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.East, true, GameOption.DoubleTon, Yaku.DoubleTon); }
        public static int Evaluate_Nan(           IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.South, false, GameOption.Nan, Yaku.Nan); }
        public static int Evaluate_DoubleNan(     IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.South, true, GameOption.DoubleNan, Yaku.DoubleNan); }
        public static int Evaluate_Sha(           IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.West, false, GameOption.Sha, Yaku.Sha); }
        public static int Evaluate_DoubleSha(     IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.West, true, GameOption.DoubleSha, Yaku.DoubleSha); }
        public static int Evaluate_Pei(           IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.North, false, GameOption.Pei, Yaku.Pei); }
        public static int Evaluate_DoublePei(     IHand hand, ICandidateHand candidateHand, bool ron) { return EvaluateWindYakuhai(hand, (candidateHand as StandardCandidateHand), TileType.North, true, GameOption.DoublePei, Yaku.DoublePei); }
        public static int Evaluate_MenzenTsumo(   IHand hand, ICandidateHand candidateHand, bool ron) { return (!ron && hand.Closed) ? Yaku.MenzenTsumo.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Ippatsu(       IHand hand, ICandidateHand candidateHand, bool ron) { return hand.CouldIppatsu ? Yaku.Ippatsu.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_HaiteiRaoyue(  IHand hand, ICandidateHand candidateHand, bool ron) { return ((hand.Parent.TilesRemaining == 0) && !ron) ? Yaku.HaiteiRaoyue.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_HouteiRaoyui(  IHand hand, ICandidateHand candidateHand, bool ron) { return ((hand.Parent.TilesRemaining == 0) && ron) ? Yaku.HouteiRaoyui.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_RinshanKaihou( IHand hand, ICandidateHand candidateHand, bool ron) { return ((GameStateImpl)hand.Parent).PlayerDeadWallPick ? Yaku.RinshanKaihou.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_Chankan(       IHand hand, ICandidateHand candidateHand, bool ron) { return (((GameStateImpl)hand.Parent).ChankanFlag) ? Yaku.Chankan.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_Shousuushii(   IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_Suushii(hand, candidateHand as StandardCandidateHand, true, GameOption.Shousuushii, Yaku.Shousuushii); }
        public static int Evaluate_Daisuushii(    IHand hand, ICandidateHand candidateHand, bool ron) { return Evaluate_Suushii(hand, candidateHand as StandardCandidateHand, true, GameOption.Daisuushii, Yaku.Daisuushii); }
        public static int Evaluate_Chiihou(       IHand hand, ICandidateHand candidateHand, bool ron) { return (!hand.Dealer && (hand.Discards.Count == 0)) ? Yaku.Chiihou.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Tenhou(        IHand hand, ICandidateHand candidateHand, bool ron) { return (hand.Dealer && (hand.Discards.Count == 0)) ? Yaku.Tenhou.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_RyansouChankan(IHand hand, ICandidateHand candidateHand, bool ron) { return (((GameStateImpl)hand.Parent).ChankanFlag && ((GameStateImpl)hand.Parent).NextActionTile.IsEqual(TileType.Bamboo2)) ? Yaku.RyansouChankan.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_Kanburi(       IHand hand, ICandidateHand candidateHand, bool ron) { return ((GameStateImpl)hand.Parent).KanburiFlag ? Yaku.Kanburi.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_PaaRenchan(    IHand hand, ICandidateHand candidateHand, bool ron) { return (hand.Streak >= 8) ? Yaku.PaaRenchan.GetHan(hand.Closed, hand.Parent.Settings) : 0; }
        public static int Evaluate_Daisharin(     IHand hand, ICandidateHand candidateHand, bool ron) { return CheckSevenPairsHandForTiles(candidateHand, DaisharinTiles)   ? Yaku.Daisharin.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Daisuurin(     IHand hand, ICandidateHand candidateHand, bool ron) { return CheckSevenPairsHandForTiles(candidateHand, DaisuurinTiles)   ? Yaku.Daisuurin.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Daichikurin(   IHand hand, ICandidateHand candidateHand, bool ron) { return CheckSevenPairsHandForTiles(candidateHand, DaichikurinTiles) ? Yaku.Daichikurin.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Shousharin(    IHand hand, ICandidateHand candidateHand, bool ron) { return (CheckSevenPairsHandForTiles(candidateHand, Shousharin1Tiles)   || CheckSevenPairsHandForTiles(candidateHand, Shousharin2Tiles))   ? Yaku.Shousharin.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Shouchikurin(  IHand hand, ICandidateHand candidateHand, bool ron) { return (CheckSevenPairsHandForTiles(candidateHand, Shouchikurin1Tiles) || CheckSevenPairsHandForTiles(candidateHand, Shouchikurin2Tiles)) ? Yaku.Shouchikurin.GetHan(true, hand.Parent.Settings) : 0; }
        public static int Evaluate_Shousuurin(    IHand hand, ICandidateHand candidateHand, bool ron) { return (CheckSevenPairsHandForTiles(candidateHand, Shousuurin1Tiles)   || CheckSevenPairsHandForTiles(candidateHand, Shousuurin2Tiles))   ? Yaku.Shousuurin.GetHan(true, hand.Parent.Settings) : 0; }

        private static void IterateAllMelds(IHand hand, StandardCandidateHand candidateHand, Action<IMeld> callback) { HandHelpers.IterateMelds(hand, callback); candidateHand.IterateMelds(callback); }

        public static int Evaluate_OpenRiichi(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Don't look at the first discard - that's for double riichi to do.
            int han = (hand.Reach == ReachType.OpenReach) ? Yaku.OpenRiichi.GetHan(true, hand.Parent.Settings) : 0;

            // If we're in open reach... if we're also in double reach, give the normal reach value to the double
            // reach. That's complicated but we'll just minus one. That'll be fine for pretty much every scenario.
            // We could just return the value of Yaku.Riichi.GetHan, but that might mess things up.... hrm.
            return ((han > 1) && (hand as HandImpl).IsInDoubleReach()) ? han - 1 : han;
        }

        public static int Evaluate_Pinfu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (hand.Open || (!hand.Parent.Settings.GetSetting<bool>(GameOption.PinfuTsumo) && !ron))
            {
                return 0;
            }

            // Make sure each of the melds are runs.
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
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
                if ((scHand.Melds[i].Tiles[0].WinningTile && (scHand.Melds[i].Tiles[0].Type.GetValue() == 7)) ||
                     scHand.Melds[i].Tiles[1].WinningTile ||
                    (scHand.Melds[i].Tiles[2].WinningTile && (scHand.Melds[i].Tiles[2].Type.GetValue() == 3)))
                {
                    return 0;
                }
            }

            // Make sure the pair is not valued.
            if (scHand.PairTile.Type.IsEqual(TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.Round)) ||
                scHand.PairTile.Type.IsEqual(TileTypeExtensionMethods.GetSeatWindTile(hand.Player, hand.Parent.Dealer)))
            {
                return 0;
            }

            // Success!
            return Yaku.Pinfu.GetHan(true, hand.Parent.Settings);
        }

        public static int Evaluate_Iipeikou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (hand.Open) { return 0; }

            // Find two melds with the same sequence of tiles. If we have two sequences of tiles that are identical, don't count this.
            // Basically if it's a chii and it starts with the same tile, it's the same.
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            TileType[] sequences = null;
            int sCount = 0;
            int calledMeldCount = hand.MeldCount;
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    if (sequences == null)
                    {
                        sequences = new TileType[4];
                    }
                    sequences[sCount++] = scHand.Melds[i].Tiles[0].Type;
                }
            }

            bool found = false;
            if (sCount == 2)
            {
                found = sequences[0].IsEqual(sequences[1]);
            }
            else if (sCount == 3)
            {
                found = hand.Parent.Settings.GetSetting<bool>(GameOption.IisouSanjun) ?
                    (sequences[0].IsEqual(sequences[1]) ^ sequences[1].IsEqual(sequences[2])) : // Ensure we have iipeikou and not iisou sanjun.
                    (sequences[0].IsEqual(sequences[1]) || sequences[1].IsEqual(sequences[2]));
            }
            else if (sCount == 4)
            {
                // Make sure we don't have Ryanpeikou. Note that if we have Ryanpeikou, sequences 0-1 and 2-3 should be the same.
                if (hand.Parent.Settings.GetSetting<bool>(GameOption.Ryanpeikou) && sequences[0].IsEqual(sequences[1]) && sequences[2].IsEqual(sequences[3]))
                {
                    return 0;
                }

                // Make sure we don't have Iisou Sanjun.
                if (hand.Parent.Settings.GetSetting<bool>(GameOption.IisouSanjun) &&
                    ((sequences[0].IsEqual(sequences[1]) && sequences[1].IsEqual(sequences[2])) || (sequences[1].IsEqual(sequences[2]) && sequences[2].IsEqual(sequences[3]))))
                {
                    return 0;
                }

                found = (sequences[0].IsEqual(sequences[1]) || sequences[1].IsEqual(sequences[2]) || sequences[2].IsEqual(sequences[3]));
            }
            return found ? Yaku.Iipeikou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_SanshokuDoujun(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Get a list of all the sequences.
            TileType[] sequences = new TileType[4];
            int sCount = 0;
            foreach (IMeld m in hand.Melds)
            {
                if (m.State == MeldState.Chii)
                {
                    sequences[sCount++] = m.GetLowestTile().Type;
                }
            }

            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    sequences[sCount++] = scHand.Melds[i].GetLowestTile().Type;
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

                    if ((suit0 != suit1) && (suit0 != suit2) && (suit1 != suit2))
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
            return fSuccess ? Yaku.SanshokuDoujun.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Ittsuu(IHand hand, ICandidateHand candidateHand, bool ron)
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

            foreach (IMeld meld in hand.Melds)
            {
                if (meld.State == MeldState.Chii)
                {
                    Suit suit = meld.Tiles[0].Type.GetSuit();
                    int value = meld.Tiles[0].Type.GetValue();
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

            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    Suit suit = scHand.Melds[i].Tiles[0].Type.GetSuit();
                    int value = scHand.Melds[i].Tiles[0].Type.GetValue();
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
                    (fCirc123 && fCirc456 && fCirc789)) ? Yaku.Ittsuu.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Ryanpeikou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // If we have any called melds then we 4 concealed runs for ryanpeikou.
            if (hand.MeldCount > 0) { return 0; }

            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < 4; ++i) { if (scHand.Melds[i].State != MeldState.Chii) { return 0; } }

            // Get the first tile of all the melds if they're sequences.
            return (scHand.Melds[0].Tiles[0].Type.IsEqual(scHand.Melds[1].Tiles[0].Type) &&
                    scHand.Melds[2].Tiles[0].Type.IsEqual(scHand.Melds[3].Tiles[0].Type)) ? Yaku.Ryanpeikou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Toitoi(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            IMeld[] openMeld = hand.Melds;
            if ((openMeld[0].State == MeldState.Chii) ||
                (openMeld[1].State == MeldState.Chii) ||
                (openMeld[2].State == MeldState.Chii) ||
                (openMeld[3].State == MeldState.Chii))
            {
                return 0;
            }

            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    return 0;
                }
            }
            return Yaku.Toitoi.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_Sanankou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Go through all the closed melds. Count how many are sets. If any of them have
            // a winning tile and fRon is true, then it doesn't count. If we get 3+, then we have sanankou.
            // Note that if we have make a meld with a Rinshan Kaihou with an open kan, it should still
            // count since it's still tsumo even though it's scored as a ron.
            int validSets = 0;
            int calledMeldCount = hand.MeldCount;
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Pon)
                {
                    if (!ron || (!scHand.Melds[i].Tiles[0].WinningTile &&
                                  !scHand.Melds[i].Tiles[1].WinningTile &&
                                  !scHand.Melds[i].Tiles[2].WinningTile))
                    {
                        validSets++;
                    }
                }
            }

            // Also go through all the open melds - if any are closed kans, then those count.
            foreach (IMeld meld in hand.Melds)
            {
                if (meld.State == MeldState.KanConcealed)
                {
                    validSets++;
                }
            }
            return (validSets >= 3) ? Yaku.Sanankou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_SanshokuDoukou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            int count = 0;
            TileType[] sets = null;
            IterateAllMelds(hand, (candidateHand as StandardCandidateHand), (IMeld meld) =>
            {
                if ((meld.State != MeldState.Chii) && !meld.Tiles[0].Type.IsHonor())
                {
                    if (sets == null) { sets = new TileType[4]; }
                    sets[count++] = meld.Tiles[0].Type;
                }
            });

            bool found = false;
            if (count == 3)
            {
                int value1 = sets[0].GetValue();
                int value2 = sets[1].GetValue();
                int value3 = sets[2].GetValue();
                Suit suit1 = sets[0].GetSuit();
                Suit suit2 = sets[1].GetSuit();
                Suit suit3 = sets[2].GetSuit();
                found = ((value1 == value2) && (value1 == value3) && (suit1 != suit2) && (suit1 != suit3) && (suit2 != suit3));
            }
            else if (count == 4)
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

                    found = ((values[0] == values[1]) &&
                             (values[0] == values[2]) &&
                             (suits[0] != suits[1]) &&
                             (suits[0] != suits[2]) &&
                             (suits[1] != suits[2]));
                }
            }
            return found ? Yaku.SanshokuDoukou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Sankantsu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            int quads = 0;
            foreach (IMeld meld in hand.Melds) { if (meld.State.GetMeldType() == MeldType.Kan) { quads++; } }
            return (quads == 3) ? Yaku.Sankantsu.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Tanyao(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (!hand.Parent.Settings.GetSetting<bool>(GameOption.Kuitan) && hand.Open) { return 0; }
            if (!HandHelpers.IterateMeldsAND(hand, (IMeld meld) => 
                {
                    return MeldHelpers.IterateTilesAND(meld, (TileType tile) => { return !tile.IsTerminalOrHonor(); });
                }))
            {
                return 0;
            }

            return HandHelpers.IterateTilesAND(hand, (TileType tile) => { return !tile.IsTerminalOrHonor(); }) ? Yaku.Tanyao.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        private static int Evaluate_DragonYakuhai(IHand hand, StandardCandidateHand scHand, bool findChun, bool findHaku, bool findHatsu, int score, GameOption setting)
        {
            // Tally up if the chun, haku, and hatsu are in use.
            bool chun = false;
            bool haku = false;
            bool hatsu = false;

            IterateAllMelds(hand, scHand, (IMeld meld) =>
            {
                if (meld.Tiles[0].Type == TileType.Chun)  { chun = true; }
                if (meld.Tiles[0].Type == TileType.Haku)  { haku = true; }
                if (meld.Tiles[0].Type == TileType.Hatsu) { hatsu = true; }
            });

            if (findChun  && scHand.PairTile.Type.IsEqual(TileType.Chun))  { chun = true; }
            if (findHaku  && scHand.PairTile.Type.IsEqual(TileType.Haku))  { haku = true; }
            if (findHatsu && scHand.PairTile.Type.IsEqual(TileType.Hatsu)) { hatsu = true; }

            // Unless we're looking for all three, don't return true specifically if we have all three.
            if ((!findChun || !findHaku || !findHatsu) && chun && haku && hatsu)
            {
                return 0;
            }
            return ((chun && findChun) ||
                    (haku && findHaku) ||
                    (hatsu && findHatsu)) ? 0 : score;
        }

        private static int EvaluateWindYakuhai(IHand hand, StandardCandidateHand scHand, TileType tile, bool doubleYakuhai, GameOption yakuhaiSetting, Yaku yaku)
        {
            bool found = false;
            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State != MeldState.None) && meld.Tiles[0].Type.IsEqual(tile))
                {
                    found = true;
                    break;
                }
            }

            for (int i = 0; !found && i < (4 - hand.MeldCount); ++i)
            {
                if (scHand.Melds[i].Tiles[0].Type.IsEqual(tile))
                {
                    found = true;
                }
            }

            if (found)
            {
                int nCount = 0;
                if (TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.Round).IsEqual(tile))
                {
                    ++nCount;
                }

                if (hand.Seat.GetTile().IsEqual(tile))
                {
                    ++nCount;
                }

                return (nCount == (doubleYakuhai ? 2 : 1)) ? yaku.GetHan(hand.Closed, hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_Chanta(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            if (!scHand.PairTile.Type.IsTerminalOrHonor()) { return 0; }

            foreach (IMeld meld in hand.Melds)
            {
                // Check calls for terminals. If it's a chii check either end for a 1 or 9. Otherwise it's a pon/kan, so check if it's an honor or terminal.
                if (meld.State == MeldState.Chii)
                {
                    if ((meld.Tiles[0].Type.GetValue() != 1) && (meld.Tiles[2].Type.GetValue() != 9))
                    {
                        return 0;
                    }
                }
                else if ((meld.State != MeldState.None) && !meld.Tiles[0].Type.IsTerminalOrHonor())
                {
                    return 0;
                }
            }

            for (int iMeld = 0; iMeld < (4 - hand.MeldCount); ++iMeld)
            {
                IMeld closedMeld = scHand.Melds[iMeld];
                if (((closedMeld.State == MeldState.Chii) && (closedMeld.Tiles[0].Type.GetValue() != 1) && (closedMeld.Tiles[2].Type.GetValue() != 9)) ||
                    ((closedMeld.State != MeldState.None) && !closedMeld.Tiles[0].Type.IsTerminalOrHonor()))
                {
                    return 0;
                }
            }

            return Yaku.Chanta.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_Honitsu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Look through all the tiles and the melds.
            bool fCircs = false;
            bool fChars = false;
            bool fBambs = false;
            bool fHonors = false;

            foreach (IMeld meld in hand.Melds)
            {
                if (meld.State != MeldState.None)
                {
                    Suit suit = meld.Tiles[0].Type.GetSuit();
                    if      (suit == Suit.Bamboo)     { fBambs = true; }
                    else if (suit == Suit.Characters) { fChars = true; }
                    else if (suit == Suit.Circles)    { fCircs = true; }
                    else                              { fHonors = true; }
                }
            }

            
            foreach (ITile tile in hand.Tiles)
            {
                if (tile.Type != TileType.None)
                {
                    Suit suit = tile.Type.GetSuit();
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
            return (fHonors && (suits == 1)) ? Yaku.Honitsu.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Junchan(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            if (!scHand.PairTile.Type.IsTerminal())
            {
                return 0;
            }

            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if (!(scHand.Melds[i].Tiles[0].Type.IsTerminal() || scHand.Melds[i].Tiles[1].Type.IsTerminal() || scHand.Melds[i].Tiles[2].Type.IsTerminal()))
                {
                    return 0;
                }
            }

            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State != MeldState.None) && !(meld.Tiles[0].Type.IsTerminal() || meld.Tiles[1].Type.IsTerminal() || meld.Tiles[2].Type.IsTerminal()))
                {
                    return 0;
                }
            }
            return Yaku.Junchan.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_Honroutou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State == MeldState.Chii) || ((meld.State != MeldState.None) && !meld.Tiles[0].Type.IsTerminalOrHonor()))
                {
                    return 0;
                }
            }

            foreach (ITile tile in hand.Tiles)
            {
                if (tile.Type.IsTile() && !tile.Type.IsTerminalOrHonor())
                {
                    return 0;
                }
            }
            return Yaku.Honroutou.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_Chinitsu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            Suit suit = hand.Tiles[0].Type.GetSuit();
            foreach (IMeld meld in hand.Melds)      { if (meld.State.IsCalled() && (meld.Tiles[0].Type.GetSuit() != suit)) { return 0; } }
            foreach (ITile tile in hand.Tiles)      { if (tile.Type.IsTile() && (tile.Type.GetSuit() != suit))             { return 0; } }
            return Yaku.Chinitsu.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_NagashiMangan(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if ((hand.Parent.TilesRemaining != 0) || (hand.MeldCount > 0))
            {
                return 0;
            }

            foreach (ITile et in hand.Discards)
            {
                if (et.Called || !et.Type.IsTerminalOrHonor())
                {
                    return 0;
                }
            }
            return Yaku.NagashiMangan.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_KokushiMusou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            int han = Yaku.KokushiMusou.GetHan(true, hand.Parent.Settings);
            if ((han == 0) || (hand.MeldCount > 0)) { return 0; }

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

            if (!HandHelpers.IterateTilesAND(hand, (TileType tile) =>
            {
                if      (!tile.IsTerminalOrHonor())    { return false; }
                else if (tile == TileType.Bamboo1)     { nBamb1++; }
                else if (tile == TileType.Bamboo9)     { nBamb9++; }
                else if (tile == TileType.Characters1) { nChar1++; }
                else if (tile == TileType.Characters9) { nChar9++; }
                else if (tile == TileType.Circles1)    { nCirc1++; }
                else if (tile == TileType.Circles9)    { nCirc9++; }
                else if (tile == TileType.Chun)        { nChun++; }
                else if (tile == TileType.Hatsu)       { nHatsu++; }
                else if (tile == TileType.Haku)        { nHaku++; }
                else if (tile == TileType.East)        { nEast++; }
                else if (tile == TileType.West)        { nWest++; }
                else if (tile == TileType.North)       { nNorth++; }
                else if (tile == TileType.South)       { nSouth++; }
                return true;
            })) { return 0; }

            if ((nBamb1 == 0) || (nBamb9 == 0) || (nCirc1 == 0) || (nCirc9 == 0) || (nChar1 == 0) || (nChar9 == 0) || (nChun == 0) ||
                (nHatsu == 0) || (nHaku == 0) || (nEast == 0) || (nWest == 0) || (nNorth == 0) || (nSouth == 0))
            {
                return 0;
            }

            // Check to see if we have a 13 way wait. If the last tile in the active hand is the same as any other tile, then double yakuman!
            if (hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman))
            {
                TileType winTile = hand.Tiles[13].Type;
                for (int i = 0; i < 13; ++i)
                {
                    if (winTile.IsEqual(hand.Tiles[i].Type))
                    {
                        --han;
                        break;
                    }
                }
            }
            return han;
        }

        public static int Evaluate_ChuurenPoutou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            Suit suit = hand.Tiles[0].Type.GetSuit();
            int han = Yaku.ChuurenPoutou.GetHan(true, hand.Parent.Settings);
            if ((han == 0) || (hand.MeldCount > 0) || !HandHelpers.IterateTilesAND(hand, (TileType tile) => { return (tile.GetSuit() == suit); })) { return 0; }

            // Make sure we have the correct amount of each value.
            int[] values = new int[10]; // values[0] isn't used.
            foreach (ITile tile in hand.Tiles)
            {
                values[tile.Type.GetValue()]++;
            }

            if ((values[1] < 3) || (values[2] < 1) || (values[3] < 1) ||
                (values[4] < 1) || (values[5] < 1) || (values[6] < 1) ||
                (values[7] < 1) || (values[8] < 1) || (values[9] < 3))
            {
                return 0;
            }

            // Success! If the winning tile is the one that makes it go over 1 or 3, then we get a double yakuman.
            int winValueCount = values[hand.Tiles[13].Type.GetValue()];
            return (hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && ((winValueCount == 2) || (winValueCount == 4))) ? (han - 1) : han;
        }

        public static int Evaluate_Suuankou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (hand.Open) { return 0; }

            // Any calls will be closed kans, so we only need to look at closed melds.
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            for (int i = 0; i < (4 - hand.MeldCount); ++i) { if (scHand.Melds[i].State != MeldState.Pon) { return 0; } }

            // If the wait is on the pair, then it's double yakuman.
            int han = Yaku.Suuankou.GetHan(true, hand.Parent.Settings);
            if ((han != 0) && !hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) && scHand.PairTile.WinningTile)
            {
                --han;
            }
            return han;
        }

        private static int Evaluate_Suushii(IHand hand, StandardCandidateHand candidateHand, bool checkPair, GameOption setting, Yaku yaku)
        {
            int windCount = 0;
            if (checkPair && candidateHand.PairTile.Type.IsWind())                            { windCount++; }
            HandHelpers.IterateMelds(hand, (IMeld meld) => { if (meld.Tiles[0].Type.IsWind()) { windCount++; } });
            candidateHand.IterateMelds(    (IMeld meld) => { if (meld.Tiles[0].Type.IsWind()) { windCount++; } });
            return (windCount == 4) ? yaku.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        private static int Evaluate_Suukantsu(IHand hand, CandidateHand candidateHand, bool ron)
        {
            IMeld[] openMeld = hand.Melds;
            if ((openMeld[0].State.GetMeldType() == MeldType.Kan) && (openMeld[1].State.GetMeldType() == MeldType.Kan) &&
                (openMeld[2].State.GetMeldType() == MeldType.Kan) && (openMeld[3].State.GetMeldType() == MeldType.Kan))
            {
                int han = Yaku.Suukantsu.GetHan(hand.Closed, hand.Parent.Settings);
                if ((hand.Parent.Settings.GetSetting<bool>(GameOption.FourQuadRinshan) &&
                     hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman) &&
                     ((GameStateImpl)hand.Parent).PlayerDeadWallPick) ||
                    (hand.Parent.Settings.GetSetting<bool>(GameOption.SuukantsuDoubleYakuman) &&
                     hand.Parent.Settings.GetSetting<bool>(GameOption.DoubleYakuman)))
                {
                    --han;
                }
                return han;
            }
            return 0;
        }

        private static bool CheckRyuuiisouMeld(IMeld meld)
        {
            if (meld.State == MeldState.Chii)
            {
                Suit suit = meld.Tiles[0].Type.GetSuit();
                int value = Math.Min(meld.Tiles[0].Type.GetValue(), meld.Tiles[1].Type.GetValue());
                if ((suit != Suit.Bamboo) || (value != 2))
                {
                    return false;
                }
            }
            else if (meld.State != MeldState.None)
            {
                if (!meld.Tiles[0].Type.IsEqual(TileType.Bamboo2) && !meld.Tiles[0].Type.IsEqual(TileType.Bamboo3) &&
                    !meld.Tiles[0].Type.IsEqual(TileType.Bamboo4) && !meld.Tiles[0].Type.IsEqual(TileType.Bamboo6) &&
                    !meld.Tiles[0].Type.IsEqual(TileType.Bamboo8) && !meld.Tiles[0].Type.IsEqual(TileType.Hatsu))
                {
                    return false;
                }
            }
            return true;
        }

        public static int Evaluate_Ryuuiisou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            if (!HandHelpers.IterateMeldsAND(hand, (IMeld meld) => { return CheckRyuuiisouMeld(meld); }) ||
                !scHand.IterateMeldsAND((IMeld meld) =>            { return CheckRyuuiisouMeld(meld); }))
            {
                return 0;
            }

            return (scHand.PairTile.Type.IsEqual(TileType.Bamboo2) || scHand.PairTile.Type.IsEqual(TileType.Bamboo3) ||
                    scHand.PairTile.Type.IsEqual(TileType.Bamboo4) || scHand.PairTile.Type.IsEqual(TileType.Bamboo6) ||
                    scHand.PairTile.Type.IsEqual(TileType.Bamboo8) || scHand.PairTile.Type.IsEqual(TileType.Hatsu)) ? Yaku.Ryuuiisou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Chinroutou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if   (!HandHelpers.IterateMeldsAND(hand, (IMeld meld)    => { return (meld.State != MeldState.Chii) && meld.Tiles[0].Type.IsTerminal(); })) { return 0; }
            return HandHelpers.IterateTilesAND(hand, (TileType tile) => { return tile.IsTerminal(); }) ? Yaku.Chinroutou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Tsuuiisou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if   (!HandHelpers.IterateMeldsAND(hand, (IMeld meld)    => { return (meld.State != MeldState.Chii) && meld.Tiles[0].Type.IsHonor(); })) { return 0; }
            return HandHelpers.IterateTilesAND(hand, (TileType tile) => { return tile.IsHonor(); }) ? Yaku.Tsuuiisou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_ShiisanBudou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Ensure that this is the first go around and noone has called.
            IList<ITile> discards = hand.Discards;
            if ((hand.Parent.Player1Hand.MeldCount > 0) ||
                (hand.Parent.Player2Hand.MeldCount > 0) ||
                (hand.Parent.Player3Hand.MeldCount > 0) ||
                (hand.Parent.Player4Hand.MeldCount > 0) ||
                (discards.Count > 0))
            {
                return 0;
            }

            // Get the sorted list.
            TileType[] sortedList = new TileType[14];
            for (int i = 0; i < 14; ++i)
            {
                sortedList[i] = hand.Tiles[i].Type;
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

        public static int Evaluate_Renhou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            return (!ron &&
                    (hand.Parent.Player1Hand.MeldCount == 0) &&
                    (hand.Parent.Player2Hand.MeldCount == 0) &&
                    (hand.Parent.Player3Hand.MeldCount == 0) &&
                    (hand.Parent.Player4Hand.MeldCount == 0) &&
                    (hand.Discards.Count == 0)) ? Yaku.Renhou.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Daichisei(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            SevenPairsCandidateHand spHand = candidateHand as SevenPairsCandidateHand;
            return spHand.IteratePairs((TileType tile) => { return tile.IsHonor(); }) ? Yaku.Daichisei.GetHan(true, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_Sanrenkou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            TileType[] triplets = new TileType[4];
            int tCount = 0;
            int calledMeldCount = hand.MeldCount;
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if ((scHand.Melds[i].State == MeldState.Pon) ||
                    (scHand.Melds[i].State == MeldState.KanConcealed) ||
                    (scHand.Melds[i].State == MeldState.KanOpen) ||
                    (scHand.Melds[i].State == MeldState.KanPromoted))
                {
                    triplets[tCount++] = scHand.Melds[i].Tiles[0].Type;
                }
            }

            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State == MeldState.Pon) ||
                    (meld.State == MeldState.KanConcealed) ||
                    (meld.State == MeldState.KanOpen) ||
                    (meld.State == MeldState.KanPromoted))
                {
                    triplets[tCount++] = meld.Tiles[0].Type;
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
                            return Yaku.Sanrenkou.GetHan(hand.Closed, hand.Parent.Settings);
                        }
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_IisouSanjun(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;

            // Find two melds with the same sequence of tiles. If we have two sequences of tiles that are identical, don't count this.
            // Basically if it's a chii and it starts with the same tile, it's the same.
            TileType[] sequences = new TileType[4];
            int sCount = 0;
            int calledMeldCount = hand.MeldCount;
            for (int i = 0; i < (4 - calledMeldCount); ++i)
            {
                if (scHand.Melds[i].State == MeldState.Chii)
                {
                    Suit suit = scHand.Melds[i].Tiles[0].Type.GetSuit();
                    int value = Math.Min(scHand.Melds[i].Tiles[0].Type.GetValue(),
                                Math.Min(scHand.Melds[i].Tiles[1].Type.GetValue(),
                                         scHand.Melds[i].Tiles[2].Type.GetValue()));
                    sequences[sCount++] = TileHelpers.BuildTile(suit, value);
                }
            }

            foreach (IMeld meld in hand.Melds)
            {
                if (meld.State == MeldState.Chii)
                {
                    Suit suit = meld.Tiles[0].Type.GetSuit();
                    int value = Math.Min(meld.Tiles[0].Type.GetValue(),
                                Math.Min(meld.Tiles[1].Type.GetValue(),
                                         meld.Tiles[2].Type.GetValue()));
                    sequences[sCount++] = TileHelpers.BuildTile(suit, value);
                }
            }

            if (sCount >= 3)
            {
                Array.Sort(sequences);
                if ((sequences[0].IsEqual(sequences[1]) && sequences[1].IsEqual(sequences[2]) && !sequences[2].IsEqual(sequences[3])) ||
                    (sequences[1].IsEqual(sequences[2]) && sequences[2].IsEqual(sequences[3]) && !sequences[0].IsEqual(sequences[1])))
                {
                    return Yaku.IisouSanjun.GetHan(hand.Closed, hand.Parent.Settings);
                }
            }

            return 0;
        }

        public static int Evaluate_Suurenkou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            TileType[] triplets = new TileType[4];
            int tCount = 0;
            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if ((scHand.Melds[i].State.GetMeldType() == MeldType.Pon) || (scHand.Melds[i].State.GetMeldType() == MeldType.Kan))
                {
                    triplets[tCount++] = scHand.Melds[i].Tiles[0].Type;
                }
            }

            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State.GetMeldType() == MeldType.Pon) || (meld.State.GetMeldType() == MeldType.Kan))
                {
                    triplets[tCount++] = meld.Tiles[0].Type;
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
                        return Yaku.Suurenkou.GetHan(hand.Closed, hand.Parent.Settings);
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_HyakumanGoku(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            int manzuTotal = 0;
            HandHelpers.IterateMelds(hand, (IMeld meld) =>
            {
                MeldHelpers.IterateTiles(meld, (TileType tile) =>
                {
                    if (tile.IsSuit(Suit.Characters)) { manzuTotal += tile.GetValue(); }
                });
            });

            HandHelpers.IterateTiles(hand, (TileType tile) => { if (tile.IsSuit(Suit.Characters)) { manzuTotal += tile.GetValue(); } });
            return (manzuTotal >= 100) ? Yaku.HyakumanGoku.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_BeniKujaku(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State == MeldState.Chii) ||
                    ((meld.State != MeldState.None) &&
                     (!meld.Tiles[0].Type.IsEqual(TileType.Bamboo1) &&
                      !meld.Tiles[0].Type.IsEqual(TileType.Bamboo5) &&
                      !meld.Tiles[0].Type.IsEqual(TileType.Bamboo7) &&
                      !meld.Tiles[0].Type.IsEqual(TileType.Bamboo9) &&
                      !meld.Tiles[0].Type.IsEqual(TileType.Chun))))
                {
                    return 0;
                }
            }

            for (int i = 0; i < (4 - hand.MeldCount); ++i)
            {
                if ((scHand.Melds[i].State == MeldState.Chii) ||
                    (!scHand.Melds[i].Tiles[0].Type.IsEqual(TileType.Bamboo1) && !scHand.Melds[i].Tiles[0].Type.IsEqual(TileType.Bamboo5) &&
                     !scHand.Melds[i].Tiles[0].Type.IsEqual(TileType.Bamboo7) && !scHand.Melds[i].Tiles[0].Type.IsEqual(TileType.Bamboo9) &&
                     !scHand.Melds[i].Tiles[0].Type.IsEqual(TileType.Chun)))
                {
                    return 0;
                }
            }

            if (!scHand.PairTile.Type.IsEqual(TileType.Bamboo1) && !scHand.PairTile.Type.IsEqual(TileType.Bamboo5) &&
                !scHand.PairTile.Type.IsEqual(TileType.Bamboo7) && !scHand.PairTile.Type.IsEqual(TileType.Bamboo9) &&
                !scHand.PairTile.Type.IsEqual(TileType.Chun))
            {
                return 0;
            }
            return Yaku.BeniKujaku.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_AoNoDoumon(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;

            bool success = false;
            if (candidateHand is SevenPairsCandidateHand spHand)
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (!spHand.PairTiles[i].Type.IsEqual(TileType.Circles2) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.Circles4) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.Circles8) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.North) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.East) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.South) &&
                        !spHand.PairTiles[i].Type.IsEqual(TileType.West))
                    {
                        return 0;
                    }
                }
                return -1;
            }
            else if (scHand != null)
            {
                foreach (IMeld meld in hand.Melds)
                {
                    if ((meld.State == MeldState.Chii) ||
                        ((meld.State != MeldState.None) &&
                            (!meld.Tiles[0].Type.IsEqual(TileType.Circles2) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.Circles4) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.Circles8) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.North) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.East) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.South) &&
                             !meld.Tiles[0].Type.IsEqual(TileType.West))))
                    {
                        return 0;
                    }
                }

                for (int iClosedMeld = 0; iClosedMeld < (4 - hand.MeldCount); ++iClosedMeld)
                {
                    if ((scHand.Melds[iClosedMeld].State == MeldState.Chii) ||
                        (!scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Circles2) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Circles4) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Circles8) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.North) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.East) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.South) &&
                         !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.West)))
                    {
                        return 0;
                    }
                }

                if (!scHand.PairTile.Type.IsEqual(TileType.Circles2) && !scHand.PairTile.Type.IsEqual(TileType.Circles4) &&
                    !scHand.PairTile.Type.IsEqual(TileType.Circles8) && !scHand.PairTile.Type.IsEqual(TileType.North) &&
                    !scHand.PairTile.Type.IsEqual(TileType.East) && !scHand.PairTile.Type.IsEqual(TileType.South) &&
                    !scHand.PairTile.Type.IsEqual(TileType.West))
                {
                    return 0;
                }
            }
            return success ? Yaku.AoNoDoumon.GetHan(hand.Closed, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_ShiisuuPuuta(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // Ensure that this is the first go around and noone has called.
            if ((hand.Parent.Player1Hand.MeldCount > 0) ||
                (hand.Parent.Player2Hand.MeldCount > 0) ||
                (hand.Parent.Player3Hand.MeldCount > 0) ||
                (hand.Parent.Player4Hand.MeldCount > 0) ||
                (hand.Discards.Count > 0))
            {
                return 0;
            }

            // Check all tiles. We should get no pairs and no two tiles should be adjacent by 1 or 2.
            TileType[] sortedList = HandHelpers.GetSortedTiles(hand);
            for (int i = 0; i < 13; ++i) /// TODO: possible bug if small sortedList leaks through
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

        public static int Evaluate_UupinKaihou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            if (((GameStateImpl)hand.Parent).PlayerDeadWallPick)
            {
                bool found = false;
                foreach (IMeld cMeld in scHand.Melds)
                {
                    if (cMeld.State != MeldState.None)
                    {
                        for (int iTile = 0; iTile < 3; ++iTile)
                        {
                            if (cMeld.Tiles[iTile].WinningTile && cMeld.Tiles[iTile].Type.IsEqual(TileType.Circles5))
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

                if (scHand.PairTile.WinningTile && scHand.PairTile.Type.IsEqual(TileType.Circles5))
                {
                    found = true;
                }
                return found ? Yaku.UupinKaihou.GetHan(hand.Closed, hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_IipinRaoyui(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            // standards
            if (hand.Parent.TilesRemaining == 0)
            {
                var spHand = candidateHand as SevenPairsCandidateHand;
                bool found = false;
                if (candidateHand is StandardCandidateHand scHand)
                {
                    foreach (IMeld meld in scHand.Melds)
                    {
                        if (meld.State != MeldState.None)
                        {
                            for (int iTile = 0; iTile < 3; ++iTile)
                            {
                                if (meld.Tiles[iTile].WinningTile && meld.Tiles[iTile].Type.IsEqual(TileType.Circles1))
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

                    found |= (scHand.PairTile.WinningTile && scHand.PairTile.Type.IsEqual(TileType.Circles1));
                }
                else if (spHand != null)
                {
                    foreach (ITile pairTile in spHand.PairTiles)
                    {
                        if (pairTile.WinningTile && pairTile.Type.IsEqual(TileType.Circles1))
                        {
                            found = true;
                            break;
                        }
                    }
                }
                return found ? Yaku.IipinRaoyui.GetHan(hand.Closed, hand.Parent.Settings) : 0;
            }
            return 0;
        }

        public static int Evaluate_KachouFuugetsu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            foreach (IMeld meld in hand.Melds)
            {
                if ((meld.State == MeldState.Chii) ||
                    ((meld.State != MeldState.None) &&
                     !meld.Tiles[0].Type.IsEqual(TileType.Circles5) && !meld.Tiles[0].Type.IsEqual(TileType.Bamboo1) &&
                     !meld.Tiles[0].Type.IsEqual(TileType.East)     && !meld.Tiles[0].Type.IsEqual(TileType.Circles1)))
                {
                    return 0;
                }
            }

            for (int iClosedMeld = 0; iClosedMeld < (4 - hand.MeldCount); ++iClosedMeld)
            {
                if ((scHand.Melds[iClosedMeld].State == MeldState.Chii) ||
                    (!scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Circles5) && !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Bamboo1) &&
                     !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.East)     && !scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.Circles1)))
                {
                    return 0;
                }
            }
            return Yaku.KachouFuugetsu.GetHan(hand.Closed, hand.Parent.Settings);
        }

        public static int Evaluate_KinkeiDokuritsu(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            return ((hand.MeldCount == 4) && scHand.PairTile.Type.IsEqual(TileType.Bamboo1)) ? Yaku.KinkeiDokuritsu.GetHan(false, hand.Parent.Settings) : 0;
        }

        public static int Evaluate_OtakazeSankou(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            TileType seatWindTile = TileTypeExtensionMethods.GetSeatWindTile(hand.Player, hand.Parent.Dealer);
            if (seatWindTile.IsEqual(TileTypeExtensionMethods.GetRoundWindTile(hand.Parent.Round)))
            {
                bool fEast = false;
                bool fSouth = false;
                bool fWest = false;
                bool fNorth = false;

                foreach (IMeld meld in hand.Melds)
                {
                    if (meld.State != MeldState.None)
                    {
                        if      (meld.Tiles[0].Type.IsEqual(TileType.East))  { fEast = true; }
                        else if (meld.Tiles[0].Type.IsEqual(TileType.South)) { fSouth = true; }
                        else if (meld.Tiles[0].Type.IsEqual(TileType.West))  { fWest = true; }
                        else if (meld.Tiles[0].Type.IsEqual(TileType.North)) { fNorth = true; }
                    }
                }

                StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
                for (int iClosedMeld = 0; iClosedMeld < (4 - hand.MeldCount); ++iClosedMeld)
                {
                    if      (scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.East))  { fEast = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.South)) { fSouth = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.West))  { fWest = true; }
                    else if (scHand.Melds[iClosedMeld].Tiles[0].Type.IsEqual(TileType.North)) { fNorth = true; }
                }

                if ((seatWindTile.IsEqual(TileType.East)  && !fEast && fSouth  && fWest  && fNorth) ||
                    (seatWindTile.IsEqual(TileType.South) && fEast  && !fSouth && fWest  && fNorth) ||
                    (seatWindTile.IsEqual(TileType.West)  && fEast  && fSouth  && !fWest && fNorth) ||
                    (seatWindTile.IsEqual(TileType.North) && fEast  && fSouth  && fWest  && !fNorth))
                {
                    return Yaku.OtakazeSankou.GetHan(hand.Closed, hand.Parent.Settings);
                }
            }
            return 0;
        }

        public static int Evaluate_Uumensai(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (hand.Closed)
            {
                bool fBamboo = false;
                bool fCircles = false;
                bool fCharacters = false;
                bool fDragon = false;
                bool fWind = false;

                StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
                foreach (TileType tt in new TileType[] { scHand.Melds[0].Tiles[0].Type,
                                                         scHand.Melds[1].Tiles[0].Type,
                                                         scHand.Melds[2].Tiles[0].Type,
                                                         scHand.Melds[3].Tiles[0].Type,
                                                         scHand.PairTile.Type })
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

        public static int Evaluate_TsubameGaeshi(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            if (ron)
            {
                Player playerTarget = ((GameStateImpl)hand.Parent).NextActionPlayer;
                if (playerTarget.IsPlayer())
                {
                    IList<ITile> discards = hand.Discards;
                    if (discards.Count > 0 && discards[discards.Count - 1].Reach.IsReach())
                    {
                        return Yaku.TsubameGaeshi.GetHan(hand.Closed, hand.Parent.Settings);
                    }
                }
            }
            return 0;
        }

        public static int Evaluate_IisouSuushun(IHand hand, ICandidateHand candidateHand, bool ron)
        {
            StandardCandidateHand scHand = candidateHand as StandardCandidateHand;
            int calledMeldCount = hand.MeldCount;
            if (calledMeldCount > 0) // Make sure we have at least one call otherwise you might also get Suuankou.
            {
                // All melds should be chiis and they should all start with the same tile.
                TileType startTile = hand.Melds[0].Tiles[0].Type;

                // Check all the open melds.
                foreach (IMeld meld in hand.Melds)
                {
                    if (meld.State != MeldState.None)
                    {
                        TileType t1 = meld.Tiles[0].Type;
                        TileType t2 = meld.Tiles[1].Type;
                        TileType t3 = meld.Tiles[2].Type;
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
                    if (!startTile.IsEqual(scHand.Melds[i].Tiles[0].Type) || (scHand.Melds[i].State != MeldState.Chii))
                    {
                        return 0;
                    }
                }

                // Done!
                return Yaku.IisouSuushun.GetHan(hand.Closed, hand.Parent.Settings);
            }
            return 0;
        }

        private static bool CheckSevenPairsHandForTiles(ICandidateHand hand, TileType[] tiles)
        {
            List<TileType> tileCollection = new List<TileType>(tiles);
            return (hand as SevenPairsCandidateHand).IteratePairs((TileType tile) => { return tileCollection.Remove(tile); });
        }
    }
}
