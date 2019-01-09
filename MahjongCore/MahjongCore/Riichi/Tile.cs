// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Riichi.Impl;
using System;
using System.Text;

namespace MahjongCore.Riichi
{
    #region Suit
        public enum Suit
        {
            [IsSuit(false), IsHonor(false)] None,
            [IsSuit(true),  IsHonor(false)] Circles,
            [IsSuit(true),  IsHonor(false)] Bamboo,
            [IsSuit(true),  IsHonor(false)] Characters,
            [IsSuit(true),  IsHonor(true)]  North,
            [IsSuit(true),  IsHonor(true)]  East,
            [IsSuit(true),  IsHonor(true)]  West,
            [IsSuit(true),  IsHonor(true)]  South,
            [IsSuit(true),  IsHonor(true)]  Chun,
            [IsSuit(true),  IsHonor(true)]  Haku,
            [IsSuit(true),  IsHonor(true)]  Hatsu
        }

        public static class SuitExtensionMethods
        {
            public static bool IsHonor(this Suit s) { return EnumAttributes.GetAttributeValue<IsHonor, bool>(s); }
            public static bool IsSuit(this Suit s)  { return EnumAttributes.GetAttributeValue<IsSuit, bool>(s); }
        }
    #endregion

    #region TileType
        public enum TileType
        {
            [IsRealTile(true),  IsTerminal(true),  TileOrder(1),   TileValue(1),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(0),  TextValue("q")] Characters1,
            [IsRealTile(true),  IsTerminal(false), TileOrder(2),   TileValue(2),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(1),  TextValue("w")] Characters2,
            [IsRealTile(true),  IsTerminal(false), TileOrder(3),   TileValue(3),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(2),  TextValue("e")] Characters3,
            [IsRealTile(true),  IsTerminal(false), TileOrder(4),   TileValue(4),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(3),  TextValue("r")] Characters4,
            [IsRealTile(true),  IsTerminal(false), TileOrder(5),   TileValue(5),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(4),  TextValue("t")] Characters5,
            [IsRealTile(false), IsTerminal(false), TileOrder(6),   TileValue(5),  TileSuit(Suit.Characters), IsRedDora(true),  SkyValue(5),  TextValue("t")] Characters5Red,
            [IsRealTile(true),  IsTerminal(false), TileOrder(7),   TileValue(6),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(6),  TextValue("y")] Characters6,
            [IsRealTile(true),  IsTerminal(false), TileOrder(8),   TileValue(7),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(7),  TextValue("u")] Characters7,
            [IsRealTile(true),  IsTerminal(false), TileOrder(9),   TileValue(8),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(8),  TextValue("i")] Characters8,
            [IsRealTile(true),  IsTerminal(true),  TileOrder(10),  TileValue(9),  TileSuit(Suit.Characters), IsRedDora(false), SkyValue(9),  TextValue("o")] Characters9,
            [IsRealTile(true),  IsTerminal(true),  TileOrder(11),  TileValue(1),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(10), TextValue("a")] Bamboo1,
            [IsRealTile(true),  IsTerminal(false), TileOrder(12),  TileValue(2),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(11), TextValue("s")] Bamboo2,
            [IsRealTile(true),  IsTerminal(false), TileOrder(13),  TileValue(3),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(12), TextValue("d")] Bamboo3,
            [IsRealTile(true),  IsTerminal(false), TileOrder(14),  TileValue(4),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(13), TextValue("f")] Bamboo4,
            [IsRealTile(true),  IsTerminal(false), TileOrder(15),  TileValue(5),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(14), TextValue("g")] Bamboo5,
            [IsRealTile(false), IsTerminal(false), TileOrder(16),  TileValue(5),  TileSuit(Suit.Bamboo),     IsRedDora(true),  SkyValue(15), TextValue("g")] Bamboo5Red,
            [IsRealTile(true),  IsTerminal(false), TileOrder(17),  TileValue(6),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(16), TextValue("h")] Bamboo6,
            [IsRealTile(true),  IsTerminal(false), TileOrder(18),  TileValue(7),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(17), TextValue("j")] Bamboo7,
            [IsRealTile(true),  IsTerminal(false), TileOrder(19),  TileValue(8),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(18), TextValue("k")] Bamboo8,
            [IsRealTile(true),  IsTerminal(true),  TileOrder(20),  TileValue(9),  TileSuit(Suit.Bamboo),     IsRedDora(false), SkyValue(19), TextValue("l")] Bamboo9,
            [IsRealTile(true),  IsTerminal(true),  TileOrder(21),  TileValue(1),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(20), TextValue("z")] Circles1,
            [IsRealTile(true),  IsTerminal(false), TileOrder(22),  TileValue(2),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(21), TextValue("x")] Circles2,
            [IsRealTile(true),  IsTerminal(false), TileOrder(23),  TileValue(3),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(22), TextValue("c")] Circles3,
            [IsRealTile(true),  IsTerminal(false), TileOrder(24),  TileValue(4),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(23), TextValue("v")] Circles4,
            [IsRealTile(true),  IsTerminal(false), TileOrder(25),  TileValue(5),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(24), TextValue("b")] Circles5,
            [IsRealTile(false), IsTerminal(false), TileOrder(26),  TileValue(5),  TileSuit(Suit.Circles),    IsRedDora(true),  SkyValue(25), TextValue("b")] Circles5Red,
            [IsRealTile(true),  IsTerminal(false), TileOrder(27),  TileValue(6),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(26), TextValue("n")] Circles6,
            [IsRealTile(true),  IsTerminal(false), TileOrder(28),  TileValue(7),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(27), TextValue("m")] Circles7,
            [IsRealTile(true),  IsTerminal(false), TileOrder(29),  TileValue(8),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(28), TextValue(",")] Circles8,
            [IsRealTile(true),  IsTerminal(true),  TileOrder(30),  TileValue(9),  TileSuit(Suit.Circles),    IsRedDora(false), SkyValue(29), TextValue(".")] Circles9,
            [IsRealTile(true),  IsTerminal(false), TileOrder(31),  TileValue(1),  TileSuit(Suit.North),      IsRedDora(false), SkyValue(33), TextValue("4")] North,
            [IsRealTile(true),  IsTerminal(false), TileOrder(32),  TileValue(1),  TileSuit(Suit.East),       IsRedDora(false), SkyValue(30), TextValue("1")] East,
            [IsRealTile(true),  IsTerminal(false), TileOrder(33),  TileValue(1),  TileSuit(Suit.South),      IsRedDora(false), SkyValue(31), TextValue("2")] South,
            [IsRealTile(true),  IsTerminal(false), TileOrder(34),  TileValue(1),  TileSuit(Suit.West),       IsRedDora(false), SkyValue(32), TextValue("3")] West,
            [IsRealTile(true),  IsTerminal(false), TileOrder(35),  TileValue(1),  TileSuit(Suit.Chun),       IsRedDora(false), SkyValue(34), TextValue("7")] Chun,
            [IsRealTile(true),  IsTerminal(false), TileOrder(36),  TileValue(1),  TileSuit(Suit.Hatsu),      IsRedDora(false), SkyValue(35), TextValue("6")] Hatsu,
            [IsRealTile(true),  IsTerminal(false), TileOrder(37),  TileValue(1),  TileSuit(Suit.Haku),       IsRedDora(false), SkyValue(36), TextValue("5")] Haku,
            [IsRealTile(false), IsTerminal(false), TileOrder(100), TileValue(-1), TileSuit(Suit.None),       IsRedDora(false), SkyValue(41), TextValue(" ")] None
        }

