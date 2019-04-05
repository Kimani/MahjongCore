// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Xml;

namespace MahjongCore.Riichi.Impl.SaveState
{
    enum RequiredAttribute
    {
        Required,
        Optional
    }

    // Version 3 of the mahjong game state stores the state as an XML document. It is more exhaustive in what it saves, so that
    // save states can be taken at more times, versus V1 and V2 which could only be reasonably saved at specific points in time.
    // The format is as like below:
    //
    // <save version="3" round="string" firstdealer="string" dealer="string" current="string" wareme="string" state="string"
    //       nextaction="string" prevaction="string" lapped="bool" offset="int" remaining="int" bonus="int" pool="int" doracount="int"
    //       roll="int" playerrecentopenkan="string" nextactionplayer="string" nextactiontile="string" playerdeadwallpick="bool"
    //       flipdoraafternextdiscard="bool" chankan="bool" kanburi="bool" nextactionplayertarget="string" nextaction1="string"
    //       nextaction2="string" nextaction3="string" nextaction4="string" rewindaction="string" advanceaction="string"
    //       nextabortivedrawtype="string" skipadvanceplayer="bool" hasextrasettings="bool" nextactionslot="int" canadvance="bool"
    //       canresume="bool" expectingdiscard="bool" nagashiwin="bool">
    //     <tag key="string" value="string" />
    //     ...
    //     <wall>
    //         <tile type="string" />
    //         ...
    //     </wall>
    //     <settings locked="bool">
    //         <setting key="string" value="string" />
    //         ...
    //     </settings>
    //     <extrasettings>
    //         <setting key="string" value="string" />
    //         ...
    //         <restrictdiscardtiles>
    //             <tile type = "string" />
    //             ...
    //         </restrictdiscardtiles>
    //     </extrasettings>
    //     <discardplayerlist>
    //         <player type="string" />
    //     </discardplayerlist>
    //     <hand player="string" seat="string" score="int" tileCount="int" streak="int" tempai="bool" furiten="bool" yakitori="bool"
    //           couldippatsu="bool" coulddoublereach="bool" couldkyuushuukyuuhai="bool" couldsuufurendan="bool" overridenoreach="bool">
    //         <tile ... />
    //         ...
    //         <meld>
    //             <tile ... />
    //             ...
    //         </meld>
    //         ...
    //         <drawsandkans>
    //             <command type="string" />
    //         </drawsandkans>
    //         <cachedmeld>
    //             // SAME AS A MELD
    //         </cachedmeld>
    //         <waittiles>
    //             <tile />
    //             ...
    //         </waittiles>
    //         <activeriichikantiles>
    //             <tile ... />
    //             ...
    //         </activeriichikantiles>
    //         <riichikantilesperslot>
    //             <kantiles>
    //                 <tile ... />
    //                 ...
    //             </kantiles>
    //             ...
    //         </riichikantilesperslot>
    //         <winninghandcache>
    //             ????
    //         </winninghandcache>
    //     </hand>
    //     ...
    // </save>
    internal static class SaveStateV3
    {
        private static readonly string SAVE_TAG                              = "save";
        private static readonly string SAVE_VERSION_ATTR                     = "version";
        private static readonly string SAVE_ROUND_ATTR                       = "round";
        private static readonly string SAVE_FIRSTDEALER_ATTR                 = "firstdealer";
        private static readonly string SAVE_DEALER_ATTR                      = "dealer";
        private static readonly string SAVE_CURRENT_ATTR                     = "current";
        private static readonly string SAVE_WAREME_ATTR                      = "wareme";
        private static readonly string SAVE_STATE_ATTR                       = "state";
        private static readonly string SAVE_NEXTACTION_ATTR                  = "nextaction";
        private static readonly string SAVE_PREVACTION_ATTR                  = "prevaction";
        private static readonly string SAVE_LAPPED_ATTR                      = "lapped";
        private static readonly string SAVE_OFFSET_ATTR                      = "offset";
        private static readonly string SAVE_REMAINING_ATTR                   = "remaining";
        private static readonly string SAVE_BONUS_ATTR                       = "bonus";
        private static readonly string SAVE_POOL_ATTR                        = "pool";
        private static readonly string SAVE_DORACOUNT_ATTR                   = "doracount";
        private static readonly string SAVE_ROLL_ATTR                        = "roll";
        private static readonly string SAVE_PLAYERRECENTOPENKAN_ATTR         = "playerrecentopenkan";
        private static readonly string SAVE_NEXTACTIONPLAYER_ATTR            = "nextactionplayer";
        private static readonly string SAVE_NEXTACTIONTILE_ATTR              = "nextactiontile";
        private static readonly string SAVE_PLAYERDEADWALLPICK_ATTR          = "playerdeadwallpick";
        private static readonly string SAVE_FLIPDORAAFTERNEXTDISCARD_ATTR    = "flipdoraafternextdiscard";
        private static readonly string SAVE_CHANKAN_ATTR                     = "chankan";
        private static readonly string SAVE_KANBURI_ATTR                     = "kanburi";
        private static readonly string SAVE_NEXTACTIONPLAYERTARGET_ATTR      = "nextactionplayertarget";
        private static readonly string SAVE_NEXTACTION1_ATTR                 = "nextaction1";
        private static readonly string SAVE_NEXTACTION2_ATTR                 = "nextaction2";
        private static readonly string SAVE_NEXTACTION3_ATTR                 = "nextaction3";
        private static readonly string SAVE_NEXTACTION4_ATTR                 = "nextaction4";
        private static readonly string SAVE_REWINDACTION_ATTR                = "rewindaction";
        private static readonly string SAVE_ADVANCEACTION_ATTR               = "advanceaction";
        private static readonly string SAVE_NEXTABORTIVEDRAWTYPE_ATTR        = "nextabortivedrawtype";
        private static readonly string SAVE_SKIPADVANCEPLAYER_ATTR           = "skipadvanceplayer";
        private static readonly string SAVE_HASEXTRASETTINGS_ATTR            = "hasextrasettings";
        private static readonly string SAVE_NEXTACTIONSLOT_ATTR              = "nextactionslot";
        private static readonly string SAVE_CANADVANCE_ATTR                  = "canadvance";
        private static readonly string SAVE_CANRESUME_ATTR                   = "canresume";
        private static readonly string SAVE_EXPECTINGDISCARD_ATTR            = "expectingdiscard";
        private static readonly string SAVE_NAGASHIWIN_ATTR                  = "nagashiwin";
        private static readonly string WALL_TAG                              = "wall";
        private static readonly string TILE_TAG                              = "tile";
        private static readonly string TILE_TYPE_ATTR                        = "type";
        private static readonly string TILE_ANCILLARY_ATTR                   = "ancillary";
        private static readonly string TILE_REACH_ATTR                       = "reach";
        private static readonly string TILE_GHOST_ATTR                       = "ghost";
        private static readonly string TILE_CALLED_ATTR                      = "called";
        private static readonly string TILE_WINNINGTILE_ATTR                 = "winningtile";
        private static readonly string TAG_TAG                               = "tag";
        private static readonly string TUPLE_KEY_ATTR                        = "key";
        private static readonly string TUPLE_VALUE_ATTR                      = "value";
        private static readonly string SETTINGS_TAG                          = "settings";
        private static readonly string SETTINGS_LOCKED_ATTR                  = "locked";
        private static readonly string SETTING_TAG                           = "settings";
        private static readonly string DISCARDPLAYERLIST_TAG                 = "discardplayerlist";
        private static readonly string PLAYER_TAG                            = "player";
        private static readonly string PLAYER_TYPE_ATTR                      = "type";
        private static readonly string EXTRASETTINGS_TAG                     = "extrasettings";
        private static readonly string EXTRASETTING_TAG                      = "setting";
        private static readonly string EXTRASETTING_DISABLEANYDISARD_KEY     = "disableanydiscard";
        private static readonly string EXTRASETTING_DISABLECALL_KEY          = "disablecall";
        private static readonly string EXTRASETTING_DISABLECALLING_KEY       = "disablecalling";
        private static readonly string EXTRASETTING_DISABLECALLPASS_KEY      = "disablecallpass";
        private static readonly string EXTRASETTING_DISABLECPUWIN_KEY        = "disablecpuwin";
        private static readonly string EXTRASETTING_DISABLECPUCALLING_KEY    = "disablecpucalling";
        private static readonly string EXTRASETTING_DISABLEPLAINDISCARD_KEY  = "disableplaindiscard";
        private static readonly string EXTRASETTING_DISABLERONPASS_KEY       = "disableronpass";
        private static readonly string EXTRASETTING_DISABLEREACH_KEY         = "disablereach";
        private static readonly string EXTRASETTING_DISABLERED5_KEY          = "disablered5";
        private static readonly string EXTRASETTING_DISABLENONREACH_KEY      = "disablenonreach";
        private static readonly string EXTRASETTING_DISABLEABORTIVEDRAW_KEY  = "disableabortivedraw";
        private static readonly string EXTRASETTING_OVERRIDEDICEROLL_KEY     = "overridediceroll";
        private static readonly string EXTRASETTING_RESTRICTDISCARDTILES_TAG = "restrictdiscardtiles";
        private static readonly string HAND_TAG                              = "hand";
        private static readonly string HAND_PLAYER_ATTR                      = "player";
        private static readonly string HAND_SEAT_ATTR                        = "seat";
        private static readonly string HAND_SCORE_ATTR                       = "score";
        private static readonly string HAND_TILECOUNT_ATTR                   = "tilecount";
        private static readonly string HAND_STREAK_ATTR                      = "streak";
        private static readonly string HAND_TEMPAI_ATTR                      = "tempai";
        private static readonly string HAND_FURITEN_ATTR                     = "furiten";
        private static readonly string HAND_YAKITORI_ATTR                    = "yakitori";
        private static readonly string HAND_COULDIPPATSU_ATTR                = "couldippatsu";
        private static readonly string HAND_COULDDOUBLEREACH_ATTR            = "coulddoublereach";
        private static readonly string HAND_COULDKYUUSHUUKYUUHAI_ATTR        = "couldkyuushuukyuuhai";
        private static readonly string HAND_COULDSUUFURENDAN_ATTR            = "couldsuufuurendan";
        private static readonly string HAND_OVERRIDENOREACH_ATTR             = "overridenoreach";

