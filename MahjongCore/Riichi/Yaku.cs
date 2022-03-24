// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Attributes;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Evaluator;

namespace MahjongCore.Riichi
{
    public enum Yaku
    {
        [DefaultClosedHan(1),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Riichi)]          Riichi,
        [DefaultClosedHan(2),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.DoubleRiichi)]    DoubleRiichi,
        [DefaultClosedHan(2),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.OpenRiichi)]      OpenRiichi,
        [DefaultClosedHan(2),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Chiitoitsu)]      Chiitoitsu,
        [DefaultClosedHan(1),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Pinfu)]           Pinfu,
        [DefaultClosedHan(1),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Iipeikou)]        Iipeikou,
        [DefaultClosedHan(2),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.SanshokuDoujun)]  SanshokuDoujun,
        [DefaultClosedHan(2),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Ittsuu)]          Ittsuu,
        [DefaultClosedHan(3),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Ryanpeikou)]      Ryanpeikou,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Toitoi)]          Toitoi,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Sanankou)]        Sanankou,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.SanshokuDoukou)]  SanshokuDoukou,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Sankantsu)]       Sankantsu,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Tanyao)]          Tanyao,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Chun)]            Chun,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Haku)]            Haku,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Hatsu)]           Hatsu,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Ton)]             Ton,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.DoubleTon)]       DoubleTon,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Nan)]             Nan,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.DoubleNan)]       DoubleNan,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Sha)]             Sha,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.DoubleSha)]       DoubleSha,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Pei)]             Pei,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.DoublePei)]       DoublePei,
        [DefaultClosedHan(2),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Chanta)]          Chanta,
        [DefaultClosedHan(3),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Honitsu)]         Honitsu,
        [DefaultClosedHan(3),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Junchan)]         Junchan,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Honroutou)]       Honroutou,
        [DefaultClosedHan(4),  DefaultOpenHan(4),  IsYakuman(false), GameOptionSetting(GameOption.Shousangen)]      Shousangen,
        [DefaultClosedHan(6),  DefaultOpenHan(5),  IsYakuman(false), GameOptionSetting(GameOption.Chinitsu)]        Chinitsu,
        [DefaultClosedHan(1),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.MenzenTsumo)]     MenzenTsumo,
        [DefaultClosedHan(1),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Ippatsu)]         Ippatsu,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.HaiteiRaoyue)]    HaiteiRaoyue,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.HouteiRaoyui)]    HouteiRaoyui,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.RinshanKaihou)]   RinshanKaihou,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Chankan)]         Chankan,
        [DefaultClosedHan(5),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.NagashiMangan)]   NagashiMangan,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.KokushiMusou)]    KokushiMusou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.ChuurenPoutou)]   ChuurenPoutou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Suuankou)]        Suuankou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Daisangen)]       Daisangen,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Shousuushii)]     Shousuushii,
        [DefaultClosedHan(-2), DefaultOpenHan(-2), IsYakuman(true),  GameOptionSetting(GameOption.Daisuushii)]      Daisuushii,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Suukantsu)]       Suukantsu,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Ryuuiisou)]       Ryuuiisou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Chinroutou)]      Chinroutou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Tsuuiisou)]       Tsuuiisou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Daisuurin)]       Daisuurin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Daisharin)]       Daisharin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Daichikurin)]     Daichikurin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.ShiisanBudou)]    ShiisanBudou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Chiihou)]         Chiihou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Renhou)]          Renhou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Tenhou)]          Tenhou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Daichisei)]       Daichisei,
        [DefaultClosedHan(2),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.Sanrenkou)]       Sanrenkou,
        [DefaultClosedHan(0),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.IisouSanjun)]     IisouSanjun,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Suurenkou)]       Suurenkou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.HyakumanGoku)]    HyakumanGoku,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.BeniKujaku)]      BeniKujaku,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.AoNoDoumon)]      AoNoDoumon,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.ShiisuuPuuta)]    ShiisuuPuuta,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.UupinKaihou)]     UupinKaihou,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.IipinRaoyui)]     IipinRaoyui,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.RyansouChankan)]  RyansouChankan,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.KachouFuugetsu)]  KachouFuugetsu,
        [DefaultClosedHan(5),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.KinkeiDokuritsu)] KinkeiDokuritsu,
        [DefaultClosedHan(3),  DefaultOpenHan(2),  IsYakuman(false), GameOptionSetting(GameOption.OtakazeSankou)]   OtakazeSankou,
        [DefaultClosedHan(2),  DefaultOpenHan(0),  IsYakuman(false), GameOptionSetting(GameOption.Uumensai)]        Uumensai,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.Kanburi)]         Kanburi,
        [DefaultClosedHan(1),  DefaultOpenHan(1),  IsYakuman(false), GameOptionSetting(GameOption.TsubameGaeshi)]   TsubameGaeshi,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.PaaRenchan)]      PaaRenchan,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Shousharin)]      Shousharin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Shouchikurin)]    Shouchikurin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.Shousuurin)]      Shousuurin,
        [DefaultClosedHan(-1), DefaultOpenHan(-1), IsYakuman(true),  GameOptionSetting(GameOption.IisouSuushun)]    IisouSuushun
    }

    public static class YakuExtensionMethods
    {
        public static bool       IsYakuman(this Yaku yaku)                                                    { return EnumAttributes.GetAttributeValue<IsYakuman, bool>(yaku); }
        public static GameOption GetSetting(this Yaku yaku)                                                   { return EnumAttributes.GetAttributeValue<GameOptionSetting, GameOption>(yaku); }
        public static int        Evaluate(this Yaku yaku, IHand hand, ICandidateHand candidateHand, bool ron) { return hand.Parent.Settings.GetSetting<bool>(yaku.GetSetting()) ? YakuEvaluator.Evaluate(yaku, hand, candidateHand, ron) : 0; }

        public static int GetHan(this Yaku y, bool closed, IGameSettings settings)
        {
            int han;
            if (y == Yaku.IisouSanjun)
            {
                var hanOption = settings.GetSetting<IisouSanjunHan>(GameOption.IisouSanjunHanOption);
                han = closed ? EnumAttributes.GetAttributeValue<DefaultClosedHan, int>(hanOption) :
                               EnumAttributes.GetAttributeValue<DefaultOpenHan, int>(hanOption);
            }
            else
            {
                han = closed ? EnumAttributes.GetAttributeValue<DefaultClosedHan, int>(y) :
                               EnumAttributes.GetAttributeValue<DefaultOpenHan, int>(y);

                if ((han == -2) && !settings.GetSetting<bool>(GameOption.DoubleYakuman))
                {
                    han = -1;
                }
            }
            return han;
        }
    }
}