        public static class TileTypeExtensionMethods
        {
            public static bool     IsTile(this TileType tt)           { return tt != TileType.None; }
            public static bool     IsSuit(this TileType a, Suit s)    { return a.GetSuit() == s; }
            public static bool     IsDragon(this TileType a)          { return a.IsEqual(TileType.Hatsu) || a.IsEqual(TileType.Haku) || a.IsEqual(TileType.Chun); }
            public static bool     IsWind(this TileType a)            { return a.IsEqual(TileType.North) || a.IsEqual(TileType.East) || a.IsEqual(TileType.South) || a.IsEqual(TileType.West); }
            public static TileType GetNext(this TileType t)           { return TileHelpers.BuildTile(EnumAttributes.GetAttributeValue<TileSuit, Suit>(t), EnumAttributes.GetAttributeValue<TileValue, int>(t) + 1); }
            public static TileType GetPrev(this TileType t)           { return TileHelpers.BuildTile(EnumAttributes.GetAttributeValue<TileSuit, Suit>(t), EnumAttributes.GetAttributeValue<TileValue, int>(t) - 1); }
            public static bool     IsTerminalOrHonor(this TileType t) { return EnumAttributes.GetAttributeValue<IsTerminal, bool>(t) || EnumAttributes.GetAttributeValue<TileSuit, Suit>(t).IsHonor(); }
            public static bool     IsHonor(this TileType t)           { return EnumAttributes.GetAttributeValue<TileSuit, Suit>(t).IsHonor(); }
            public static bool     IsRedDora(this TileType t)         { return EnumAttributes.GetAttributeValue<IsRedDora, bool>(t); }
            public static Suit     GetSuit(this TileType t)           { return EnumAttributes.GetAttributeValue<TileSuit, Suit>(t); }
            public static int      GetValue(this TileType t)          { return EnumAttributes.GetAttributeValue<TileValue, int>(t); }
            public static int      GetSkyValue(this TileType t)       { return EnumAttributes.GetAttributeValue<SkyValue, int>(t); }
            public static string   GetText(this TileType t)           { return EnumAttributes.GetAttributeValue<TextValue, string>(t); }
            public static int      GetOrder(this TileType t)          { return EnumAttributes.GetAttributeValue<TileOrder, int>(t); }
            public static string   GetHexString(this TileType t)      { return string.Format("{0:X2}", t.GetSkyValue()); }
            public static bool     IsTerminal(this TileType t)        { return EnumAttributes.GetAttributeValue<IsTerminal, bool>(t); }