        private static readonly string VERSION_VALUE = "3";

        private static void SaveAttribute<T>(XmlElement element, string attribute, T value)                            { element.SetAttribute(attribute, value.ToString()); }
        private static int  GetAttributeInt(XmlElement element, string attribute, int defaultValue)                    { return int.TryParse(element.GetAttribute(attribute), out int intValue) ? intValue : defaultValue; }
        private static bool GetAttributeBool(XmlElement element, string attribute, bool defaultValue)                  { return bool.TryParse(element.GetAttribute(attribute), out bool boolValue) ? boolValue : defaultValue; }
        private static T    GetAttributeEnum<T>(XmlElement element, string attribute, T defaultValue) where T : struct { return EnumHelper.TryGetEnumByString<T>(element.GetAttribute(attribute), out T attrValue) ? attrValue : defaultValue; }

        public static bool Matches(string state)
        {
            try
            {
                return Unmarshal(state).GetElementById(SAVE_TAG).GetAttribute(SAVE_VERSION_ATTR).Equals(VERSION_VALUE);
            }
            catch (Exception e)
            {
                Global.Log("Exception trying to match save state to V3: " + e.Message);
                return false;
            }
        }

        internal static void LoadCommon(string state, SaveStateImpl save)
        {
            XmlDocument content = Unmarshal(state);
            XmlElement saveElement = content.GetElementById(SAVE_TAG);

            // Load the common save state data.
            save.Round          = GetAttributeEnum<Round>(saveElement, SAVE_ROUND_ATTR, RequiredAttribute.Required);
            save.TilesRemaining = GetAttributeInt(saveElement, SAVE_ROUND_ATTR, RequiredAttribute.Required);
            save.Lapped         = GetAttributeBool(saveElement, SAVE_LAPPED_ATTR, false);
            /*save.Settings = game.Settings;
            save.Player1Score = game.Player1Hand.Score;
            save.Player2Score = game.Player2Hand.Score;
            save.Player3Score = game.Player3Hand.Score;
            save.Player4Score = game.Player4Hand.Score;
            */

            // Load the tags.
        }

