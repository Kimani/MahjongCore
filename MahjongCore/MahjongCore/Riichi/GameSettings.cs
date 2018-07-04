// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Impl;
using System;

namespace MahjongCore.Riichi
{
    public enum CustomSettingType
    {
        Rule,
        Yaku
    }

    public enum GameOption
    {
        // Rules
        [DescriptionName("Uma"),                                      OptionValueType(typeof(Uma)),            DefaultOptionValue(Uma.Uma_15_5)                       ] UmaOption,
        [DescriptionName("Red Dora"),                                 OptionValueType(typeof(RedDora)),        DefaultOptionValue(RedDora.RedDora_3)                  ] RedDoraOption,
        [DescriptionName("Oka"),                                      OptionValueType(typeof(Oka)),            DefaultOptionValue(Oka.Oka_None)                       ] OkaOption,
        [DescriptionName("Iisou Sanjun Han"),                         OptionValueType(typeof(IisouSanjunHan)), DefaultOptionValue(IisouSanjunHan.Han_2_1)             ] IisouSanjunHanOption,
        [DescriptionName("Yakitori"),                                 OptionValueType(typeof(Yakitori)),       DefaultOptionValue(Yakitori.Yakitori_Disabled)         ] YakitoriOption,
        [DescriptionName("Starting Points"),                          OptionValueType(typeof(int)),            DefaultOptionValue(25000)                              ] StartingPoints,
        [DescriptionName("Victory Points"),                           OptionValueType(typeof(int)),            DefaultOptionValue(30000)                              ] VictoryPoints,
        [DescriptionName("Dora"),                                     OptionValueType(typeof(bool)),           DefaultOptionValue(true)                               ] Dora,
        [DescriptionName("ChomboPenalty"),                            OptionValueType(typeof(ChomboPenalty)),  DefaultOptionValue(ChomboPenalty.ReverseMangan)        ] ChomboPenaltyOption,
        [DescriptionName("Ura Dora"),                                 OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000002)] UraDora,
        [DescriptionName("Kan Dora"),                                 OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000004)] KanDora,
        [DescriptionName("Tonpussen"),                                OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00000008)] Tonpussen,
        [DescriptionName("Kuitan"),                                   OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000010)] Kuitan,
        [DescriptionName("Agari Yame"),                               OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000020)] EndgameDealerFinish,
        [DescriptionName("Atozuke"),                                  OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000040)] Atozuke,
        [DescriptionName("Sekinin Barai: Rinshan Kaihou"),            OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00000100)] SekininBaraiRinshan,
        [DescriptionName("Sekinin Barai: Daisangen"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00000200)] SekininBaraiDaisangen,
        [DescriptionName("Sekinin Barai: Daisuushii"),                OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00000400)] SekininBaraiDaisuushii,
        [DescriptionName("Sekinin Barai: Tsuuiisou"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00000800)] SekininBaraiTsuuiisou,
        [DescriptionName("Sekinin Barai: Suukantsu"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00001000)] SekininBaraiSuukantsu,
        [DescriptionName("Sekinin Barai: Chinroutou"),                OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00002000)] SekininBaraiChinroutou,
        [DescriptionName("Sekinin Barai: Ryuuiisou"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00004000)] SekininBaraiRyuuiisou,
        [DescriptionName("Sekinin Barai: Iisou Suushun"),             OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00008000)] SekininBaraiIisouSuushun,
        [DescriptionName("Ryanhan Shibari"),                          OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00010000)] RyanhanShibari,
        [DescriptionName("Eight Win Retire"),                         OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00020000)] EightWinRetire,
        [DescriptionName("South Not Ready"),                          OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00040000)] SouthNotReady,
        [DescriptionName("Natural Wins"),                             OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00080000)] NaturalWins,
        [DescriptionName("Sequence Switch"),                          OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x00100000)] SequenceSwitch,
        [DescriptionName("Kan After Riichi"),                         OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00200000)] KanAfterRiichi,
        [DescriptionName("Double Yakuman"),                           OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00400000)] DoubleYakuman,
        [DescriptionName("Pinfu Tsumo"),                              OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x00800000)] PinfuTsumo,
        [DescriptionName("Buttobi"),                                  OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x01000000)] Buttobi,
        [DescriptionName("Kiriage Mangan"),                           OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x02000000)] KiriageMangan,
        [DescriptionName("Same Tile Chiitoi"),                        OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x04000000)] SameTileChiitoi,
        [DescriptionName("Furiten"),                                  OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(1), BitfieldMask(0x08000000)] Furiten,
        [DescriptionName("Wareme"),                                   OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x10000000)] Wareme,
        [DescriptionName("Four Quad Rinshan"),                        OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x20000000)] FourQuadRinshan,
        [DescriptionName("Rinshan Ippatsu"),                          OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x40000000)] RinshanIppatsu,
        [DescriptionName("Suukantsu Double Yakuman"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(1), BitfieldMask(0x80000000)] SuukantsuDoubleYakuman,
        [DescriptionName("Chiitoi 50 Fu"),                            OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00000001)] Chiitoi50Fu,
        [DescriptionName("Tamahane"),                                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00000002)] Tamahane,
        [DescriptionName("Game Winner Gets Pool"),                    OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000004)] WinnerGetsPool,
        [DescriptionName("KyuushuKyuuhai"),                           OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000008)] KyuushuKyuuhai,
        [DescriptionName("Suufurendan"),                              OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000010)] Suufurendan,
        [DescriptionName("Nagashi Mangan Consumes Pool"),             OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00000020)] NagashiConsumesPool,
        [DescriptionName("Nagashi Mangan Scored with Bonus"),         OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00000040)] NagashiUsesBonus,
        [DescriptionName("Nagashi Mangan Bonus only if East Tempai"), OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00000080)] NagashiBonusOnEastTempaiOnly,
        [DescriptionName("Draw on Fourth Reach"),                     OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000100)] FourReachDraw,
        [DescriptionName("Draw on Fourth Kan"),                       OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000200)] FourKanDraw,
        [DescriptionName("Draw on Fifth Kan"),                        OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000400)] FifthKanDraw,
        [DescriptionName("Chombo Penalty applied Post Ranking"),      OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00000800)] ChomboPenaltyPostRank,
        [DescriptionName("Renhou Mangan"),                            OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00001000)] RenhouMangan,
        [DescriptionName("Split Tie Uma"),                            OptionValueType(typeof(bool)), DefaultOptionValue(true),  RuleValue(2), BitfieldMask(0x00002000)] SplitTieUma,
        [DescriptionName("Use Integer Final Scores"),                 OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00004000)] IntFinalScores,
        [DescriptionName("Flip Dora Tiles Immediately"),              OptionValueType(typeof(bool)), DefaultOptionValue(false), RuleValue(2), BitfieldMask(0x00008000)] FlipDoraTilesImmediately,

        // Yaku
        [DescriptionName("Riichi"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000001)] Riichi,
        [DescriptionName("Double Riichi"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000002)] DoubleRiichi,
        [DescriptionName("Open Riichi"),      OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(1), BitfieldMask(0x00000004)] OpenRiichi,
        [DescriptionName("Ippatsu"),          OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000008)] Ippatsu,
        [DescriptionName("Tsumo"),            OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000010)] MenzenTsumo,
        [DescriptionName("Chiitoitsu"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000020)] Chiitoitsu,
        [DescriptionName("Pinfu"),            OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000040)] Pinfu,
        [DescriptionName("Iipeikou"),         OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000080)] Iipeikou,
        [DescriptionName("Sanshoku Doujun"),  OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000100)] SanshokuDoujun,
        [DescriptionName("Ittsuu"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000200)] Ittsuu,
        [DescriptionName("Ryanpeikou"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000400)] Ryanpeikou,
        [DescriptionName("Toitoi"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00000800)] Toitoi,
        [DescriptionName("Sanankou"),         OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00001000)] Sanankou,
        [DescriptionName("Sanshoku Doukou"),  OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00002000)] SanshokuDoukou,
        [DescriptionName("Sankantsu"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00004000)] Sankantsu,
        [DescriptionName("Tanyao"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00008000)] Tanyao,
        [DescriptionName("Chun"),             OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00010000)] Chun,
        [DescriptionName("Haku"),             OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00020000)] Haku,
        [DescriptionName("Hatsu"),            OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00040000)] Hatsu,
        [DescriptionName("Ton"),              OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00080000)] Ton,
        [DescriptionName("Nan"),              OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00100000)] Nan,
        [DescriptionName("Sha"),              OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00200000)] Sha,
        [DescriptionName("Pei"),              OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00400000)] Pei,
        [DescriptionName("Double Ton"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x00800000)] DoubleTon,
        [DescriptionName("Double Nan"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x01000000)] DoubleNan,
        [DescriptionName("Double Sha"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x02000000)] DoubleSha,
        [DescriptionName("Double Pei"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x04000000)] DoublePei,
        [DescriptionName("Chanta"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x08000000)] Chanta,
        [DescriptionName("Honitsu"),          OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x10000000)] Honitsu,
        [DescriptionName("Junchan"),          OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x20000000)] Junchan,
        [DescriptionName("Honroutou"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x40000000)] Honroutou,
        [DescriptionName("Shousangen"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(1), BitfieldMask(0x80000000)] Shousangen,
        [DescriptionName("Chinitsu"),         OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000001)] Chinitsu,
        [DescriptionName("Haitei Raoyue"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000002)] HaiteiRaoyue,
        [DescriptionName("Houtei Raoyui"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000004)] HouteiRaoyui,
        [DescriptionName("Rinshan Kaihou"),   OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000008)] RinshanKaihou,
        [DescriptionName("Chankan"),          OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000010)] Chankan,
        [DescriptionName("Nagashi Mangan"),   OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000020)] NagashiMangan,
        [DescriptionName("Sanrenkou"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00000040)] Sanrenkou,
        [DescriptionName("Iisou Sanjun"),     OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00000080)] IisouSanjun,
        [DescriptionName("Uumensai"),         OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00000100)] Uumensai,
        [DescriptionName("Kanburi"),          OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00000200)] Kanburi,
        [DescriptionName("Tsubame Gaeshi"),   OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00000400)] TsubameGaeshi,
        [DescriptionName("Otakaze Sankou"),   OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00000800)] OtakazeSankou,
        [DescriptionName("Kinkei Dokuritsu"), OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x00001000)] KinkeiDokuritsu,
        [DescriptionName("Kokushi Musou"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00002000)] KokushiMusou,
        [DescriptionName("Chuuren Poutou"),   OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00004000)] ChuurenPoutou,
        [DescriptionName("Suuankou"),         OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00008000)] Suuankou,
        [DescriptionName("Daisangen"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00010000)] Daisangen,
        [DescriptionName("Shousuushii"),      OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00020000)] Shousuushii,
        [DescriptionName("Daisuushii"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00040000)] Daisuushii,
        [DescriptionName("Suukantsu"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00080000)] Suukantsu,
        [DescriptionName("Ryuuiisou"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00100000)] Ryuuiisou,
        [DescriptionName("Chinroutou"),       OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00200000)] Chinroutou,
        [DescriptionName("Tsuuiisou"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00400000)] Tsuuiisou,
        [DescriptionName("Daisharin"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x00800000)] Daisharin,
        [DescriptionName("Daichikurin"),      OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x01000000)] Daichikurin,
        [DescriptionName("Daisuurin"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x02000000)] Daisuurin,
        [DescriptionName("Shiisan Budou"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x04000000)] ShiisanBudou,
        [DescriptionName("Paa Renchan"),      OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(2), BitfieldMask(0x08000000)] PaaRenchan,
        [DescriptionName("Tenhou"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x10000000)] Tenhou,
        [DescriptionName("Chiihou"),          OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x20000000)] Chiihou,
        [DescriptionName("Renhou"),           OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x40000000)] Renhou,
        [DescriptionName("Daichisei"),        OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(2), BitfieldMask(0x80000000)] Daichisei,
        [DescriptionName("Suurenkou"),        OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000001)] Suurenkou,
        [DescriptionName("Hyakuman Goku"),    OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000002)] HyakumanGoku,
        [DescriptionName("Beni Kujaku"),      OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000004)] BeniKujaku,
        [DescriptionName("Ao No Doumon"),     OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000008)] AoNoDoumon,
        [DescriptionName("Shiisuu Puuta"),    OptionValueType(typeof(bool)), DefaultOptionValue(true),  YakuValue(3), BitfieldMask(0x00000010)] ShiisuuPuuta,
        [DescriptionName("Uupin Kaihou"),     OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000020)] UupinKaihou,
        [DescriptionName("Iipin Raoyui"),     OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000040)] IipinRaoyui,
        [DescriptionName("Ryansou Chankan"),  OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000080)] RyansouChankan,
        [DescriptionName("Kachou Fuugetsu"),  OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000100)] KachouFuugetsu,
        [DescriptionName("Shousharin"),       OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000200)] Shousharin,
        [DescriptionName("Shouchikurin"),     OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000400)] Shouchikurin,
        [DescriptionName("Shousuurin"),       OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00000800)] Shousuurin,
        [DescriptionName("Iisou Suushun"),    OptionValueType(typeof(bool)), DefaultOptionValue(false), YakuValue(3), BitfieldMask(0x00001000)] IisouSuushun
    }

    #region IisouSanjunHan
        public enum IisouSanjunHan
        {
            [DefaultClosedHan(2), DefaultOpenHan(1), DescriptionName("2 Closed / 1 Open"), TextValue("0")] Han_2_1,
            [DefaultClosedHan(2), DefaultOpenHan(2), DescriptionName("2 Closed / 2 Open"), TextValue("1")] Han_2_2,
            [DefaultClosedHan(3), DefaultOpenHan(2), DescriptionName("3 Closed / 2 Open"), TextValue("2")] Han_3_2
        };

        public static class IisouSanjunHanExtensionMethods
        {
            public static string GetTextValue(this IisouSanjunHan ish) { return EnumAttributes.GetAttributeValue<TextValue, string>(ish); }

            public static bool TryGetIisouSanjunHan(string text, out IisouSanjunHan ish)
            {
                IisouSanjunHan? result = EnumHelper.GetEnumValueFromAttribute<IisouSanjunHan, TextValue, string>(text);
                ish = (result != null) ? result.Value : default(IisouSanjunHan);
                return result != null;
            }

            public static IisouSanjunHan GetIisouSanjunHan(string text)
            {
                IisouSanjunHan? result = EnumHelper.GetEnumValueFromAttribute<IisouSanjunHan, TextValue, string>(text);
                CommonHelpers.Check((result != null), "Failed to parse IisouSanjunHan: " + text);
                return result.Value;
            }
        }
    #endregion

    #region RedDora
        public enum RedDora
        {
            [DescriptionName("0"), TextValue("0"), RedDoraManzu(0), RedDoraPinzu(0), RedDoraSouzu(0)] RedDora_0,
            [DescriptionName("3"), TextValue("3"), RedDoraManzu(1), RedDoraPinzu(1), RedDoraSouzu(1)] RedDora_3,
            [DescriptionName("4"), TextValue("4"), RedDoraManzu(1), RedDoraPinzu(2), RedDoraSouzu(1)] RedDora_4
        };

        public static class RedDoraExtensionMethods
        {
            public static int GetRedDoraManzu(this RedDora d) { return EnumAttributes.GetAttributeValue<RedDoraManzu, int>(d); }
            public static int GetRedDoraPinzu(this RedDora d) { return EnumAttributes.GetAttributeValue<RedDoraPinzu, int>(d); }
            public static int GetRedDoraSouzu(this RedDora d) { return EnumAttributes.GetAttributeValue<RedDoraSouzu, int>(d); }
            public static string GetTextValue(this RedDora d) { return EnumAttributes.GetAttributeValue<TextValue, string>(d); }

            public static bool TryGetRedDora(string text, out RedDora rd)
            {
                RedDora? result = EnumHelper.GetEnumValueFromAttribute<RedDora, TextValue, string>(text);
                rd = (result != null) ? result.Value : default(RedDora);
                return result != null;
            }

            public static RedDora GetRedDora(string text)
            {
                RedDora? result = EnumHelper.GetEnumValueFromAttribute<RedDora, TextValue, string>(text);
                CommonHelpers.Check((result != null), "Failed to parse RedDora: " + text);
                return result.Value;
            }
        }
    #endregion

    #region Uma
        public enum Uma
        {
            [DescriptionName("None"),            TextValue("0"), Place1Value(0),  Place2Value(0),  Place3Value(0),   Place4Value(0)  ] Uma_None,
            [DescriptionName("+9/3/-3/-9"),      TextValue("1"), Place1Value(9),  Place2Value(3),  Place3Value(-3),  Place4Value(-9) ] Uma_9_3,
            [DescriptionName("+10/+5/-5/-10"),   TextValue("2"), Place1Value(10), Place2Value(5),  Place3Value(-5),  Place4Value(-10)] Uma_10_5,
            [DescriptionName("+15/+5/-5/-15"),   TextValue("3"), Place1Value(15), Place2Value(5),  Place3Value(-5),  Place4Value(-15)] Uma_15_5,
            [DescriptionName("+20/+10/-10/-20"), TextValue("4"), Place1Value(20), Place2Value(10), Place3Value(-10), Place4Value(-20)] Uma_20_10,
            [DescriptionName("+30/+10/-10/-30"), TextValue("5"), Place1Value(30), Place2Value(10), Place3Value(-10), Place4Value(-30)] Uma_30_10
        };

        public static class UmaExtensionMethods
        {
            public static string GetTextValue(this Uma u) { return EnumAttributes.GetAttributeValue<TextValue, string>(u); }

            public static int GetScoreDelta(this Uma u, Placement place)
            {
                return (place == Placement.Place1) ? EnumAttributes.GetAttributeValue<Place1Value, int>(u) :
                       (place == Placement.Place2) ? EnumAttributes.GetAttributeValue<Place2Value, int>(u) :
                       (place == Placement.Place3) ? EnumAttributes.GetAttributeValue<Place3Value, int>(u) :
                                                     EnumAttributes.GetAttributeValue<Place4Value, int>(u);
            }

            public static bool TryGetUma(string text, out Uma u)
            {
                Uma? result = EnumHelper.GetEnumValueFromAttribute<Uma, TextValue, string>(text);
                u = (result != null) ? result.Value : default(Uma);
                return result != null;
            }

            public static Uma GetUma(string text)
            {
                Uma? result = EnumHelper.GetEnumValueFromAttribute<Uma, TextValue, string>(text);
                CommonHelpers.Check((result != null), ("Failed to parse Uma: " + text));
                return result.Value;
            }
        }
    #endregion

    #region Oka
        public enum Oka
        {
            [DescriptionName("None"), TextValue("0"), Place1Value(0)]  Oka_None,
            [DescriptionName("5"),    TextValue("1"), Place1Value(5)]  Oka_5,
            [DescriptionName("10"),   TextValue("2"), Place1Value(10)] Oka_10,
            [DescriptionName("15"),   TextValue("3"), Place1Value(15)] Oka_15,
            [DescriptionName("20"),   TextValue("4"), Place1Value(20)] Oka_20,
            [DescriptionName("25"),   TextValue("5"), Place1Value(25)] Oka_25,
            [DescriptionName("30"),   TextValue("6"), Place1Value(30)] Oka_30
        };

        public static class OkaExtensionMethods
        {
            public static int    GetDelta(this Oka o)     { return EnumAttributes.GetAttributeValue<Place1Value, int>(o); }
            public static string GetTextValue(this Oka o) { return EnumAttributes.GetAttributeValue<TextValue, string>(o); }

            public static bool TryGetOka(string text, out Oka o)
            {
                Oka? result = EnumHelper.GetEnumValueFromAttribute<Oka, TextValue, string>(text);
                o = (result != null) ? result.Value : default(Oka);
                return result != null;
            }

            public static Oka GetOka(string text)
            {
                Oka? result = EnumHelper.GetEnumValueFromAttribute<Oka, TextValue, string>(text);
                CommonHelpers.Check((result != null), ("Failed to parse Oka: " + text));
                return result.Value;
            }
        }
    #endregion

    #region Yakitori
        public enum Yakitori
        {
            [DescriptionName("Disabled"),           TextValue("0"), Place1Value(0),  YakitoriDelta(0)]      Yakitori_Disabled,
            [DescriptionName("-1"),                 TextValue("1"), Place1Value(0),  YakitoriDelta(-1000)]  Yakitori_1,
            [DescriptionName("-5"),                 TextValue("2"), Place1Value(0),  YakitoriDelta(-5000)]  Yakitori_5,
            [DescriptionName("-10"),                TextValue("3"), Place1Value(0),  YakitoriDelta(-10000)] Yakitori_10,
            [DescriptionName("-15"),                TextValue("4"), Place1Value(0),  YakitoriDelta(-15000)] Yakitori_15,
            [DescriptionName("-20"),                TextValue("5"), Place1Value(0),  YakitoriDelta(-20000)] Yakitori_20,
            [DescriptionName("-25"),                TextValue("6"), Place1Value(0),  YakitoriDelta(-25000)] Yakitori_25,
            [DescriptionName("-1, 1st Place +1"),   TextValue("7"), Place1Value(1),  YakitoriDelta(-1000)]  Yakitori_1_1stBonus,
            [DescriptionName("-5, 1st Place +5"),   TextValue("8"), Place1Value(5),  YakitoriDelta(-5000)]  Yakitori_5_1stBonus,
            [DescriptionName("-10, 1st Place +10"), TextValue("9"), Place1Value(10), YakitoriDelta(-10000)] Yakitori_10_1stBonus,
            [DescriptionName("-15, 1st Place +15"), TextValue("a"), Place1Value(15), YakitoriDelta(-15000)] Yakitori_15_1stBonus,
            [DescriptionName("-20, 1st Place +20"), TextValue("b"), Place1Value(20), YakitoriDelta(-20000)] Yakitori_20_1stBonus,
            [DescriptionName("-25, 1st Place +25"), TextValue("c"), Place1Value(25), YakitoriDelta(-25000)] Yakitori_25_1stBonus,
        }

        public static class YakitoriExtensionMethods
        {
            public static string GetTextValue(this Yakitori y) { return EnumAttributes.GetAttributeValue<TextValue, string>(y); }
            public static int GetDelta(this Yakitori y)        { return EnumAttributes.GetAttributeValue<YakitoriDelta, int>(y); }

            public static bool TryGetYakitori(string text, out Yakitori y)
            {
                Yakitori? result = EnumHelper.GetEnumValueFromAttribute<Yakitori, TextValue, string>(text);
                y = (result != null) ? result.Value : default(Yakitori);
                return result != null;
            }

            public static Yakitori GetYakitori(string text)
            {
                Yakitori? result = EnumHelper.GetEnumValueFromAttribute<Yakitori, TextValue, string>(text);
                CommonHelpers.Check((result != null), "Failed to parse RedDora: " + text);
                return result.Value;
            }
        }
    #endregion

    #region CustomBitfields
        public enum CustomBitfields
        {
            [TextValue("customgamerules"),  TargetTypeKey(CustomSettingType.Rule), TargetTypeValue(1)] CustomGameRules1,
            [TextValue("customgamerules2"), TargetTypeKey(CustomSettingType.Rule), TargetTypeValue(2)] CustomGameRules2,
            [TextValue("customgameyaku1"),  TargetTypeKey(CustomSettingType.Yaku), TargetTypeValue(1)] CustomGameYaku1,
            [TextValue("customgameyaku2"),  TargetTypeKey(CustomSettingType.Yaku), TargetTypeValue(2)] CustomGameYaku2,
            [TextValue("customgameyaku3"),  TargetTypeKey(CustomSettingType.Yaku), TargetTypeValue(3)] CustomGameYaku3,
        }

        public static class CustomBitfieldsExtensionMethods
        {
            public static CustomSettingType GetCustomSettingType(this CustomBitfields cb) { return EnumAttributes.GetAttributeValue<TargetTypeKey, CustomSettingType>(cb); }
            public static int               GetTargetTypeValue(this CustomBitfields cb)   { return EnumAttributes.GetAttributeValue<TargetTypeValue, int>(cb); }
        }
    #endregion

    #region ChomboPenalty
        public enum ChomboPenalty
        {
            [DescriptionName("Reverse Mangan"), PointLoss(8000)]  ReverseMangan,
            [DescriptionName("3000 All"),       PointLoss(9000)]  Reverse3000All,
            [DescriptionName("-8000 Points"),   PointLoss(8000)]  Penalty8000,
            [DescriptionName("-12000 Points"),  PointLoss(12000)] Penalty12000,
            [DescriptionName("-20000 Points"),  PointLoss(20000)] Penalty20000,
        }

        public static class ChomboPenaltyExtensionMethods
        {
            public static int GetPointLoss(this ChomboPenalty cp) { return EnumAttributes.GetAttributeValue<PointLoss, int>(cp); }
        }
    #endregion

    public interface IGameSettings : ICloneable
    {
        T    GetSetting<T>(GameOption option);
        void SetSetting(GameOption option, object value);
        bool HasCustomSettings();
        void Reset();
    }

    public static class GameSettingsFactory
    {
        public static IGameSettings BuildGameSettings() { return new GameSettingsImpl(); }
    }
}
