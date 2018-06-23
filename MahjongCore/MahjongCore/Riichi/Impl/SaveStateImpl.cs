// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Riichi.Evaluator;
using MahjongCore.Riichi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Impl
{
    public class SaveStateImpl : ISaveState
    {
        // ISaveState
        public IGameSettings               Settings       { get; internal set; }
        public IDictionary<string, string> Tags           { get { return _Tags; } }
        public Round                       Round          { get; internal set; }
        public bool                        Lapped         { get; internal set; }
        public int                         Player1Score   { get; internal set; }
        public int                         Player2Score   { get; internal set; }
        public int                         Player3Score   { get; internal set; }
        public int                         Player4Score   { get; internal set; }
        public int                         TilesRemaining { get; internal set; }

        public ISaveState Clone() { return new SaveStateImpl(_State); }
        public string Marshall()  { return _State; }

        // IComparable<ISaveState>
        public int CompareTo(ISaveState other)
        {
            var state = other as SaveStateImpl;
            return (state == null) ? (state.GetHashCode() - other.GetHashCode()) : _State.CompareTo(state._State);
        }

        // SaveStateImpl
        private string _State;
        private Dictionary<string, string> _Tags = new Dictionary<string, string>();

        private static string PREFIX_V2             = "v2";
        private static string MULTI_STRING_NAME_END = "multistringnameend";
        private static string NEUTRAL_FLAGS_STR     = "neutral";

        internal SaveStateImpl(string state)
        {
            CommonHelpers.Check(((state != null) && (state.Length > 0)), "Empty or null save string...");
            _State = state;
            InitializeCommon(PopulateState(null, _Tags));
        }

        internal SaveStateImpl(GameStateImpl state)
        {
            InitializeCommon(state);
            _State = SaveToString(state);
        }

        internal GameStateImpl PopulateState(GameStateImpl state, Dictionary<string, string> tags)
        {
            var stateImpl = (state != null) ? state : new GameStateImpl();
            if (_State.StartsWith(PREFIX_V2)) { LoadFromStringV2(stateImpl, tags); }
            else                              { throw new Exception("Unrecognized state string"); }
            return stateImpl;
        }

        private void InitializeCommon(GameStateImpl state)
        {
            Settings = state.Settings;
            Round = state.Round;
            Lapped = state.Lapped;
            Player1Score = state.Player1HandRaw.Score;
            Player2Score = state.Player2HandRaw.Score;
            Player3Score = state.Player3HandRaw.Score;
            Player4Score = state.Player4HandRaw.Score;
            TilesRemaining = state.TilesRemaining;
        }

        private void LoadFromStringV2(GameStateImpl state, Dictionary<string, string> tags)
        {
            Queue<string> lines = new Queue<string>(_State.Split(null)); // Passing in null splits at any whitespace.
            Global.Assert(PREFIX_V2.Equals(lines.Dequeue()));

            // Get data from the save string.
            Global.Assert(PlayStateExtentionMethods.TryGetPlayState(lines.Dequeue(), out CurrentState));
            Global.Assert(ColorExtensionMethods.TryGetColor(lines.Dequeue(), out TileColor));
            Global.Assert(RoundExtensionMethods.TryGetRound(lines.Dequeue(), out CurrentRound));
            Global.Assert(bool.TryParse(lines.Dequeue(), out CurrentRoundLapped));
            Global.Assert(int.TryParse(lines.Dequeue(), out Bonus));
            Global.Assert(int.TryParse(lines.Dequeue(), out Pool));

            Player startingDealer;
            Global.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out startingDealer));
            Global.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out CurrentPlayer));
            Global.Assert(PlayerExtensionMethods.TryGetPlayer(lines.Dequeue(), out PlayerRecentOpenKan));
            Global.Assert(int.TryParse(lines.Dequeue(), out Roll));
            Global.Assert(bool.TryParse(lines.Dequeue(), out PlayerDeadWallPick));
            GameTypeFlags = lines.Dequeue();

            int pickedTileCount;
            Global.Assert(int.TryParse(lines.Dequeue(), out pickedTileCount));
            Global.Assert(GameActionExtentionMethods.TryGetGameAction(lines.Dequeue(), out PrevAction));
            Global.Assert(GameActionExtentionMethods.TryGetGameAction(lines.Dequeue(), out NextAction));

            string tileWallString = lines.Dequeue();
            Global.Assert(tileWallString.Length == (2 * 136)); // 136 tiles, 2-bytes per character.
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
                        Global.Assert(MULTI_STRING_NAME_END.Equals(nameEndToken));
                    }
                }

                Global.Assert(int.TryParse(lines.Dequeue(), out p.Score));
                Global.Assert(int.TryParse(lines.Dequeue(), out p.ConsecutiveWinStreak));
                Global.Assert(bool.TryParse(lines.Dequeue(), out p.Yakitori));

                // Get the active hand.
                string activeHandString = lines.Dequeue();
                Global.Assert(activeHandString.Length == (2 * 13)); // 13 tiles, 2-bytes per character.
                for (int i = 0; i < 13; ++i)
                {
                    p.ActiveHand[i] = TileTypeExtensionMethods.GetTile(activeHandString.Substring((i * 2), 2));
                }

                // Get open melds.
                int openMeldCount;
                Global.Assert(int.TryParse(lines.Dequeue(), out openMeldCount));
                for (int i = 0; i < openMeldCount; ++i)
                {
                    MeldState meldState;
                    ExtendedTile et1, et2, et3, et4;
                    Global.Assert(MeldStateExtensionMethods.TryGetMeldState(lines.Dequeue(), out meldState));
                    Global.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et1));
                    Global.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et2));
                    Global.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et3));
                    Global.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et4));
                    p.Melds.Add(new Meld(meldState, et1, et2, et3, et4));
                }

                // Get discards.
                int discardCount;
                Global.Assert(int.TryParse(lines.Dequeue(), out discardCount));
                for (int i = 0; i < discardCount; ++i)
                {
                    ExtendedTile et;
                    Global.Assert(ExtendedTile.TryGetExtendedTile(lines.Dequeue(), out et));
                    p.Discards.Add(et);
                }
            }

            // Get custom rules.
            CustomSettings = new GameSettings();
            bool hasCustomRules;
            Global.Assert(bool.TryParse(lines.Dequeue(), out hasCustomRules));

            if (hasCustomRules)
            {
                uint rules1;        Global.Assert(uint.TryParse(lines.Dequeue(), out rules1));                                    CustomSettings.SetSettingField(rules1, CustomBitfields.CustomGameRules1);
                uint rules2;        Global.Assert(uint.TryParse(lines.Dequeue(), out rules2));                                    CustomSettings.SetSettingField(rules2, CustomBitfields.CustomGameRules2);
                uint yaku1;         Global.Assert(uint.TryParse(lines.Dequeue(), out yaku1));                                     CustomSettings.SetSettingField(yaku1,  CustomBitfields.CustomGameYaku1);
                uint yaku2;         Global.Assert(uint.TryParse(lines.Dequeue(), out yaku2));                                     CustomSettings.SetSettingField(yaku2,  CustomBitfields.CustomGameYaku2);
                uint yaku3;         Global.Assert(uint.TryParse(lines.Dequeue(), out yaku3));                                     CustomSettings.SetSettingField(yaku3,  CustomBitfields.CustomGameYaku3);
                int victoryScore;   Global.Assert(int.TryParse(lines.Dequeue(), out victoryScore));                               CustomSettings.SetSetting(GameOption.VictoryPoints, victoryScore);
                Uma uma;            Global.Assert(UmaExtensionMethods.TryGetUma(lines.Dequeue(), out uma));                       CustomSettings.SetSetting(GameOption.UmaOption, uma);
                RedDora redDora;    Global.Assert(RedDoraExtensionMethods.TryGetRedDora(lines.Dequeue(), out redDora));           CustomSettings.SetSetting(GameOption.RedDoraOption, redDora);
                Oka oka;            Global.Assert(OkaExtensionMethods.TryGetOka(lines.Dequeue(), out oka));                       CustomSettings.SetSetting(GameOption.OkaOption, oka);
                IisouSanjunHan ish; Global.Assert(IisouSanjunHanExtensionMethods.TryGetIisouSanjunHan(lines.Dequeue(), out ish)); CustomSettings.SetSetting(GameOption.IisouSanjunHanOption, ish);
                Yakitori yakitori;  Global.Assert(YakitoriExtensionMethods.TryGetYakitori(lines.Dequeue(), out yakitori));        CustomSettings.SetSetting(GameOption.YakitoriOption, yakitori);
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

        private string SaveToString(GameStateImpl state)
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
                Global.Assert(!flags.Contains(' '));
                Global.Assert(!flags.Contains('\n'));
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
    }
}
