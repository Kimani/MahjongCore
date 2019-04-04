// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;
using MahjongCore.Riichi.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MahjongCore.Riichi.Impl.SaveState
{
    internal static class SaveStateV2
    {
        private static readonly string PREFIX_V2             = "v2";
        private static readonly string MULTI_STRING_NAME_END = "multistringnameend";
        private static readonly string NEUTRAL_FLAGS_STR     = "neutral";

        public static bool Matches(string state) { return state.StartsWith(PREFIX_V2); }

        internal static void LoadCommon(string state, SaveStateImpl save)
        {
            CommonHelpers.Check(((state != null) && (state.Length > 0)), "Empty or null save string...");

            IGameState game = LoadState(state, new GameStateImpl());
            save.Settings = game.Settings;
            save.Round = game.Round;
            save.Lapped = game.Lapped;
            save.Player1Score = game.Player1Hand.Score;
            save.Player2Score = game.Player2Hand.Score;
            save.Player3Score = game.Player3Hand.Score;
            save.Player4Score = game.Player4Hand.Score;
            save.TilesRemaining = game.TilesRemaining;
        }

        internal static IGameState LoadState(string state, GameStateImpl target)
        {
            Queue<string> lines = new Queue<string>(state.Split(null)); // Passing in null splits at any whitespace.
            CommonHelpers.Check(PREFIX_V2.Equals(lines.Dequeue()), "");

            target.Reset();

            // Read game state.
            target.State               = PlayStateExtentionMethods.GetPlayState(lines.Dequeue());
            lines.Dequeue();          // TileColor
            target.Round               = RoundExtensionMethods.GetRound(lines.Dequeue());
            target.Lapped              = bool.Parse(lines.Dequeue());
            target.Bonus               = int.Parse(lines.Dequeue());
            target.Pool                = int.Parse(lines.Dequeue());
            target.FirstDealer         = PlayerHelpers.GetPlayer(lines.Dequeue());
            target.Current             = PlayerHelpers.GetPlayer(lines.Dequeue());
            target.PlayerRecentOpenKan = PlayerHelpers.GetPlayer(lines.Dequeue());
            target.Roll                = int.Parse(lines.Dequeue());
            target.PlayerDeadWallPick  = bool.Parse(lines.Dequeue());
            lines.Dequeue();          // GameTypeFlags

            int pickedTileCount = int.Parse(lines.Dequeue());
            target.PreviousAction = GameActionExtentionMethods.GetGameAction(lines.Dequeue());
            target.NextAction = GameActionExtentionMethods.GetGameAction(lines.Dequeue());

            string tileWallString = lines.Dequeue();
            CommonHelpers.Check(tileWallString.Length == (2 * 136), "Tile wall expected to be 2 * 136 characters long. Got: " + tileWallString.Length);
            for (int i = 0; i < 136; ++i)
            {
                target.WallRaw[i].Type = TileTypeExtensionMethods.GetTile(tileWallString.Substring((i * 2), 2));
                target.WallRaw[i].Ancillary = Player.None;
                target.WallRaw[i].Ghost = false;
            }

            foreach (Player p in PlayerHelpers.Players)
            {
                HandImpl hand = target.GetHand(p);

                // Get the name and score.
                string name = lines.Dequeue();
                if (int.TryParse(name, out int nameTokenCount) && (lines.Count > nameTokenCount))
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
                    hand.MeldsRaw[i].Set(p,
                                         MeldStateExtensionMethods.TryGetMeldState(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()),
                                         TileImpl.GetTile(lines.Dequeue()));
                }

                // Get discards.
                int discardCount = int.Parse(lines.Dequeue());
                for (int i = 0; i < discardCount; ++i)
                {
                    hand.DiscardsRaw.Add(TileImpl.GetTile(lines.Dequeue()));
                }
            }

            // Read settings.
            if (bool.Parse(lines.Dequeue()) && (target.Settings is GameSettingsImpl)) // Bool if there are custom game settings.
            {
                GameSettingsImpl settings = target.Settings as GameSettingsImpl;
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
            if (target.PreviousAction == GameAction.ReplacementTilePick)
            {
                MeldImpl latestMeld = target.GetHand(target.Current).GetLatestMeld();
                target.FlipDoraAfterNextDiscard = (latestMeld != null) && ((latestMeld.State == MeldState.KanOpen) || (latestMeld.State == MeldState.KanPromoted));
            }

            target.Dealer = target.FirstDealer.AddOffset(target.Round.GetOffset());
            target.Wareme = target.Settings.GetSetting<bool>(GameOption.Wareme) ? target.Dealer.AddOffset(target.Roll - 1) : Player.None;
            target.Offset = GameStateHelpers.GetOffset(target.Dealer, target.Roll);
            target.TilesRemaining = 122 - (13 * 4) - pickedTileCount;
            target.DoraCount = 1 + (target.Player1Hand.KanCount + target.Player2Hand.KanCount + target.Player3Hand.KanCount + target.Player4Hand.KanCount) - (target.FlipDoraAfterNextDiscard ? 1 : 0);
            target.Player1HandRaw.Rebuild();
            target.Player2HandRaw.Rebuild();
            target.Player3HandRaw.Rebuild();
            target.Player4HandRaw.Rebuild();
            target.NextActionPlayer = target.Current;
            target.PopulateDoraIndicators();

            if (target.State == PlayState.GatherDecisions)
            {
                target.NextActionTile = target.GetHand(target.Current).Discards.Last().Type;
            }
            else if (target.State == PlayState.KanPerformDecision)
            {
                // Set NextActionTile to the last tile of the most recent Kan. Should be for the current player.
                // TODO: This. We need to figure out which promoted kan was the thing. Probably need to store NextActionTile in the state I guess.
            }

            // TODO: Populate state.DiscardPlayerList, Chankan flag, Kanburi flag
            return target;
        }

        internal static string Marshal(GameStateImpl state, IDictionary<string, string> tags)
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
            sb.AppendWithSpace(state.PreviousAction.GetSkyValue().ToString());
            sb.AppendWithSpace(state.NextAction.GetSkyValue().ToString());

            for (int i = 0; i < 136; ++i) { sb.Append(state.Wall[i].Type.GetHexString()); }
            sb.Append(' ');

            // Save player values.
            foreach (Player p in PlayerHelpers.Players)
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
                foreach (TileImpl discard in hand.DiscardsRaw) { sb.AppendWithSpace(discard.GetHexString()); }
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
    }
}
