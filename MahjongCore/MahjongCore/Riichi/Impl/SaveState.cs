// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using MahjongCore.Common;
using System;

namespace MahjongCore.Riichi
{
    public static class StringBuilderExtensionMethods
    {
        public static void AppendWithSpace(this StringBuilder sb, string str)
        {
            sb.Append(str);
            sb.Append(' ');
        }
    }

    public class SaveState : IComparable<SaveState>
    {
        public class PlayerValue : IComparable<PlayerValue>
        {
            public string             Name;
            public int                Score = 0;
            public int                ConsecutiveWinStreak = 0;
            public bool               Ippatsu = false;
            public bool               Furiten = false;
            public bool               Yakitori = false;
            public List<ExtendedTile> Discards = new List<ExtendedTile>();
            public TileType[]         ActiveHand = new TileType[TileHelpers.HAND_SIZE];
            public List<Meld>         Melds = new List<Meld>();
            public List<TileCommand>  DrawsAndKans = new List<TileCommand>();

            public int CountKans()
            {
                int kanCount = 0;
                foreach (Meld m in Melds)
                {
                    if (m.State.GetMeldType() == MeldType.Kan)
                    {
                        kanCount++;
                    }
                }
                return kanCount;
            }

            public int GetActiveTileCount()
            {
                int activeTileCount = TileHelpers.HAND_SIZE;
                foreach (TileType tt in ActiveHand)
                {
                    if (tt == TileType.None)
                    {
                        --activeTileCount;
                    }
                }
                return activeTileCount;
            }

            public PlayerValue Clone()
            {
                PlayerValue pv = new PlayerValue();
                pv.Name = Name;
                pv.Score = Score;
                pv.ConsecutiveWinStreak = ConsecutiveWinStreak;
                pv.Ippatsu = Ippatsu;
                pv.Furiten = Furiten;
                pv.Yakitori = Yakitori;

                CommonHelpers.SafeCopyIntoList(pv.Discards, Discards);
                CommonHelpers.SafeCopyIntoList(pv.Melds, Melds);
                CommonHelpers.SafeCopyIntoList(pv.DrawsAndKans, DrawsAndKans);

                for (int i = 0; i < ActiveHand.Length; ++i)
                {
                    pv.ActiveHand[i] = ActiveHand[i];
                }
                return pv;
            }

            // IComparable
            public int CompareTo(PlayerValue other)
            {
                throw new NotImplementedException();
            }
        }

        public PlayerValue[] Players           = new PlayerValue[] { new PlayerValue(), new PlayerValue(), new PlayerValue(), new PlayerValue() };
        public TileType[]    Wall              = new TileType[TileHelpers.TOTAL_TILE_COUNT];
        public List<Player>  DiscardPlayerList = new List<Player>();
        public TileColor     TileColor;
        public Round         CurrentRound;
        public bool          CurrentRoundLapped;
        public int           Bonus;
        public int           Pool;
        public int           Roll;
        public int           Offset;
        public int           TilesRemaining;
        public int           DoraCount;
        public Player        CurrentDealer;
        public Player        CurrentPlayer;
        public Player        PlayerRecentOpenKan;
        public Player        WaremePlayer;
        public bool          PlayerDeadWallPick;
        public bool          FlipDoraAfterNextDiscard;
        public string        GameTypeFlags;
        public GameAction    PrevAction;
        public GameAction    NextAction;
        public GameSettings  CustomSettings;
        public PlayState     CurrentState;

        private static string PREFIX_V2 = "v2";
        private static string MULTI_STRING_NAME_END = "multistringnameend";
        private static string NEUTRAL_FLAGS_STR = "neutral";

        private SaveState() { }