        internal static IGameState LoadState(string state, GameStateImpl target)
        {
            XmlDocument content = Unmarshal(state);

            // Setup the basic GameStateImpl values.

            // Setup the more in depth GameStateImpl values.

            // Setup the hands.

            // Done! Return the state.
            return target;
        }

        internal static string Marshal(GameStateImpl state, IDictionary<string, string> tags = null)
        {
            XmlDocument content = new XmlDocument();
            XmlElement saveElement = content.CreateElement(SAVE_TAG);

            // Setup the basic GameStateImpl attributes on the save element.
            SaveAttribute(saveElement, SAVE_VERSION_ATTR, VERSION_VALUE);
            SaveAttribute(saveElement, SAVE_ROUND_ATTR, state.Round);
            SaveAttribute(saveElement, SAVE_FIRSTDEALER_ATTR, state.FirstDealer);
            SaveAttribute(saveElement, SAVE_DEALER_ATTR, state.Dealer);
            SaveAttribute(saveElement, SAVE_CURRENT_ATTR, state.Current);
            SaveAttribute(saveElement, SAVE_WAREME_ATTR, state.Wareme);
            SaveAttribute(saveElement, SAVE_STATE_ATTR, state.State);
            SaveAttribute(saveElement, SAVE_NEXTACTION_ATTR, state.NextAction);
            SaveAttribute(saveElement, SAVE_PREVACTION_ATTR, state.PreviousAction);
            SaveAttribute(saveElement, SAVE_LAPPED_ATTR, state.Lapped);
            SaveAttribute(saveElement, SAVE_OFFSET_ATTR, state.Offset);
            SaveAttribute(saveElement, SAVE_REMAINING_ATTR, state.TilesRemaining);
            SaveAttribute(saveElement, SAVE_BONUS_ATTR, state.Bonus);
            SaveAttribute(saveElement, SAVE_POOL_ATTR, state.Pool);
            SaveAttribute(saveElement, SAVE_DORACOUNT_ATTR, state.DoraCount);
            SaveAttribute(saveElement, SAVE_ROLL_ATTR, state.Roll);
            SaveAttribute(saveElement, SAVE_PLAYERRECENTOPENKAN_ATTR, state.PlayerRecentOpenKan);
            SaveAttribute(saveElement, SAVE_NEXTACTIONPLAYER_ATTR, state.NextActionPlayer);
            SaveAttribute(saveElement, SAVE_NEXTACTIONTILE_ATTR, state.NextActionTile);
            SaveAttribute(saveElement, SAVE_PLAYERDEADWALLPICK_ATTR, state.PlayerDeadWallPick);
            SaveAttribute(saveElement, SAVE_FLIPDORAAFTERNEXTDISCARD_ATTR, state.FlipDoraAfterNextDiscard);
            SaveAttribute(saveElement, SAVE_CHANKAN_ATTR, state.ChankanFlag);
            SaveAttribute(saveElement, SAVE_KANBURI_ATTR, state.KanburiFlag);
            SaveAttribute(saveElement, SAVE_NEXTACTIONPLAYERTARGET_ATTR, state._NextActionPlayerTarget);
            SaveAttribute(saveElement, SAVE_NEXTACTION1_ATTR, state._NextAction1);
            SaveAttribute(saveElement, SAVE_NEXTACTION2_ATTR, state._NextAction2);
            SaveAttribute(saveElement, SAVE_NEXTACTION3_ATTR, state._NextAction3);
            SaveAttribute(saveElement, SAVE_NEXTACTION4_ATTR, state._NextAction4);
            SaveAttribute(saveElement, SAVE_REWINDACTION_ATTR, state._RewindAction);
            SaveAttribute(saveElement, SAVE_ADVANCEACTION_ATTR, state._AdvanceAction);
            SaveAttribute(saveElement, SAVE_NEXTABORTIVEDRAWTYPE_ATTR, state._NextAbortiveDrawType);
            SaveAttribute(saveElement, SAVE_SKIPADVANCEPLAYER_ATTR, state._SkipAdvancePlayer);
            SaveAttribute(saveElement, SAVE_HASEXTRASETTINGS_ATTR, state._HasExtraSettings);
            SaveAttribute(saveElement, SAVE_NEXTACTIONSLOT_ATTR, state._NextActionSlot);
            SaveAttribute(saveElement, SAVE_CANADVANCE_ATTR, state._CanAdvance);
            SaveAttribute(saveElement, SAVE_CANRESUME_ATTR, state._CanResume);
            SaveAttribute(saveElement, SAVE_EXPECTINGDISCARD_ATTR, state._ExpectingDiscard);
            SaveAttribute(saveElement, SAVE_NAGASHIWIN_ATTR, state._NagashiWin);

            // Setup the GameStateImpl children values on the save element.
            if (tags != null)
            {
                foreach (var tuple in tags) { saveElement.AppendChild(SaveTupleElement(content, TAG_TAG, tuple.Key, tuple.Value)); }
            }

            XmlElement wallElement = content.CreateElement(WALL_TAG);
            foreach (TileImpl tile in state.WallRaw)
            {
                wallElement.AppendChild(SaveTileElement(content, tile));
            }

            saveElement.AppendChild(SaveGameSettings(content, state.Settings as GameSettingsImpl));
            saveElement.AppendChild(SaveExtraSettings(content, state.ExtraSettings as ExtraSettingsImpl));

            XmlElement discardPlayerListElement = content.CreateElement(DISCARDPLAYERLIST_TAG);
            foreach (Player player in state.DiscardPlayerList.ToArray())
            {
                XmlElement playerElement = content.CreateElement(PLAYER_TAG);
                playerElement.SetAttribute(PLAYER_TYPE_ATTR, player.ToString());
                discardPlayerListElement.AppendChild(playerElement);
            }
            saveElement.AppendChild(discardPlayerListElement);

            // Setup the hands and add them to the save element.
            saveElement.AppendChild(SaveHandElement(content, state.Player1HandRaw));
            saveElement.AppendChild(SaveHandElement(content, state.Player2HandRaw));
            saveElement.AppendChild(SaveHandElement(content, state.Player3HandRaw));
            saveElement.AppendChild(SaveHandElement(content, state.Player4HandRaw));

            // Add the save element to the document and return it as a string.
            content.AppendChild(saveElement);
            return content.ToString();
        }

