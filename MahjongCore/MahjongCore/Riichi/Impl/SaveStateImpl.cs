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
            CommonHelpers.Check(PREFIX_V2.Equals(lines.Dequeue()), "");

            // Get data from the save string.
            state.State               = PlayStateExtentionMethods.GetPlayState(lines.Dequeue());
            lines.Dequeue();          // TileColor
            state.Round               = RoundExtensionMethods.GetRound(lines.Dequeue());
            state.Lapped              = bool.Parse(lines.Dequeue());
            state.Bonus               = int.Parse(lines.Dequeue());
            state.Pool                = int.Parse(lines.Dequeue());
            state.FirstDealer         = PlayerExtensionMethods.GetPlayer(lines.Dequeue());
            state.Current             = PlayerExtensionMethods.GetPlayer(lines.Dequeue());
            state.PlayerRecentOpenKan = PlayerExtensionMethods.GetPlayer(lines.Dequeue());
            state.Roll                = int.Parse(lines.Dequeue());
            state.PlayerDeadWallPick  = bool.Parse(lines.Dequeue());
            lines.Dequeue();          // GameTypeFlags

            int pickedTileCount = int.Parse(lines.Dequeue());
            state.PrevAction = GameActionExtentionMethods.GetGameAction(lines.Dequeue());
            state.NextAction = GameActionExtentionMethods.GetGameAction(lines.Dequeue());

            string tileWallString = lines.Dequeue();
            CommonHelpers.Check(tileWallString.Length == (2 * 136), "Tile wall expected to be 2 * 136 characters long. Got: " + tileWallString.Length);
            for (int i = 0; i < 136; ++i)
            {
                state.WallRaw[i].Type = TileTypeExtensionMethods.GetTile(tileWallString.Substring((i * 2), 2));
                state.WallRaw[i].Ancillary = Player.None;
                state.WallRaw[i].Ghost = false;
            }

            foreach (Player p in PlayerExtensionMethods.Players)
            {
                HandImpl hand = state.GetHand(p);
                hand.Reset();

                // Get the name and score.
                string name = lines.Dequeue();
                int nameTokenCount;
                if (int.TryParse(name, out nameTokenCount) && (lines.Count > nameTokenCount))
                {
                    if (MULTI_STRING_NAME_END.Equals(lines.ElementAt(nameTokenCount)))
                    {
                        name = lines.Dequeue();
                        for (int i = 0; i < (nameTokenCount - 1); ++i)
                        {
                            name += " " + lines.Dequeue();
                        }

                        string nameEndToken = lines.Dequeue();
                        CommonHelpers.Check(MULTI_STRING_NAME_END.Equals(nameEndToken), "Last string of multi string didn't match end name token: " + nameEndToken);
                    }
                }

                hand.Score = int.Parse(lines.Dequeue());
                hand.Streak = int.Parse(lines.Dequeue());
                hand.Yakitori = bool.Parse(lines.Dequeue());

                // Get the active hand.
                string activeHandString = lines.Dequeue();
                CommonHelpers.Check(activeHandString.Length == (2 * 13), "Expecting 13 tiles, 2 characters per tile... found: " + activeHandString.Length);
                for (int i = 0; i < 13; ++i)
                {
                    hand.ActiveHandRaw[i].Type = TileTypeExtensionMethods.GetTile(activeHandString.Substring((i * 2), 2));
                }

                // Get open melds.
                int openMeldCount = int.Parse(lines.Dequeue());
                for (int i = 0; i < openMeldCount; ++i)
                {
                    hand.MeldsRaw[i].Set(MeldStateExtensionMethods.TryGetMeldState(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()));
                }

                // Get discards.
                int discardCount = int.Parse(lines.Dequeue());
                hand.DiscardsImpl.Clear();
                for (int i = 0; i < discardCount; ++i)
                {
                    hand.DiscardsImpl.Add(TileImpl.GetTile(lines.Dequeue()));
                }
            }

            // Get custom rules.
            state.Settings.Reset();
            bool hasCustomRules = bool.Parse(lines.Dequeue());
            GameSettingsImpl settings = state.Settings as GameSettingsImpl;

            if (hasCustomRules && (settings != null))
            {
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameRules1);
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameRules2);
                settings.SetSettingField(uint.Parse(lines.Dequeue()),  CustomBitfields.CustomGameYaku1);
                settings.SetSettingField(uint.Parse(lines.Dequeue()),  CustomBitfields.CustomGameYaku2);
                settings.SetSettingField(uint.Parse(lines.Dequeue()),  CustomBitfields.CustomGameYaku3);
                settings.SetSetting(GameOption.VictoryPoints, int.Parse(lines.Dequeue()));
                settings.SetSetting(GameOption.UmaOption, UmaExtensionMethods.GetUma(lines.Dequeue()));
                settings.SetSetting(GameOption.RedDoraOption, RedDoraExtensionMethods.GetRedDora(lines.Dequeue()));
                settings.SetSetting(GameOption.OkaOption, OkaExtensionMethods.GetOka(lines.Dequeue()));
                settings.SetSetting(GameOption.IisouSanjunHanOption, IisouSanjunHanExtensionMethods.GetIisouSanjunHan(lines.Dequeue()));
                settings.SetSetting(GameOption.YakitoriOption, YakitoriExtensionMethods.GetYakitori(lines.Dequeue()));
            }

            // Extrapolate other data.
            state.Dealer = state.FirstDealer.AddOffset(state.Round.GetOffset());
            state.Wareme = state.Settings.GetSetting<bool>(GameOption.Wareme) ? state.Dealer.AddOffset(state.Roll - 1) : Player.None;
            state.Offset = GameStateHelpers.GetOffset(state.Dealer, state.Roll);
            state.TilesRemaining = 122 - (13 * 4) - pickedTileCount;
            state.FlipDoraAfterNextDiscard = false;
            state.DoraCount = 1 + (state.Player1Hand.KanCount + state.Player2Hand.KanCount + state.Player3Hand.KanCount + state.Player4Hand.KanCount) - (state.FlipDoraAfterNextDiscard ? 1 : 0);

            if (state.PrevAction == GameAction.ReplacementTilePick)
            {
                HandImpl currentHand = state.GetHand(state.Current);
                if (currentHand.MeldCount > 0)
                {
                    MeldState ms = currentHand.GetLatestMeld().State;
                    state.FlipDoraAfterNextDiscard = (ms == MeldState.KanOpen) || (ms == MeldState.KanPromoted);
                }
            }

            foreach (Player player in PlayerExtensionMethods.Players)
            {
                HandImpl hand = state.GetHand(player);
                hand.CouldIppatsu = DetermineIppatsu(state, player, pickedTileCount);
                hand.Furiten = DetermineFuriten(hand);
            }
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
                CommonHelpers.Check(!flags.Contains(' '));
                CommonHelpers.Check(!flags.Contains('\n'));
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

        private bool DetermineIppatsu(GameStateImpl state, Player player, int tilesPicked)
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

        private bool DetermineFuriten(HandImpl hand)
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
