// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Helpers;
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

    enum MarshalSlot
    {
        Include,
        Ignore
    }

    enum MarshalLocation
    {
        Include,
        Ignore
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
    //         <tile type="string" ancillary="string" reach="string" ghost="bool" called="bool" winningtile="bool" />
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
    //             <tile type="string" />
    //             ...
    //         </restrictdiscardtiles>
    //     </extrasettings>
    //     <discardplayerlist>
    //         <player type="string" />
    //     </discardplayerlist>
    //     <hand player="string" seat="string" score="int" tileCount="int" streak="int" tempai="bool" furiten="bool" yakitori="bool"
    //           couldippatsu="bool" coulddoublereach="bool" couldkyuushuukyuuhai="bool" couldsuufurendan="bool" overridenoreach="bool"
    //           hastemporarytile="bool">
    //         <tile ... />
    //         ...
    //         <meld target="string" meldstate="string" direction="direction">
    //             <tile ... />
    //             ...
    //         </meld>
    //         ...
    //         <activetilewaits>
    //             <waittiles slot="int">
    //                 <tile type="string" />
    //                 ..
    //             </waittiles>
    //         </activetilewaits>
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
    //         <drawsandkans>
    //             <command type="string" tileB="string" tileC="string>
    //                 <tile ... /> // IF Tile != null then this is present. INCLUDE SOURCE and SLOT
    //             </command
    //         </drawsandkans>
    //         <cachedmeld ...>
    //             // SAME AS A MELD
    //         </cachedmeld>
    //         <waittiles>
    //             <tile />
    //             ...
    //         </waittiles>
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
        private static readonly string TILE_SLOT_ATTR                        = "slot";
        private static readonly string TILE_LOCATION_ATTR                    = "location";
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
        private static readonly string HAND_HASTEMPORARYTILE_ATTR            = "hastemporarytile";
        private static readonly string MELD_TAG                              = "meld";
        private static readonly string MELD_TARGET_ATTR                      = "target";
        private static readonly string MELD_STATE_ATTR                       = "state";
        private static readonly string MELD_DIRECTION_ATTR                   = "direction";
        private static readonly string CACHEDMELD_TAG                        = "meld";
        private static readonly string WAITTILES_TAG                         = "waittiles";
        private static readonly string DRAWSANDKANS_TAG                      = "drawsandkans";
        private static readonly string ACTIVERIICHIKANTILES_TAG              = "activeriichikantiles";
        private static readonly string RIICHIKANTILESPERSLOT_TAG             = "riichikantilesperslot";
        private static readonly string WINNINGHANDCACHE_TAG                  = "winninghandcache";
        private static readonly string KANTILES_TAG                          = "kantiles";
        private static readonly string ACTIVETILEWAITS_TAG                   = "activetilewaits";
        private static readonly string COMMAND_TAG                           = "command";

        private static readonly string VERSION_VALUE = "3";

        private static void MarshalAttribute<T>(XmlElement element, string attribute, T value)                          { element.SetAttribute(attribute, value.ToString()); }
        private static int  LoadAttributeInt(XmlElement element, string attribute, int defaultValue)                    { return int.TryParse(element.GetAttribute(attribute), out int intValue) ? intValue : defaultValue; }
        private static bool LoadAttributeBool(XmlElement element, string attribute, bool defaultValue)                  { return bool.TryParse(element.GetAttribute(attribute), out bool boolValue) ? boolValue : defaultValue; }
        private static T    LoadAttributeEnum<T>(XmlElement element, string attribute, T defaultValue) where T : struct { return EnumHelper.TryGetEnumByString<T>(element.GetAttribute(attribute), out T attrValue) ? attrValue : defaultValue; }

        internal static bool Matches(string state)
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
            save.Round          = LoadAttributeEnum<Round>(saveElement, SAVE_ROUND_ATTR, RequiredAttribute.Required);
            save.TilesRemaining = LoadAttributeInt(saveElement, SAVE_ROUND_ATTR, RequiredAttribute.Required);
            save.Lapped         = LoadAttributeBool(saveElement, SAVE_LAPPED_ATTR, false);
            save.Settings       = LoadSettings(saveElement.GetElementsByTagName(SETTINGS_TAG).Item(0) as XmlElement);

            XmlNodeList handList = saveElement.GetElementsByTagName(HAND_TAG);
            CommonHelpers.Check((handList.Count == 4), ("Expected four hands, found " + handList.Count));
            save.Player1Score = LoadAttributeInt((handList.Item(0) as XmlElement), HAND_SCORE_ATTR, RequiredAttribute.Required);
            save.Player2Score = LoadAttributeInt((handList.Item(1) as XmlElement), HAND_SCORE_ATTR, RequiredAttribute.Required);
            save.Player3Score = LoadAttributeInt((handList.Item(2) as XmlElement), HAND_SCORE_ATTR, RequiredAttribute.Required);
            save.Player4Score = LoadAttributeInt((handList.Item(3) as XmlElement), HAND_SCORE_ATTR, RequiredAttribute.Required);

            // Load the tags.
            save.Tags.Clear();
            XmlNodeList nodeList = saveElement.GetElementsByTagName(TAG_TAG);
            for (int i = 0; i < nodeList.Count; ++i)
            {
                var tagNode = nodeList.Item(i) as XmlElement;
                save.Tags.Add(tagNode.GetAttribute(TUPLE_KEY_ATTR), tagNode.GetAttribute(TUPLE_VALUE_ATTR));
            }
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

        private static XmlDocument Unmarshal(string state)
        {
            XmlDocument content = new XmlDocument();
            content.LoadXml(state);
            return content;
        }

        private static IGameSettings LoadSettings(XmlElement gameSettingsDocument)
        {
            return null;
        }

        private static T LoadAttributeEnum<T>(XmlElement element, string attribute, RequiredAttribute required = RequiredAttribute.Optional) where T : struct
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

        private static int LoadAttributeInt(XmlElement element, string attribute, RequiredAttribute required = RequiredAttribute.Optional)
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

        internal static string Marshal(GameStateImpl state, IDictionary<string, string> tags = null)
        {
            XmlDocument content = new XmlDocument();
            XmlElement saveElement = content.CreateElement(SAVE_TAG);

            // Setup the basic GameStateImpl attributes on the save element.
            MarshalAttribute(saveElement, SAVE_VERSION_ATTR, VERSION_VALUE);
            MarshalAttribute(saveElement, SAVE_ROUND_ATTR, state.Round);
            MarshalAttribute(saveElement, SAVE_FIRSTDEALER_ATTR, state.FirstDealer);
            MarshalAttribute(saveElement, SAVE_DEALER_ATTR, state.Dealer);
            MarshalAttribute(saveElement, SAVE_CURRENT_ATTR, state.Current);
            MarshalAttribute(saveElement, SAVE_WAREME_ATTR, state.Wareme);
            MarshalAttribute(saveElement, SAVE_STATE_ATTR, state.State);
            MarshalAttribute(saveElement, SAVE_NEXTACTION_ATTR, state.NextAction);
            MarshalAttribute(saveElement, SAVE_PREVACTION_ATTR, state.PreviousAction);
            MarshalAttribute(saveElement, SAVE_LAPPED_ATTR, state.Lapped);
            MarshalAttribute(saveElement, SAVE_OFFSET_ATTR, state.Offset);
            MarshalAttribute(saveElement, SAVE_REMAINING_ATTR, state.TilesRemaining);
            MarshalAttribute(saveElement, SAVE_BONUS_ATTR, state.Bonus);
            MarshalAttribute(saveElement, SAVE_POOL_ATTR, state.Pool);
            MarshalAttribute(saveElement, SAVE_DORACOUNT_ATTR, state.DoraCount);
            MarshalAttribute(saveElement, SAVE_ROLL_ATTR, state.Roll);
            MarshalAttribute(saveElement, SAVE_PLAYERRECENTOPENKAN_ATTR, state.PlayerRecentOpenKan);
            MarshalAttribute(saveElement, SAVE_NEXTACTIONPLAYER_ATTR, state.NextActionPlayer);
            MarshalAttribute(saveElement, SAVE_NEXTACTIONTILE_ATTR, state.NextActionTile);
            MarshalAttribute(saveElement, SAVE_PLAYERDEADWALLPICK_ATTR, state.PlayerDeadWallPick);
            MarshalAttribute(saveElement, SAVE_FLIPDORAAFTERNEXTDISCARD_ATTR, state.FlipDoraAfterNextDiscard);
            MarshalAttribute(saveElement, SAVE_CHANKAN_ATTR, state.ChankanFlag);
            MarshalAttribute(saveElement, SAVE_KANBURI_ATTR, state.KanburiFlag);
            MarshalAttribute(saveElement, SAVE_NEXTACTIONPLAYERTARGET_ATTR, state._NextActionPlayerTarget);
            MarshalAttribute(saveElement, SAVE_NEXTACTION1_ATTR, state._NextAction1);
            MarshalAttribute(saveElement, SAVE_NEXTACTION2_ATTR, state._NextAction2);
            MarshalAttribute(saveElement, SAVE_NEXTACTION3_ATTR, state._NextAction3);
            MarshalAttribute(saveElement, SAVE_NEXTACTION4_ATTR, state._NextAction4);
            MarshalAttribute(saveElement, SAVE_REWINDACTION_ATTR, state._RewindAction);
            MarshalAttribute(saveElement, SAVE_ADVANCEACTION_ATTR, state._AdvanceAction);
            MarshalAttribute(saveElement, SAVE_NEXTABORTIVEDRAWTYPE_ATTR, state._NextAbortiveDrawType);
            MarshalAttribute(saveElement, SAVE_SKIPADVANCEPLAYER_ATTR, state._SkipAdvancePlayer);
            MarshalAttribute(saveElement, SAVE_HASEXTRASETTINGS_ATTR, state._HasExtraSettings);
            MarshalAttribute(saveElement, SAVE_NEXTACTIONSLOT_ATTR, state._NextActionSlot);
            MarshalAttribute(saveElement, SAVE_CANADVANCE_ATTR, state._CanAdvance);
            MarshalAttribute(saveElement, SAVE_CANRESUME_ATTR, state._CanResume);
            MarshalAttribute(saveElement, SAVE_EXPECTINGDISCARD_ATTR, state._ExpectingDiscard);
            MarshalAttribute(saveElement, SAVE_NAGASHIWIN_ATTR, state._NagashiWin);

            // Setup the GameStateImpl children values on the save element.
            CommonHelpers.IterateDictionary(tags, (string key, string value) => { saveElement.AppendChild(MarshalTupleElement(content, TAG_TAG, key, value)); });

            XmlElement wallElement = content.CreateElement(WALL_TAG);
            foreach (TileImpl tile in state.WallRaw)
            {
                wallElement.AppendChild(MarshalTileElement(content, tile));
            }

            saveElement.AppendChild(MarshalGameSettings(content, state.Settings as GameSettingsImpl));
            saveElement.AppendChild(MarshalExtraSettings(content, state.ExtraSettings as ExtraSettingsImpl));
                        
            if (state.DiscardPlayerList.Count > 0)
            {
                XmlElement discardPlayerListElement = content.CreateElement(DISCARDPLAYERLIST_TAG);
                foreach (Player player in state.DiscardPlayerList.ToArray())
                {
                    XmlElement playerElement = content.CreateElement(PLAYER_TAG);
                    playerElement.SetAttribute(PLAYER_TYPE_ATTR, player.ToString());
                    discardPlayerListElement.AppendChild(playerElement);
                }
                saveElement.AppendChild(discardPlayerListElement);
            }            

            // Setup the hands and add them to the save element.
            saveElement.AppendChild(MarshalHandElement(content, state.Player1HandRaw));
            saveElement.AppendChild(MarshalHandElement(content, state.Player2HandRaw));
            saveElement.AppendChild(MarshalHandElement(content, state.Player3HandRaw));
            saveElement.AppendChild(MarshalHandElement(content, state.Player4HandRaw));

            // Add the save element to the document and return it as a string.
            content.AppendChild(saveElement);
            return content.ToString();
        }

        private static XmlElement MarshalHandElement(XmlDocument document, HandImpl hand)
        {
            XmlElement handElement = document.CreateElement(HAND_TAG);

            // Save the simple hand element attributes.
            MarshalAttribute(handElement, HAND_PLAYER_ATTR, hand.Player);
            MarshalAttribute(handElement, HAND_SEAT_ATTR, hand.Seat);
            MarshalAttribute(handElement, HAND_SCORE_ATTR, hand.Score);
            MarshalAttribute(handElement, HAND_TILECOUNT_ATTR, hand.TileCount);
            MarshalAttribute(handElement, HAND_STREAK_ATTR, hand.Streak);
            MarshalAttribute(handElement, HAND_TEMPAI_ATTR, hand.Tempai);
            MarshalAttribute(handElement, HAND_FURITEN_ATTR, hand.Furiten);
            MarshalAttribute(handElement, HAND_YAKITORI_ATTR, hand.Yakitori);
            MarshalAttribute(handElement, HAND_COULDIPPATSU_ATTR, hand.CouldIppatsu);
            MarshalAttribute(handElement, HAND_COULDDOUBLEREACH_ATTR, hand.CouldDoubleReach);
            MarshalAttribute(handElement, HAND_COULDKYUUSHUUKYUUHAI_ATTR, hand.CouldKyuushuuKyuuhai);
            MarshalAttribute(handElement, HAND_COULDSUUFURENDAN_ATTR, hand.CouldSuufurendan);
            MarshalAttribute(handElement, HAND_OVERRIDENOREACH_ATTR, hand.OverrideNoReachFlag);
            MarshalAttribute(handElement, HAND_HASTEMPORARYTILE_ATTR, hand.HasTemporaryTile);

            // Save the more complicated hand elements.
            HandHelpers.IterateTiles(hand, (ITile tile) => { handElement.AppendChild(MarshalTileElement(document, tile as TileImpl)); });
            HandHelpers.IterateMelds(hand, (IMeld meld) => { handElement.AppendChild(MarshalMeldElement(document, meld as MeldImpl, MELD_TAG)); });
            handElement.AppendChild(MarshalActiveTileWaits(document, hand.ActiveTileWaits));
            handElement.AppendChild(MarshalActiveRiichiKanTiles(document, hand.ActiveRiichiKanTiles));
            handElement.AppendChild(MarshalRiichiKanTilesPerSlot(document, hand.RiichiKanTilesPerSlot));

            if (hand.DrawsAndKans.Count > 0)
            {
                XmlElement drawsAndKansElement = document.CreateElement(DRAWSANDKANS_TAG);
                foreach (ICommand command in hand.DrawsAndKans)
                {
                    drawsAndKansElement.AppendChild(MarshalCommand(document, command));
                }
                handElement.AppendChild(drawsAndKansElement);
            }

            if (hand.CachedCall != null)
            {
                handElement.AppendChild(MarshalMeldElement(document, (hand.CachedCall as MeldImpl), CACHEDMELD_TAG));
            }

            if (hand.Waits.Count > 0)
            {
                XmlElement waitTilesElement = document.CreateElement(WAITTILES_TAG);
                foreach (TileType tile in hand.Waits)
                {
                    waitTilesElement.AppendChild(MarshalTileElement(document, tile));
                }
                handElement.AppendChild(waitTilesElement);
            }

            if (hand.WinningHandCache != null)
            {
                handElement.AppendChild(MarshalWinningHandCache(document, hand.WinningHandCache));
            }
            return handElement;
        }

        private static XmlElement MarshalTileElement(
            XmlDocument document,
            TileImpl tile, 
            MarshalSlot shouldMarshalSlot = MarshalSlot.Ignore,
            MarshalLocation shouldMarshalLocation = MarshalLocation.Ignore)
        {
            XmlElement tileElement = document.CreateElement(TILE_TAG);
            tileElement.SetAttribute(TILE_TYPE_ATTR, tile.Type.ToString());
            tileElement.SetAttribute(TILE_ANCILLARY_ATTR, tile.Ancillary.ToString());
            tileElement.SetAttribute(TILE_REACH_ATTR, tile.Reach.ToString());
            tileElement.SetAttribute(TILE_GHOST_ATTR, tile.Ghost.ToString());
            tileElement.SetAttribute(TILE_CALLED_ATTR, tile.Called.ToString());
            tileElement.SetAttribute(TILE_WINNINGTILE_ATTR, tile.WinningTile.ToString());

            if (shouldMarshalSlot == MarshalSlot.Include)
            {
                tileElement.SetAttribute(TILE_SLOT_ATTR, tile.Slot.ToString());
            }

            if (shouldMarshalLocation == MarshalLocation.Include)
            {
                tileElement.SetAttribute(TILE_LOCATION_ATTR, tile.Location.ToString());
            }
            return tileElement;
        }

        private static XmlElement MarshalTileElement(XmlDocument document, TileType tile)
        {
            XmlElement tileElement = document.CreateElement(TILE_TAG);
            tileElement.SetAttribute(TILE_TYPE_ATTR, tile.ToString());
            return tileElement;
        }

        private static XmlElement MarshalMeldElement(XmlDocument document, MeldImpl meld, string meldTag)
        {
            XmlElement meldElement = document.CreateElement(meldTag);
            MarshalAttribute(meldElement, MELD_TARGET_ATTR, meld.Target);
            MarshalAttribute(meldElement, MELD_STATE_ATTR, meld.State);
            MarshalAttribute(meldElement, MELD_DIRECTION_ATTR, meld.Direction);
            MeldHelpers.IterateTiles(meld, (ITile tile) => { meldElement.AppendChild(MarshalTileElement(document, tile as TileImpl)); });
            return meldElement;
        }

        private static XmlElement MarshalCommand(XmlDocument document, ICommand command)
        {
            // COMMAND_TAG
            return null;
        }

        private static XmlElement MarshalActiveTileWaits(XmlDocument document, List<TileType>[] activeTileWaits)
        {
            // ACTIVETILEWAITS_TAG
            return null;
        }

        private static XmlElement MarshalActiveRiichiKanTiles(XmlDocument document, TileType[] activeRiichiKanTiles)
        {
            // ACTIVERIICHIKANTILES_TAG
            return null;
        }

        private static XmlElement MarshalRiichiKanTilesPerSlot(XmlDocument document, TileType[][] riichiKanTilesPerSlot)
        {
            // RIICHIKANTILESPERSLOT_TAG
            // KANTILES_TAG
            return null;
        }

        private static XmlElement MarshalWinningHandCache(XmlDocument document, ICandidateHand candidateHand)
        {
            // WINNINGHANDCACHE_TAG
            return null;
        }

        private static XmlElement MarshalGameSettings(XmlDocument document, GameSettingsImpl settings)
        {
            XmlElement settingsElement = document.CreateElement(SETTINGS_TAG);
            settingsElement.SetAttribute(SETTINGS_LOCKED_ATTR, settings.Locked.ToString());
            foreach (var tuple in settings._CustomSettings)
            {
                settingsElement.AppendChild(MarshalTupleElement(document, SETTING_TAG, tuple.Key.ToString(), tuple.Value.ToString()));
            }
            return settingsElement;
        }

        private static XmlElement MarshalExtraSettings(XmlDocument document, ExtraSettingsImpl settings)
        {
            XmlElement extraSettingsElement = document.CreateElement(EXTRASETTINGS_TAG);

            // Save the simple properties.
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEANYDISARD_KEY,    settings.DisableAnyDiscard.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALL_KEY,         settings.DisableCall.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALLING_KEY,      settings.DisableCalling.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECALLPASS_KEY,     settings.DisableCallPass.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECPUWIN_KEY,       settings.DisableCPUWin.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLECPUCALLING_KEY,   settings.DisableCPUCalling.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEPLAINDISCARD_KEY, settings.DisablePlainDiscard.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLERONPASS_KEY,      settings.DisableRonPass.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEREACH_KEY,        settings.DisableReach.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLERED5_KEY,         settings.DisableRed5.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLENONREACH_KEY,     settings.DisableNonReach.ToString()));
            extraSettingsElement.AppendChild(MarshalTupleElement(document, EXTRASETTING_TAG, EXTRASETTING_DISABLEABORTIVEDRAW_KEY, settings.DisableAbortiveDraw.ToString()));

            // Save the more complicated properties.
            if (settings.RestrictDiscardTiles.Count > 0)
            {
                XmlElement restrictDiscardTiles = document.CreateElement(EXTRASETTING_RESTRICTDISCARDTILES_TAG);
                foreach (TileType tile in settings.RestrictDiscardTiles)
                {
                    extraSettingsElement.AppendChild(MarshalTileElement(document, tile));
                }
                extraSettingsElement.AppendChild(restrictDiscardTiles);
            }

            if (settings.OverrideDiceRoll != null)
            {
                extraSettingsElement.AppendChild(MarshalTupleElement(document, SETTING_TAG, EXTRASETTING_OVERRIDEDICEROLL_KEY, settings.OverrideDiceRoll.ToString()));
            }
            return extraSettingsElement;
        }

        private static XmlElement MarshalTupleElement(XmlDocument document, string tagName, string key, string value)
        {
            XmlElement tupleElement = document.CreateElement(tagName);
            tupleElement.SetAttribute(TUPLE_KEY_ATTR, key);
            tupleElement.SetAttribute(TUPLE_VALUE_ATTR, value);
            return tupleElement;
        }
    }
}