        private static XmlElement SaveTileElement(XmlDocument document, TileImpl tile)
        {
            XmlElement tileElement = document.CreateElement(TILE_TAG);
            tileElement.SetAttribute(TILE_TYPE_ATTR, tile.Type.ToString());
            tileElement.SetAttribute(TILE_ANCILLARY_ATTR, tile.Ancillary.ToString());
            tileElement.SetAttribute(TILE_REACH_ATTR, tile.Reach.ToString());
            tileElement.SetAttribute(TILE_GHOST_ATTR, tile.Ghost.ToString());
            tileElement.SetAttribute(TILE_CALLED_ATTR, tile.Called.ToString());
            tileElement.SetAttribute(TILE_WINNINGTILE_ATTR, tile.WinningTile.ToString());
            return tileElement;
        }

        private static XmlElement SaveTileElement(XmlDocument document, TileType tile)
        {
            XmlElement tileElement = document.CreateElement(TILE_TAG);
            tileElement.SetAttribute(TILE_TYPE_ATTR, tile.ToString());
            return tileElement;
        }

        private static XmlElement SaveHandElement(XmlDocument document, HandImpl hand)
        {
            XmlElement handElement = document.CreateElement(HAND_TAG);

            // Save the simple hand element attributes.
            SaveAttribute(handElement, HAND_PLAYER_ATTR, hand.Player);
            SaveAttribute(handElement, HAND_SEAT_ATTR, hand.Seat);
            SaveAttribute(handElement, HAND_SCORE_ATTR, hand.Score);
            SaveAttribute(handElement, HAND_TILECOUNT_ATTR, hand.TileCount);
            SaveAttribute(handElement, HAND_STREAK_ATTR, hand.Streak);
            SaveAttribute(handElement, HAND_TEMPAI_ATTR, hand.Tempai);
            SaveAttribute(handElement, HAND_FURITEN_ATTR, hand.Furiten);
            SaveAttribute(handElement, HAND_YAKITORI_ATTR, hand.Yakitori);
            SaveAttribute(handElement, HAND_COULDIPPATSU_ATTR, hand.CouldIppatsu);
            SaveAttribute(handElement, HAND_COULDDOUBLEREACH_ATTR, hand.CouldDoubleReach);
            SaveAttribute(handElement, HAND_COULDKYUUSHUUKYUUHAI_ATTR, hand.CouldKyuushuuKyuuhai);
            SaveAttribute(handElement, HAND_COULDSUUFURENDAN_ATTR, hand.CouldSuufurendan);
            SaveAttribute(handElement, HAND_OVERRIDENOREACH_ATTR, hand.OverrideNoReachFlag);

            // Save the more complicated hand elements.
            // ...
            return handElement;
        }

