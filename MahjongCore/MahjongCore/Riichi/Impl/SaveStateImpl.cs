// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
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
            _State = SaveToString(state, _Tags);
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

            state.Reset();

            // Read game state.
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
                for (int i = 0; i < discardCount; ++i)
                {
                    hand.DiscardsImpl.Add(TileImpl.GetTile(lines.Dequeue()));
                }
            }

            // Read settings.
            if (bool.Parse(lines.Dequeue()) && (state.Settings is GameSettingsImpl)) // Bool if there are custom game settings.
            {
                GameSettingsImpl settings = state.Settings as GameSettingsImpl;
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameRules1);
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameRules2);
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameYaku1);
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameYaku2);
                settings.SetSettingField(uint.Parse(lines.Dequeue()), CustomBitfields.CustomGameYaku3);
                settings.SetSetting(GameOption.VictoryPoints, int.Parse(lines.Dequeue()));
                settings.SetSetting(GameOption.UmaOption, UmaExtensionMethods.GetUma(lines.Dequeue()));
                settings.SetSetting(GameOption.RedDoraOption, RedDoraExtensionMethods.GetRedDora(lines.Dequeue()));
                settings.SetSetting(GameOption.OkaOption, OkaExtensionMethods.GetOka(lines.Dequeue()));
                settings.SetSetting(GameOption.IisouSanjunHanOption, IisouSanjunHanExtensionMethods.GetIisouSanjunHan(lines.Dequeue()));
                settings.SetSetting(GameOption.YakitoriOption, YakitoriExtensionMethods.GetYakitori(lines.Dequeue()));
            }

            // TODO: Populate tags with remaining lines.

            // Extrapolate other data.
            if (state.PrevAction == GameAction.ReplacementTilePick)
            {
                MeldImpl latestMeld = state.GetHand(state.Current).GetLatestMeld();
                state.FlipDoraAfterNextDiscard = (latestMeld != null) && ((latestMeld.State == MeldState.KanOpen) || (latestMeld.State == MeldState.KanPromoted));
            }

            state.Dealer = state.FirstDealer.AddOffset(state.Round.GetOffset());
            state.Wareme = state.Settings.GetSetting<bool>(GameOption.Wareme) ? state.Dealer.AddOffset(state.Roll - 1) : Player.None;
            state.Offset = GameStateHelpers.GetOffset(state.Dealer, state.Roll);
            state.TilesRemaining = 122 - (13 * 4) - pickedTileCount;
            state.DoraCount = 1 + (state.Player1Hand.KanCount + state.Player2Hand.KanCount + state.Player3Hand.KanCount + state.Player4Hand.KanCount) - (state.FlipDoraAfterNextDiscard ? 1 : 0);
            state.Player1HandRaw.Rebuild();
            state.Player2HandRaw.Rebuild();
            state.Player3HandRaw.Rebuild();
            state.Player4HandRaw.Rebuild();
            state.NextActionPlayer = state.Current;
            state.PopulateDoraIndicators();

            if (state.State == PlayState.GatherDecisions)
            {
                state.NextActionTile = state.GetHand(state.Current).Discards.Last().Type;
            }
            else if (state.State == PlayState.KanPerformDecision)
            {
                // Set NextActionTile to the last tile of the most recent Kan. Should be for the current player.
                // TODO: This. We need to figure out which promoted kan was the thing. Probably need to store NextActionTile in the state I guess.
            }

            // TODO: Populate state.DiscardPlayerList, Chankan flag, Kanburi flag
        }

        private static void AppendMultiString(StringBuilder sb, string value)
        {
            string[] tokens = value.Trim().Split(null);
            sb.AppendWithSpace(tokens.Length.ToString());
            foreach (string token in tokens)
            {
                sb.AppendWithSpace(token);
            }
            sb.AppendWithSpace(MULTI_STRING_NAME_END);
        }

        private static string SaveToString(GameStateImpl state, Dictionary<string, string> tags)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendWithSpace(PREFIX_V2);

            // Save state values.
            sb.AppendWithSpace(state.State.GetSkyValue().ToString());
            sb.AppendWithSpace("o"); // TileColor (orange).
            sb.AppendWithSpace(state.Round.GetTextValue());
            sb.AppendWithSpace(state.Lapped.ToString());
            sb.AppendWithSpace(state.Bonus.ToString());
            sb.AppendWithSpace(state.Pool.ToString());
            sb.AppendWithSpace(state.FirstDealer.ToString()); // Starting dealer
            sb.AppendWithSpace(state.Current.GetPlayerValue().ToString());
            sb.AppendWithSpace(state.PlayerRecentOpenKan.GetPlayerValue().ToString());
            sb.AppendWithSpace(state.Roll.ToString());
            sb.AppendWithSpace(state.PlayerDeadWallPick.ToString());
            sb.AppendWithSpace(NEUTRAL_FLAGS_STR); // GameTypeFlags
            sb.AppendWithSpace((122 - (13 * 4) - state.TilesRemaining).ToString()); // PickedTileCount
            sb.AppendWithSpace(state.PrevAction.GetSkyValue().ToString());
            sb.AppendWithSpace(state.NextAction.GetSkyValue().ToString());

            for (int i = 0; i < 136; ++i) { sb.Append(state.Wall[i].Type.GetHexString()); }
            sb.Append(' ');

            // Save player values.
            foreach (Player p in PlayerExtensionMethods.Players)
            {
                sb.AppendWithSpace("P" + p.GetPlayerValue()); // Name

                HandImpl hand = state.GetHand(p);
                sb.AppendWithSpace(hand.Score.ToString());
                sb.AppendWithSpace(hand.Streak.ToString());
                sb.AppendWithSpace(hand.Yakitori.ToString());

                for (int i = 0; i < 13; ++i) { sb.Append(hand.ActiveHandRaw[i].GetHexString()); }
                sb.Append(" ");

                sb.AppendWithSpace(hand.MeldCount.ToString());
                for (int i = 0; i < hand.MeldCount; ++i)
                {
                    MeldImpl meld = hand.MeldsRaw[i];
                    sb.AppendWithSpace(meld.State.GetMeldCode().ToString());
                    foreach (TileImpl meldTile in meld.TilesRaw) { sb.AppendWithSpace(meldTile.GetHexString()); }
                }

                sb.AppendWithSpace(hand.Discards.Count.ToString());
                foreach (TileImpl discard in hand.DiscardsImpl) { sb.AppendWithSpace(discard.GetHexString()); }
            }

            // Save settings.
            bool hasCustomRules = state.Settings.HasCustomSettings();
            sb.AppendWithSpace(hasCustomRules.ToString());
            if (hasCustomRules && (state.Settings is GameSettingsImpl))
            {
                GameSettingsImpl settings = state.Settings as GameSettingsImpl;
                sb.AppendWithSpace(settings.GetSettingField(CustomBitfields.CustomGameRules1).ToString());
                sb.AppendWithSpace(settings.GetSettingField(CustomBitfields.CustomGameRules2).ToString());
                sb.AppendWithSpace(settings.GetSettingField(CustomBitfields.CustomGameYaku1).ToString());
                sb.AppendWithSpace(settings.GetSettingField(CustomBitfields.CustomGameYaku2).ToString());
                sb.AppendWithSpace(settings.GetSettingField(CustomBitfields.CustomGameYaku3).ToString());
                sb.AppendWithSpace(settings.GetSetting<int>(GameOption.VictoryPoints).ToString());
                sb.AppendWithSpace(settings.GetSetting<Uma>(GameOption.UmaOption).GetTextValue());
                sb.AppendWithSpace(settings.GetSetting<RedDora>(GameOption.RedDoraOption).GetTextValue());
                sb.AppendWithSpace(settings.GetSetting<Oka>(GameOption.OkaOption).GetTextValue());
                sb.AppendWithSpace(settings.GetSetting<IisouSanjunHan>(GameOption.IisouSanjunHanOption).GetTextValue());
                sb.AppendWithSpace(settings.GetSetting<Yakitori>(GameOption.YakitoriOption).GetTextValue());
            }

            // Save tags.
            foreach (KeyValuePair<string, string> tuple in tags)
            {
                AppendMultiString(sb, tuple.Key);
                AppendMultiString(sb, tuple.Value);
            }

            // Output the final string.
            return sb.ToString().Trim();
        }
    }
}
