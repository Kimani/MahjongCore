// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;

namespace MahjongCore.Riichi
{
    public enum TileSource
    {
        Wall,     // Tile came from the wall. A regular draw.
        DeadWall, // Tile came from the dead wall after a kan.
        Call,     // Tile came from performing a chii or a pon.
    }

    public enum Placement
    {
        Place1,
        Place2,
        Place3,
        Place4
    }

    public enum AbortiveDrawType
    {
        Suufuurendan,
        KyuushuuKyuuhai,
        FourKans,
        FiveKans,
        FourReach,
        Other
    }

    #region Wind
        public enum Wind
        {
                                                                         None,
            [Tile(TileType.East),  WindNext(South), WindPrevious(North)] East,
            [Tile(TileType.South), WindNext(West),  WindPrevious(East)]  South,
            [Tile(TileType.West),  WindNext(North), WindPrevious(South)] West,
            [Tile(TileType.North), WindNext(East),  WindPrevious(West)]  North,
        }

        public static class WindExtensionMethods
        {
            public static TileType GetTile(this Wind w) { return EnumAttributes.GetAttributeValue<Tile, TileType>(w); }
        
            public static Wind GetWind(Player current, Player dealer)
            {
                Global.Assert(current.IsPlayer());
                Global.Assert(dealer.IsPlayer());
                return (current == dealer)           ? Wind.East :
                       (current == dealer.GetNext()) ? Wind.South :
                       (current.GetNext() == dealer) ? Wind.North :
                                                       Wind.West;
            }
        }
    #endregion

    #region Round
        public enum Round
        {
            [DescriptionName("East 1"),  TextValue("e1"), RoundOffset(0), NextRound(East2)]  East1,
            [DescriptionName("East 2"),  TextValue("e2"), RoundOffset(1), NextRound(East3)]  East2,
            [DescriptionName("East 3"),  TextValue("e3"), RoundOffset(2), NextRound(East4)]  East3,
            [DescriptionName("East 4"),  TextValue("e4"), RoundOffset(3), NextRound(South1)] East4,
            [DescriptionName("South 1"), TextValue("s1"), RoundOffset(0), NextRound(South2)] South1,
            [DescriptionName("South 2"), TextValue("s2"), RoundOffset(1), NextRound(South3)] South2,
            [DescriptionName("South 3"), TextValue("s3"), RoundOffset(2), NextRound(South4)] South3,
            [DescriptionName("South 4"), TextValue("s4"), RoundOffset(3), NextRound(West1)]  South4,
            [DescriptionName("West 1"),  TextValue("w1"), RoundOffset(0), NextRound(West2)]  West1,
            [DescriptionName("West 2"),  TextValue("w2"), RoundOffset(1), NextRound(West3)]  West2,
            [DescriptionName("West 3"),  TextValue("w3"), RoundOffset(2), NextRound(West4)]  West3,
            [DescriptionName("West 4"),  TextValue("w4"), RoundOffset(3), NextRound(North1)] West4,
            [DescriptionName("North 1"), TextValue("n1"), RoundOffset(0), NextRound(North2)] North1,
            [DescriptionName("North 2"), TextValue("n2"), RoundOffset(1), NextRound(North3)] North2,
            [DescriptionName("North 3"), TextValue("n3"), RoundOffset(2), NextRound(North4)] North3,
            [DescriptionName("North 4"), TextValue("n4"), RoundOffset(3), NextRound(East1)]  North4
        };

        public static class RoundExtensionMethods
        {
            public static Round  GetNext(this Round r)      { return EnumAttributes.GetAttributeValue<NextRound, Round>(r); }
            public static int    GetOffset(this Round r)    { return EnumAttributes.GetAttributeValue<RoundOffset, int>(r); }
            public static string GetTextValue(this Round r) { return EnumAttributes.GetAttributeValue<TextValue, string>(r); }
            public static string GetDescName(this Round r)  { return EnumAttributes.GetAttributeValue<DescriptionName, string>(r); }