        public SaveState Clone()
        {
            SaveState s = new SaveState();
            s.TileColor = TileColor;
            s.CurrentRound = CurrentRound;
            s.CurrentRoundLapped = CurrentRoundLapped;
            s.Bonus = Bonus;
            s.Pool = Pool;
            s.Roll = Roll;
            s.Offset = Offset;
            s.TilesRemaining = TilesRemaining;
            s.DoraCount = DoraCount;
            s.CurrentDealer = CurrentDealer;
            s.CurrentPlayer = CurrentPlayer;
            s.PlayerRecentOpenKan = PlayerRecentOpenKan;
            s.WaremePlayer = WaremePlayer;
            s.PlayerDeadWallPick = PlayerDeadWallPick;
            s.FlipDoraAfterNextDiscard = FlipDoraAfterNextDiscard;
            s.GameTypeFlags = GameTypeFlags;
            s.PrevAction = PrevAction;
            s.NextAction = NextAction;
            s.CustomSettings = CustomSettings;
            s.CurrentState = CurrentState;
            CommonHelpers.SafeCopyIntoValueList(s.DiscardPlayerList, DiscardPlayerList);

            for (int i = 0; i < Wall.Length; ++i)
            {
                s.Wall[i] = Wall[i];
            }

            for (int i = 0; i < Players.Length; ++i)
            {
                s.Players[i] = Players[i].Clone();
            }
            return s;
        }

        public static SaveState LoadFromString(string save)
        {
            RiichiGlobal.Assert((save != null) && (save.Length > 0));
            SaveState state = new SaveState();

            if (save.StartsWith(PREFIX_V2)) { state.LoadFromStringV2(save); }
            else                            { state.LoadFromStringV1(save); }
            return state;
        }