        private static XmlElement SaveGameSettings(XmlDocument document, GameSettingsImpl settings)
        {
            XmlElement settingsElement = document.CreateElement(SETTINGS_TAG);
            settingsElement.SetAttribute(SETTINGS_LOCKED_ATTR, settings.Locked.ToString());
            foreach (var tuple in settings._CustomSettings)
            {
                settingsElement.AppendChild(SaveTupleElement(document, SETTING_TAG, tuple.Key.ToString(), tuple.Value.ToString()));
            }
            return settingsElement;
        }

        private static XmlElement SaveExtraSettings(XmlDocument document, ExtraSettingsImpl settings)
        {
            XmlElement extraSettingsElement = document.CreateElement(EXTRASETTINGS_TAG);

            // Save the simple properties.
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEANYDISARD_KEY,    settings.DisableAnyDiscard.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALL_KEY,         settings.DisableCall.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALLING_KEY,      settings.DisableCalling.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALLPASS_KEY,     settings.DisableCallPass.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECPUWIN_KEY,       settings.DisableCPUWin.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECPUCALLING_KEY,   settings.DisableCPUCalling.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEPLAINDISCARD_KEY, settings.DisablePlainDiscard.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLERONPASS_KEY,      settings.DisableRonPass.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEREACH_KEY,        settings.DisableReach.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLERED5_KEY,         settings.DisableRed5.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLENONREACH_KEY,     settings.DisableNonReach.ToString()));
            extraSettingsElement.AppendChild(SaveTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEABORTIVEDRAW_KEY, settings.DisableAbortiveDraw.ToString()));

            // Save the more complicated properties.
            XmlElement restrictDiscardTiles = document.CreateElement(EXTRASETTING_RESTRICTDISCARDTILES_TAG);
            foreach (TileType tile in settings.RestrictDiscardTiles)
            {
                extraSettingsElement.AppendChild(SaveTileElement(document, tile));
            }
            extraSettingsElement.AppendChild(restrictDiscardTiles);

            if (settings.OverrideDiceRoll != null)
            {
                extraSettingsElement.AppendChild(SaveTupleElement(document, SETTING_TAG, EXTRASETTING_OVERRIDEDICEROLL_KEY, settings.OverrideDiceRoll.ToString()));
            }
            return extraSettingsElement;
        }

        private static XmlElement SaveTupleElement(XmlDocument document, string tagName, string key, string value)
        {
            XmlElement tupleElement = document.CreateElement(tagName);
            tupleElement.SetAttribute(TUPLE_KEY_ATTR, key);
            tupleElement.SetAttribute(TUPLE_VALUE_ATTR, value);
            return tupleElement;
        }

        private static T GetAttributeEnum<T>(XmlElement element, string attribute, RequiredAttribute required = RequiredAttribute.Optional) where T : struct
        {
            if (EnumHelper.TryGetEnumByString<T>(element.GetAttribute(attribute), out T attrValue))
            {
                return attrValue;
            }
            else if (required == RequiredAttribute.Required)
            {
                throw new Exception("Couldn't find required attribute!");
            }
            return default(T);
        }

        private static int GetAttributeInt(XmlElement element, string attribute, RequiredAttribute required = RequiredAttribute.Optional)
        {
            if (int.TryParse(element.GetAttribute(attribute), out int intValue))
            {
                return intValue;
            }
            else if (required == RequiredAttribute.Required)
            {
                throw new Exception("Couldn't find required attribute!");
            }
            return default(int);
        }

        private static XmlDocument Unmarshal(string state)
        {
            XmlDocument content = new XmlDocument();
            content.LoadXml(state);
            return content;
        }
    }
}