            public static bool TryGetRound(string text, out Round c)
            {
                Round? result = EnumHelper.GetEnumValueFromAttribute<Round, TextValue, string>(text);
                c = (result != null) ? result.Value : default(Round);
                return result != null;
            }

            public static Round GetRound(string text)
            {
                Round? result = EnumHelper.GetEnumValueFromAttribute<Round, TextValue, string>(text);
                CommonHelpers.Check((result != null), ("Failed to parse into Round: " + text));
                return result.Value;
            }
        }
    #endregion

    #region Player
        public enum Player
        {
            [IsSinglePlayer(false), PlayerValue(0)]                                                                                           None,
            [IsSinglePlayer(false), PlayerValue(5)]                                                                                           All,
            [IsSinglePlayer(false), PlayerValue(6)]                                                                                           Multiple,
            [IsSinglePlayer(true),  PlayerNext(Player.Player2), PlayerPrevious(Player.Player4), TextValue("1"), PlayerValue(1), ZeroIndex(0)] Player1,
            [IsSinglePlayer(true),  PlayerNext(Player.Player3), PlayerPrevious(Player.Player1), TextValue("2"), PlayerValue(2), ZeroIndex(1)] Player2,
            [IsSinglePlayer(true),  PlayerNext(Player.Player4), PlayerPrevious(Player.Player2), TextValue("3"), PlayerValue(3), ZeroIndex(2)] Player3,
            [IsSinglePlayer(true),  PlayerNext(Player.Player1), PlayerPrevious(Player.Player3), TextValue("4"), PlayerValue(4), ZeroIndex(3)] Player4
        };

        public static class PlayerExtensionMethods
        {
            public static Player[] Players = new Player[] { Player.Player1, Player.Player2, Player.Player3, Player.Player4 };

            public static bool   IsPlayer(this Player p)                 { return EnumAttributes.GetAttributeValue<IsSinglePlayer, bool>(p); }
            public static int    GetZeroIndex(this Player p)             { return EnumAttributes.GetAttributeValue<ZeroIndex, int>(p); }
            public static int    GetPlayerValue(this Player p)           { return EnumAttributes.GetAttributeValue<PlayerValue, int>(p); }
            public static bool   TryGetPlayer(string text, out Player p) { return EnumHelper.TryGetEnumByCode<Player, PlayerValue>(text, out p); }
            public static Player GetNext(this Player p)                  { return EnumAttributes.GetAttributeValue<PlayerNext, Player>(p); }
            public static Player GetPrevious(this Player p)              { return EnumAttributes.GetAttributeValue<PlayerPrevious, Player>(p); }

            public static CalledDirection GetTargetPlayerDirection(this Player p, Player target)
            {
                return (!p.IsPlayer() || (p == target)) ? CalledDirection.None :
                        (p.GetNext() == target)         ? CalledDirection.Right :
                        (p.GetPrevious() == target)     ? CalledDirection.Left :
                                                          CalledDirection.Across;
            }

            public static Player AddOffset(this Player p, int offset)
            {
                int playerValue = EnumAttributes.GetAttributeValue<PlayerValue, int>(p);
                int offsetValue = playerValue + offset - 1;
                while (offsetValue < 0)
                {
                    offsetValue += 4;
                }
                int targetPlayer = (offsetValue % 4) + 1;
                return (targetPlayer == 1) ? Player.Player1 :
                       (targetPlayer == 2) ? Player.Player2 :
                       (targetPlayer == 3) ? Player.Player3 :
                                             Player.Player4;
            }

            public static Player GetRandom()
            {
                int targetPlayer = Global.RandomRange(1, 5);
                return (targetPlayer == 1) ? Player.Player1 :
                       (targetPlayer == 2) ? Player.Player2 :
                       (targetPlayer == 3) ? Player.Player3 :
                                             Player.Player4;
            }

            public static Player GetPlayer(string text)
            {
                Player p;
                CommonHelpers.Check(EnumHelper.TryGetEnumByCode<Player, PlayerValue>(text, out p), ("Failed to parse into Player: " + text));
                return p;
            }
        }
    #endregion
}