        private void LoadFromStringV2(string save)
        {
            Queue<string> lines = new Queue<string>(save.Split(null)); // Passing in null splits at any whitespace.
            RiichiGlobal.Assert(PREFIX_V2.Equals(lines.Dequeue()));

            // Get data from the save string.
            RiichiGlobal.Assert(PlayStateExtentionMethods.TryGetPlayState(lines.Dequeue(), out CurrentState));
            RiichiGlobal.Assert(ColorExtensionMethods.TryGetColor(lines.Dequeue(), out TileColor));
            RiichiGlobal.Assert(RoundExtensionMethods.TryGetRound(lines.Dequeue(), out CurrentRound));
            RiichiGlobal.Assert(bool.TryParse(lines.Dequeue(), out CurrentRoundLapped));
            RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out Bonus));
            RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out Pool));

            Player startingDealer;
            RiichiGlobal.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out startingDealer));
            RiichiGlobal.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out CurrentPlayer));
            RiichiGlobal.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out PlayerRecentOpenKan));
            RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out Roll));
            RiichiGlobal.Assert(bool.TryParse(lines.Dequeue(), out PlayerDeadWallPick));
            GameTypeFlags = lines.Dequeue();

            int pickedTileCount;
            RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out pickedTileCount));
            RiichiGlobal.Assert(GameActionExtentionMethods.TryGetGameAction(lines.Dequeue(), out PrevAction));
            RiichiGlobal.Assert(GameActionExtentionMethods.TryGetGameAction(lines.Dequeue(), out NextAction));

            string tileWallString = lines.Dequeue();
            RiichiGlobal.Assert(tileWallString.Length == (2 * 136)); // 136 tiles, 2-bytes per character.
            for (int i = 0; i < 136; ++i)
            {
                Wall[i] = TileTypeExtensionMethods.GetTile(tileWallString.Substring((i * 2), 2));
            }

            foreach (PlayerValue p in Players)
            {
                // Get the name and score.
                p.Name = lines.Dequeue();

                int nameTokenCount;
                if (int.TryParse(p.Name, out nameTokenCount) && (lines.Count > nameTokenCount))
                {
                    if (MULTI_STRING_NAME_END.Equals(lines.ElementAt(nameTokenCount)))
                    {
                        p.Name = lines.Dequeue();
                        for (int i = 0; i < (nameTokenCount - 1); ++i)
                        {
                            p.Name += " " + lines.Dequeue();
                        }

                        string nameEndToken = lines.Dequeue();
                        RiichiGlobal.Assert(MULTI_STRING_NAME_END.Equals(nameEndToken));
                    }
                }

                RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out p.Score));
                RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out p.ConsecutiveWinStreak));
                RiichiGlobal.Assert(bool.TryParse(lines.Dequeue(), out p.Yakitori));

                // Get the active hand.
                string activeHandString = lines.Dequeue();
                RiichiGlobal.Assert(activeHandString.Length == (2 * 13)); // 13 tiles, 2-bytes per character.
                for (int i = 0; i < 13; ++i)
                {
                    p.ActiveHand[i] = TileTypeExtensionMethods.GetTile(activeHandString.Substring((i * 2), 2));
                }

                // Get open melds.
                int openMeldCount;
                RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out openMeldCount));
                for (int i = 0; i < openMeldCount; ++i)
                {
                    MeldState meldState;
                    ExtendedTile et1, et2, et3, et4;
                    RiichiGlobal.Assert(MeldStateExtensionMethods.TryGetMeldState(lines.Dequeue(), out meldState));
                    RiichiGlobal.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et1));
                    RiichiGlobal.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et2));
                    RiichiGlobal.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et3));
                    RiichiGlobal.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et4));
                    p.Melds.Add(new Meld(meldState, et1, et2, et3, et4));
                }

                // Get discards.
                int discardCount;
                RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out discardCount));
                for (int i = 0; i < discardCount; ++i)
                {
                    ExtendedTile et;
                    RiichiGlobal.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et));
                    p.Discards.Add(et);
                }
            }

            // Get custom rules.
            CustomSettings = new GameSettings();
            bool hasCustomRules;
            RiichiGlobal.Assert(bool.TryParse(lines.Dequeue(), out hasCustomRules));

            if (hasCustomRules)
            {
                uint rules1;        RiichiGlobal.Assert(uint.TryParse(lines.Dequeue(), out rules1));                                    CustomSettings.SetSettingField(rules1, CustomBitfields.CustomGameRules1);
                uint rules2;        RiichiGlobal.Assert(uint.TryParse(lines.Dequeue(), out rules2));                                    CustomSettings.SetSettingField(rules2, CustomBitfields.CustomGameRules2);
                uint yaku1;         RiichiGlobal.Assert(uint.TryParse(lines.Dequeue(), out yaku1));                                     CustomSettings.SetSettingField(yaku1,  CustomBitfields.CustomGameYaku1);
                uint yaku2;         RiichiGlobal.Assert(uint.TryParse(lines.Dequeue(), out yaku2));                                     CustomSettings.SetSettingField(yaku2,  CustomBitfields.CustomGameYaku2);
                uint yaku3;         RiichiGlobal.Assert(uint.TryParse(lines.Dequeue(), out yaku3));                                     CustomSettings.SetSettingField(yaku3,  CustomBitfields.CustomGameYaku3);
                int victoryScore;   RiichiGlobal.Assert(int.TryParse(lines.Dequeue(), out victoryScore));                               CustomSettings.SetSetting(GameOption.VictoryPoints, victoryScore);
                Uma uma;            RiichiGlobal.Assert(UmaExtensionMethods.TryGetUma(lines.Dequeue(), out uma));                       CustomSettings.SetSetting(GameOption.UmaOption, uma);
                RedDora redDora;    RiichiGlobal.Assert(RedDoraExtensionMethods.TryGetRedDora(lines.Dequeue(), out redDora));           CustomSettings.SetSetting(GameOption.RedDoraOption, redDora);
                Oka oka;            RiichiGlobal.Assert(OkaExtensionMethods.TryGetOka(lines.Dequeue(), out oka));                       CustomSettings.SetSetting(GameOption.OkaOption, oka);
                IisouSanjunHan ish; RiichiGlobal.Assert(IisouSanjunHanExtensionMethods.TryGetIisouSanjunHan(lines.Dequeue(), out ish)); CustomSettings.SetSetting(GameOption.IisouSanjunHanOption, ish);
                Yakitori yakitori;  RiichiGlobal.Assert(YakitoriExtensionMethods.TryGetYakitori(lines.Dequeue(), out yakitori));        CustomSettings.SetSetting(GameOption.YakitoriOption, yakitori);
            }

            // Extrapolate other data.
            CurrentDealer = startingDealer.AddOffset(CurrentRound.GetOffset());
            WaremePlayer = CustomSettings.GetSetting<bool>(GameOption.Wareme) ? CurrentDealer.AddOffset(Roll - 1) : Player.None;
            Offset = GameStateHelpers.GetOffset(CurrentDealer, Roll);
            TilesRemaining = 122 - (13 * 4) - pickedTileCount;

            FlipDoraAfterNextDiscard = false;
            if (PrevAction == GameAction.ReplacementTilePick)
            {
                PlayerValue currPlayerHand = Players[CurrentPlayer.GetZeroIndex()];
                if (currPlayerHand.Melds.Count > 0)
                {
                    MeldState ms = currPlayerHand.Melds[currPlayerHand.Melds.Count - 1].State;
                    FlipDoraAfterNextDiscard = (ms == MeldState.KanOpen) || (ms == MeldState.KanPromoted);
                }
            }

            int kanCount = Players[0].CountKans() + Players[1].CountKans() + Players[2].CountKans() + Players[3].CountKans();
            DoraCount = 1 + kanCount - (FlipDoraAfterNextDiscard ? 1 : 0);
            Players[0].Ippatsu = DetermineIppatsu(Player.Player1, CurrentDealer, pickedTileCount, Players);
            Players[1].Ippatsu = DetermineIppatsu(Player.Player2, CurrentDealer, pickedTileCount, Players);
            Players[2].Ippatsu = DetermineIppatsu(Player.Player3, CurrentDealer, pickedTileCount, Players);
            Players[3].Ippatsu = DetermineIppatsu(Player.Player4, CurrentDealer, pickedTileCount, Players);

            // Read in furiten flags.
            Players[0].Furiten = DetermineFuriten(Players[0].ActiveHand, Players[0].Discards);
            Players[1].Furiten = DetermineFuriten(Players[1].ActiveHand, Players[1].Discards);
            Players[2].Furiten = DetermineFuriten(Players[2].ActiveHand, Players[2].Discards);
            Players[3].Furiten = DetermineFuriten(Players[3].ActiveHand, Players[3].Discards);
        }

        private void LoadFromStringV1(string state)
        {
            RiichiGlobal.Assert(false, "V1 String Not Supported.");
        }

        private void LoadFromState(GameState state)
        {
            TileColor = state.TileColor;
            CurrentRound = state.CurrentRound;
            CurrentRoundLapped = state.CurrentRoundLapped;
            Bonus = state.Bonus;
            Pool = state.Pool;
            Roll = state.Roll;
            Offset = state.Offset;
            TilesRemaining = state.TilesRemaining;
            DoraCount = state.DoraCount;
            CurrentDealer = state.CurrentDealer;
            CurrentPlayer = state.CurrentPlayer;
            PlayerRecentOpenKan = state.PlayerRecentOpenKan;
            WaremePlayer = state.WaremePlayer;
            PlayerDeadWallPick = state.PlayerDeadWallPick;
            FlipDoraAfterNextDiscard = state.FlipDoraAfterNextDiscard;
            GameTypeFlags = NEUTRAL_FLAGS_STR;
            PrevAction = state.PrevAction;
            NextAction = state.NextAction;
            CustomSettings = state.Settings;
            CurrentState = state.CurrentState;

            // Copy over the wall.
            for (int i = 0; i < 136; ++i)
            {
                Wall[i] = state.Wall[i];
            }

            // Copy over per-player data.
            for (int iPlayer = 0; iPlayer < 4; ++iPlayer)
            {
                PlayerValue p = Players[iPlayer];
                Hand hand = state.GetHandZeroIndexed(iPlayer);

                p.Name = "Player " + (iPlayer + 1);
                p.Score = hand.Score;
                p.ConsecutiveWinStreak = hand.Streak;
                p.Ippatsu = hand.IsIppatsu();
                p.Furiten = hand.IsFuriten();
                p.Yakitori = hand.Yakitori;
                p.Discards = state.GetDiscardsZeroIndexed(iPlayer);

                for (int iActiveHandTile = 0; iActiveHandTile < 14; ++iActiveHandTile)
                {
                    p.ActiveHand[iActiveHandTile] = hand.ActiveHand[iActiveHandTile];
                }

                p.Melds.Clear();
                for (int iMeldCount = 0; iMeldCount < hand.GetCalledMeldCount(); ++iMeldCount)
                {
                    p.Melds.Add(hand.OpenMeld[iMeldCount]);
                }

                p.DrawsAndKans.Clear();
                StringBuilder sb = new StringBuilder();
                sb.Append("SAVING dnk, p: " + hand.Player + " drawsnkans size: " + hand.DrawsAndKans.Count + " drawsnaksn in order: ");
                for (int iDrawAndKanSlot = hand.DrawsAndKans.Count - 1; iDrawAndKanSlot >= 0; --iDrawAndKanSlot)
                {
                    TileCommand tc = hand.DrawsAndKans.ElementAt(iDrawAndKanSlot);
                    p.DrawsAndKans.Add(tc);
                    sb.Append(tc.TilePrimary.Tile + ", ");
                }
                RiichiGlobal.Log(sb.ToString());
            }

            // Copy over DiscardPlayerList.
            DiscardPlayerList.Clear();
            for (int iDiscardPlayerSlot = state.DiscardPlayerList.Count - 1; iDiscardPlayerSlot >= 0; --iDiscardPlayerSlot)
            {
                DiscardPlayerList.Add(state.DiscardPlayerList.ElementAt(iDiscardPlayerSlot));
            }
        }

        public static SaveState GetState(GameState state)
        {
            SaveState rs = new SaveState();
            rs.LoadFromState(state);
            return rs;
        }

        public string SaveToString()
        {
            return SaveToStringV2();
        }

        private string SaveToStringV2()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendWithSpace(PREFIX_V2);

            // Save individual values.
            sb.AppendWithSpace(CurrentState.GetSkyValue().ToString());
            sb.AppendWithSpace(TileColor.GetTextValue());
            sb.AppendWithSpace(CurrentRound.GetTextValue());
            sb.AppendWithSpace(CurrentRoundLapped.ToString());
            sb.AppendWithSpace(Bonus.ToString());
            sb.AppendWithSpace(Pool.ToString());
            sb.AppendWithSpace(CurrentDealer.AddOffset(-CurrentRound.GetOffset()).GetPlayerValue().ToString()); // Starting dealer
            sb.AppendWithSpace(CurrentPlayer.GetPlayerValue().ToString());
            sb.AppendWithSpace(PlayerRecentOpenKan.GetPlayerValue().ToString());
            sb.AppendWithSpace(Roll.ToString());
            sb.AppendWithSpace(PlayerDeadWallPick.ToString());

            string flags = GameTypeFlags;
            if ((flags == null) || (flags.Trim().Length == 0))
            {
                flags = NEUTRAL_FLAGS_STR;
            }
            else
            {
                RiichiGlobal.Assert(!flags.Contains(' '));
                RiichiGlobal.Assert(!flags.Contains('\n'));
            }
            sb.AppendWithSpace(flags);

            int pickedTileCount = 122 - (13 * 4) - TilesRemaining;
            sb.AppendWithSpace(pickedTileCount.ToString());
            sb.AppendWithSpace(PrevAction.GetSkyValue().ToString());
            sb.AppendWithSpace(NextAction.GetSkyValue().ToString());

            // Save wall.
            for (int i = 0; i < 136; ++i)
            {
                sb.Append(Wall[i].GetHexString());
            }
            sb.Append(' ');

            // Save player values.
            for (int iPlayer = 0; iPlayer < 4; ++iPlayer)
            {
                PlayerValue p = Players[iPlayer];

                // Save the name. If there are multiple tokens, first output the number, the each token, then the end token.
                string name = p.Name;
                if ((name == null) || (name.Trim().Length == 0))
                {
                    name = "Player " + (iPlayer + 1);
                }

                string[] nameTokens = p.Name.Trim().Split(null);
                if (nameTokens.Length == 1)
                {
                    sb.AppendWithSpace(nameTokens[0]);
                }
                else
                {
                    sb.AppendWithSpace(nameTokens.Length.ToString());
                    foreach (string nameToken in nameTokens)
                    {
                        sb.AppendWithSpace(nameToken);
                    }
                    sb.AppendWithSpace(MULTI_STRING_NAME_END);
                }

                // Save some values.
                sb.AppendWithSpace(p.Score.ToString());
                sb.AppendWithSpace(p.ConsecutiveWinStreak.ToString());
                sb.AppendWithSpace(p.Yakitori.ToString());

                // Save the active hand.
                for (int i = 0; i < 13; ++i)
                {
                    sb.Append(p.ActiveHand[i].GetHexString());
                }
                sb.Append(" ");

                // Save the melds.
                sb.AppendWithSpace(p.Melds.Count.ToString());
                foreach (Meld m in p.Melds)
                {
                    sb.AppendWithSpace(m.State.GetMeldCode().ToString());
                    sb.AppendWithSpace(m.Tiles[0].GetHexString());
                    sb.AppendWithSpace(m.Tiles[1].GetHexString());
                    sb.AppendWithSpace(m.Tiles[2].GetHexString());
                    sb.AppendWithSpace(m.Tiles[3].GetHexString());
                }

                // Get discards.
                sb.AppendWithSpace(p.Discards.Count.ToString());
                foreach (ExtendedTile et in p.Discards)
                {
                    sb.AppendWithSpace(et.GetHexString());
                }
            }

            // Get custom rules.
            bool hasCustomRules = CustomSettings.HasCustomSettings();
            sb.AppendWithSpace(hasCustomRules.ToString());
            if (hasCustomRules)
            {
                sb.AppendWithSpace(CustomSettings.GetSettingField(CustomBitfields.CustomGameRules1).ToString());
                sb.AppendWithSpace(CustomSettings.GetSettingField(CustomBitfields.CustomGameRules2).ToString());
                sb.AppendWithSpace(CustomSettings.GetSettingField(CustomBitfields.CustomGameYaku1).ToString());
                sb.AppendWithSpace(CustomSettings.GetSettingField(CustomBitfields.CustomGameYaku2).ToString());
                sb.AppendWithSpace(CustomSettings.GetSettingField(CustomBitfields.CustomGameYaku3).ToString());

                sb.AppendWithSpace(CustomSettings.GetSetting<int>(GameOption.VictoryPoints).ToString());
                sb.AppendWithSpace(CustomSettings.GetSetting<Uma>(GameOption.UmaOption).GetTextValue());
                sb.AppendWithSpace(CustomSettings.GetSetting<RedDora>(GameOption.RedDoraOption).GetTextValue());
                sb.AppendWithSpace(CustomSettings.GetSetting<Oka>(GameOption.OkaOption).GetTextValue());
                sb.AppendWithSpace(CustomSettings.GetSetting<IisouSanjunHan>(GameOption.IisouSanjunHanOption).GetTextValue());
                sb.AppendWithSpace(CustomSettings.GetSetting<Yakitori>(GameOption.YakitoriOption).GetTextValue());
            }

            // Return the final state string.
            return sb.ToString().Trim();
        }

        private bool DetermineIppatsu(Player p, Player dealer, int tilesPicked, PlayerValue[] players)
        {
            int [] slots = new int[] { 0, 0, 0, 0 };
            bool ippatsu = false;

            Player currentPlayer = dealer;
            for (int iTile = 0; iTile < tilesPicked; ++iTile)
            {
                int currPlayerIndex = currentPlayer.GetZeroIndex();

                ExtendedTile et = players[currPlayerIndex].Discards[slots[currPlayerIndex]];
                slots[currPlayerIndex]++;

                if (currentPlayer == p)
                {
                    ippatsu = et.Reach || et.OpenReach;
                }

                if (et.Called)
                {
                    ippatsu = false;
                    currentPlayer = et.Caller;

                    // Roll back because the next discard wont count as a picked.
                    iTile--;
                }
                else
                {
                    currentPlayer = currentPlayer.GetNext();
                }
            }
            return ippatsu;
        }
        
        private bool DetermineFuriten(TileType[] activeHand, List<ExtendedTile> discards)
        {
            // Get the list of waits.
            bool anyCalls = (Players[0].Melds.Count + Players[1].Melds.Count + Players[2].Melds.Count + Players[3].Melds.Count) > 0;
            bool overrideNoReachDummy;
            List<TileType> waits = HandEvaluator.GetWaits(activeHand, null, discards, anyCalls, out overrideNoReachDummy);

            bool furiten = false;
            if ((waits != null) && (waits.Count > 0))
            {
                // If any of our discards match one of the waits, then we're furiten!
                foreach (ExtendedTile et in discards)
                {
                    foreach (TileType tv in waits)
                    {
                        if (tv == et.Tile)
                        {
                            furiten = true;
                            break;
                        }

                        if (furiten == true)
                        {
                            break;
                        }
                    }
                }

                // Check if we've passed up a wait since our last discard. Reset on our discard unless we've reached.
                if (!furiten)
                {
                    // TODO: this
                }
            }
            return furiten;
        }

        // IComparable
        public int CompareTo(SaveState obj)
        {
            throw new NotImplementedException();
        }
    }
}