            public static bool IsEqual(this TileType a, TileType b, bool matchRed = false)
            {
                return (a == b) ||
                       (!matchRed &&
                        (EnumAttributes.GetAttributeValue<TileValue, int>(a) == EnumAttributes.GetAttributeValue<TileValue, int>(b)) &&
                        (EnumAttributes.GetAttributeValue<TileSuit, Suit>(a) == EnumAttributes.GetAttributeValue<TileSuit, Suit>(b)));
            }

            public static bool IsNext(this TileType a, TileType b)
            {
                return (EnumAttributes.GetAttributeValue<TileSuit, Suit>(a) == EnumAttributes.GetAttributeValue<TileSuit, Suit>(b)) &&
                       ((EnumAttributes.GetAttributeValue<TileValue, int>(a) + 1) == EnumAttributes.GetAttributeValue<TileValue, int>(b));
            }

            public static TileType GetNonRedDoraVersion(this TileType t)
            {
                return (t == TileType.Bamboo5Red)     ? TileType.Bamboo5 :
                       (t == TileType.Characters5Red) ? TileType.Characters5 :
                       (t == TileType.Circles5Red)    ? TileType.Circles5 :
                                                        t;
            }

            public static TileType GetRedDoraVersion(this TileType t)
            {
                return (t == TileType.Bamboo5)     ? TileType.Bamboo5Red :
                       (t == TileType.Characters5) ? TileType.Characters5Red :
                       (t == TileType.Circles5)    ? TileType.Circles5Red :
                                                     t;
            }

