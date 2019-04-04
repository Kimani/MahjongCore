// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;
using System.Collections.Generic;
using System.Xml;

namespace MahjongCore.Riichi.Impl.SaveState
{
    // Version 3 of the mahjong game state stores the state as an XML document. It is more exhaustive in what it saves, so that
    // save states can be taken at more times, versus V1 and V2 which could only be reasonably saved at specific points in time.
    // The format is as like below:
    //
    // <save version="3" round="string" firstdealer="string" dealer="string" current="string" wareme="string" state="string"
    //       nextaction="string" prevaction="string" lapped="bool" offset="int" remaining="int" bonus="int" pool="int" doracount="int"
    //       roll="int" playerrecentopenkan="string" nextactionplayer="string" nextactiontile="string" playerdeadwallpick="bool"
    //       flipdoraafternextdiscard="bool" chankanflag="bool" kanburiflag="bool" nextactionplayertarget="string" nextaction1="string"
    //       nextaction2="string" nextaction3="string" nextaction4="string" rewindaction="string" advanceaction="string"
    //       nextabortivedrawtype="string" skipadvanceplayer="bool" hasextrasettings="bool" nextactionslot="int" canadvance="bool"
    //       canresume="bool" expectingdiscard="bool" nagashiwin="bool">
    //     <tag key="string" value="string" />
    //     ...
    //     <settings>
    //         <setting key="string" value="string" />
    //         ...
    //     </settings>
    //     <extrasettings>
    //         <setting key="string" value="string" />
    //         ...
    //     </extrasettings>
    //     <discardplayerlist>
    //         <player type="string" />
    //     </discardplayerlist>
    //     <wall>
    //         <tile type="string" />
    //         ...
    //     </wall>
    //     <hand player="string" seat="string" score="int" tileCount="int" streak="int" tempai="bool" furiten="bool" yakitori="bool"
    //           couldippatsu="bool" coulddoublereach="bool" couldkyuushuukyuuhai="bool" couldsuufurendan="bool" overridenoreach="bool">
    //         <tile />
    //         ...
    //         <meld>
    //             <tile />
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
    //             <tile />
    //             ...
    //         </activeriichikantiles>
    //         <riichikantilesperslot>
    //             <kantiles>
    //                 <tiles />
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
        private static readonly string SAVE_TAG      = "save";
        private static readonly string VERISON_ATTR  = "version";


        private static readonly string VERSION_VALUE = "3";

        public static bool Matches(string state)
        {
            try
            {
                XmlDocument content = Unmarshal(state);
                return content.GetElementById(SAVE_TAG).GetAttribute(VERISON_ATTR).Equals(VERSION_VALUE);
            }
            catch (Exception e)
            {
                Global.Log("Exception trying to match save state to V3: " + e.Message);
                return false;
            }
        }

        internal static void LoadCommon(string state, SaveStateImpl save)
        {
            XmlDocument xmlState = Unmarshal(state);
        }

        internal static IGameState LoadState(string state, GameStateImpl target)
        {
            return null;
        }

        internal static string Marshal(GameStateImpl state, IDictionary<string, string> tags = null)
        {
            return null;
        }

        private static XmlDocument Unmarshal(string state)
        {
            XmlDocument content = new XmlDocument();
            content.LoadXml(state);
            return content;
        }
    }
}