            public static TileType GetTile(string hexString)
            {
                TileType tt = TileType.None;
                if ((hexString != null) && (hexString.Length == 2))
                {
                    if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out int skyValue))
                    {
                        tt = GetTile(skyValue);
                    }
                }
                return tt;
            }

            public static TileType GetTile(int skyValue)
            {
                TileType t = TileType.None;
                foreach (TileType tt in Enum.GetValues(typeof(TileType)))
                {
                    if (skyValue == tt.GetSkyValue())
                    {
                        t = tt;
                        break;
                    }
                }
                return t;
            }

            public static TileType GetRoundWindTile(Round r)
            {
                return ((r == Round.East1)  || (r == Round.East2)  || (r == Round.East3)  || (r == Round.East4))  ? TileType.East :
                       ((r == Round.South1) || (r == Round.South2) || (r == Round.South3) || (r == Round.South4)) ? TileType.South :
                       ((r == Round.West1)  || (r == Round.West2)  || (r == Round.West3)  || (r == Round.West4))  ? TileType.West :
                                                                                                                    TileType.North;
            }

            public static TileType GetSeatWindTile(Player current, Player dealer)
            {
                Global.Assert(current.IsPlayer());
                Global.Assert(dealer.IsPlayer());
                return (current == dealer)           ? TileType.East :
                       (current == dealer.GetNext()) ? TileType.South :
                       (current.GetNext() == dealer) ? TileType.North :
                                                       TileType.West;
            }

            public static TileType GetNextSeatWindTile(TileType seat)
            {
                Global.Assert(seat.IsWind());
                return (seat == TileType.East)  ? TileType.South :
                       (seat == TileType.South) ? TileType.West :
                       (seat == TileType.West)  ? TileType.North :
                                                  TileType.East;
            }

            public static TileType GetPrevSeatWindTile(TileType seat)
            {
                Global.Assert(seat.IsWind());
                return (seat == TileType.East)  ? TileType.North :
                       (seat == TileType.North) ? TileType.West :
                       (seat == TileType.West)  ? TileType.South :
                                                  TileType.East;
            }

            public static TileType GetDoraTile(this TileType t)
            {
                return (t == TileType.Bamboo9)     ? TileType.Bamboo1 :
                       (t == TileType.Characters9) ? TileType.Characters1 :
                       (t == TileType.Circles9)    ? TileType.Circles1 :
                       (t == TileType.North)       ? TileType.East :
                       (t == TileType.East)        ? TileType.South :
                       (t == TileType.South)       ? TileType.West :
                       (t == TileType.West)        ? TileType.North :
                       (t == TileType.Chun)        ? TileType.Haku :
                       (t == TileType.Hatsu)       ? TileType.Chun :
                       (t == TileType.Haku)        ? TileType.Hatsu :
                       t.IsTile()                  ? t.GetNext() :
                                                     TileType.None;
            }

            public static void GetSummary(this TileType t, StringBuilder sb, TileType? prev)
            {
                if (t.IsHonor())
                {
                    sb.Append((t == TileType.North) ? "n" :
                              (t == TileType.East)  ? "e" :
                              (t == TileType.South) ? "s" :
                              (t == TileType.West)  ? "w" :
                              (t == TileType.Chun)  ? "c" :
                              (t == TileType.Haku)  ? "h" :
                                                      "g");
                }
                else
                {
                    Suit suit = t.GetSuit();
                    if (suit != ((prev != null) ? prev.Value.GetSuit() : Suit.None))
                    {
                        sb.Append((suit == Suit.Bamboo)     ? "b" :
                                  (suit == Suit.Characters) ? "m" :
                                                              "p");
                    }

                    sb.Append(t.GetValue());
                    if (t.IsRedDora())
                    {
                        sb.Append("r");
                    }
                }
            }
        }
    #endregion

    public enum Location
    {
        None,
        Wall,
        Hand,
        Discard,
        Call
    }

    public interface ITile : ICloneable
    {
        TileType  Type        { get; }
        Location  Location    { get; }
        Player    Ancillary   { get; } // Ex: For discarded tiles, point to player who called. For called meld && Called == true, player who sourced tile
        ReachType Reach       { get; } // Ex: Set to not-None if this tile is from a discard pile and is reached/open reached/etc.
        bool      Ghost       { get; } // Ex: True if a called tile from a discard pile or a drawn tile from a wall.
        bool      Called      { get; } // Ex: Called tile either from a discard pile or the called tile from a meld.
        bool      WinningTile { get; } // Ex: Tile in a candidate hand that is the winning tile Tsumo or Ron is called upon.
        int       Slot        { get; } // Ex: Slot index from source hand or discard pile. Could also refer to wall index. When part of a meld, may ref slot from hand.
    }

    public static class TileFactory
    {
        public static ITile BuildTile(TileType type, int slot = 0, bool called = false) { return new TileImpl(type) { Slot = slot, Called = called }; }
        public static ITile BuildTile(string value)                                     { return TileImpl.GetTile(value); }
    }
}
