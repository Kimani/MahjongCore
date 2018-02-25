// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System.Collections.Generic;

namespace MahjongCore.Riichi.AI
{
    public class PostDiscardDecision
    {
        public enum Decision
        {
            Nothing,
            Call,
            Ron
        }

        public Decision   DecisionToMake = Decision.Nothing;
        public CallOption CallToMake     = null;

        public bool Validate()
        {
            // Make sure we have a valid call.
            if (DecisionToMake == Decision.Call)
            {
                if (CallToMake == null)                        { return false; }
                if (CallToMake.Type == MeldState.None)         { return false; }
                if (CallToMake.Type == MeldState.KanConcealed) { return false; }
                if (CallToMake.Type == MeldState.KanPromoted)  { return false; }
            }
            return true;
        }
    }

    public class DiscardDecision
    {
        public enum Decision
        {
            [IsDiscard(false)] Invalid,           // Should never fill out a Decision and leave DecisionToMake as this value.
            [IsDiscard(true)]  Discard,           // Tile should be the value of the tile being discarded.
            [IsDiscard(false)] Tsumo,             // Tile is ignored.
            [IsDiscard(false)] ClosedKan,         // Tile should be one of the tiles of tile of kan is being made.
            [IsDiscard(false)] PromotedKan,       // Tile should be the tile in the active hand that is being added.
            [IsDiscard(true)]  RiichiDiscard,     // Tile should be the value of the tile being discarded.
            [IsDiscard(true)]  OpenRiichiDiscard, // Tile should be the value of the tile being discarded.
            [IsDiscard(false)] AbortiveDraw       // Tile is ignored.
        }

        public Decision DecisionToMake = Decision.Invalid;
        public TileType Tile           = TileType.None;    // Should be one of the RiichiTiles tiles. See remarks on Decision.
        public int      Slot           = -1;               // Slot must be valid for a slot in the hand matching Tile when Tile is valid.

        public bool Validate(Hand hand)
        {
            bool fValid = false;
            switch (DecisionToMake)
            {
                case Decision.PromotedKan:       // Make sure this wasn't proceeded by a chii or pon.
                                                 fValid = (hand.Parent.PrevAction != GameAction.Chii) && (hand.Parent.PrevAction != GameAction.Pon);

                                                 // Ensure that we have the tile in rHand. Once we find it
                                                 // fValid will become true and we'll be done here.
                                                 if (fValid)
                                                 {
                                                     fValid = false;
                                                     for (int i = 0; !fValid && (i < hand.ActiveTileCount); ++i)
                                                     {
                                                         fValid = Tile.IsEqual(hand.ActiveHand[i]);
                                                     }
                                                 }

                                                 // Make sure we have a pon of this type too.
                                                 if (fValid)
                                                 {
                                                     fValid = false;
                                                     for (int i = 0; !fValid && (i < 4); ++i)
                                                     {
                                                         fValid = (hand.OpenMeld[i].State == MeldState.Pon) && hand.OpenMeld[i].Tiles[0].Tile.IsEqual(Tile);
                                                     }
                                                 }
                                                 break;

                case Decision.Discard:           fValid = (Tile != TileType.None) && (!hand.IsInReach() || Tile.IsEqual(hand.ActiveHand[hand.ActiveTileCount - 1]));
                                                 break;

                case Decision.RiichiDiscard:     fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.Riichi) && hand.IsTempai() && hand.IsClosed();
                                                 break;

                case Decision.OpenRiichiDiscard: fValid = hand.Parent.Settings.GetSetting<bool>(GameOption.OpenRiichi) && hand.IsTempai() && hand.IsClosed();
                                                 break;

                case Decision.ClosedKan:         fValid = hand.CanClosedKanWithTile(Tile);
                                                 break;

                case Decision.Tsumo:             fValid = (hand.WinningHandCache != null);
                                                 break;

                case Decision.AbortiveDraw:      fValid = hand.CanKyuushuuKyuuhai();
                                                 break;

                default:                         break; // fValid should remain false.
            }
            return fValid;
        }
    }

    public static class DecisionExtensionMethods
    {
        public static bool IsDiscard(this DiscardDecision.Decision d) { return EnumAttributes.GetAttributeValue<IsDiscard, bool>(d); }
    }

    public interface IRMahjongAI
    {
        /**
         * Called when your AI gets instantiated. Lets you know what player number you are. Will be one of the
         * following in the case of a local, single player AI game (only known AI type we're worried about right now)
         * GameConstants.PLAYER_RIGHT  - Player's shimocha.
         * GameConstants.PLAYER_ACROSS - Player's toimen.
         * GameConstants.PLAYER_LEFT   - Player's kamicha.
         */
        void Initialize(Player player);

        /**
         * Gets called when a new round begins. It's possible that this won't get called, especially if we're loading
         * a game from a saved game state. Best to lazy initialize any tracking of the current round if the implementation
         * depends on it.
         * @param Round RiichiRounds.EAST_1, etc.
         */
        void StartRound(Round round);

        /**
         * Make a decision based on the most recently discard tile not by you. This gets called when you
         * have the option to chii, pon, open kan, or ron. This will also get called if you have the
         * opportunity to chankan. Decide in the body of this method what you want to do and then return
         * from this method what you want to do by setting the members on the PostDiscardDecision object
         * that is passed to the method. Check the remarks on PostDiscardDecision on how to fill it out.
         *
         * @param gameState      will allow you to poll the state of the rest of the board. DONT CACHE THIS.
         * @param currHand       will allow you to poll the state of your hand. DONT CACHE THIS.
         * @param calls          will have all the calls you can make. If you can pon, chii, or open kan, then this array
         *                       will be non-null and have it in there. You should set decision.CallToMake to be one of these.
         *                       and then set DecisionToMake to PostDiscardDecision.Decision.Call to reflect the call.
         * @param discardedTile  The value of the tile that was discarded.
         * @param targetPlayer   The player that discarded the tile. GameConstants.PLAYER_{BOTTOM/RIGHT/ACROSS/LEFT}.
         * @param fCanRon        True if a ron can be made.
         * @param fCanChankanRon True if we can ron with chankan.
         * @return decision      Fill this out to make a decision and return what should be done.
         */
        PostDiscardDecision GetPostDiscardDecision(GameState state,
                                                   Hand hand,
                                                   List<CallOption> calls,
                                                   TileType discardedTile,
                                                   Player targetPlayer,
                                                   bool canRon,
                                                   bool canChankanRon);

        /**
         * Make a decision when it's your turn. This gets called after your seat has drawn a tile and currently
         * has a full hand. You're expected to decide which tile to discard, to reach, tsumo, make a kan, make
         * an abortive draw (via kyuushuukyuuhai), etc. You are told whether or not your draw came from the wall
         * or the dead wall, which can occur if the previous call to this resulted in you making a kan.
         *
         * This also gets called after you decide to call pon or chii, or make a kan and get a tile from the dead wall.
         *
         * If you are in reach, and you decide to discard, the tile you pick to discard is ignored - it will
         * always be the one you draw.
         *
         * @param gameState           Poll the state of the rest of the board. DONT CACHE THIS.
         * @param currHand            Poll the state of the hand. DONT CACHE THIS.
         * @param fCanReach           True if you can reach OR double reach OR open reach. Pay attention if normal reach is disabled.
         *                            That means only double or open reach are possible.
         * @param fCanDoubleReach     True only if you can double reach AND if double reach is enabled.
         * @param fCanOpenReach       True only if you can open reach AND if  open reach is enabled.
         * @param fCanTsumo           True if you can tsumo.
         * @param fCanClosedKan       True if you can make a closed kan with tiles in your hand.
         * @param fCanPromotedKan     True if you can make a promoted kan with a previously called pon and the 4th tile in your hand.
         * @param fCanAbortiveDraw    True if you can declare Kyuushuu Kyuuhai. This does not get set if you have the opportunity to
         *                            make any of the other kinds of abortive draws (suufonrenda, etc.) so you'll have to check for those.
         * @param tileSource          WallDraw if from a normal draw, Call if after a pon/chii, or ReplacementTileDraw after a kan.
         * @param invalidDiscardTile  A tile value that you cannot discard. This can happen if you perform a chii and you're not
         *                            allowed to sequence shift.
         * @return decision           Fill this out to make a decision and return what should be done.
         */
        DiscardDecision GetDiscardDecision(GameState state,
                                           Hand hand,
                                           bool canReach,
                                           bool canDoubleReach,
                                           bool canOpenReach,
                                           bool canTsumo,
                                           bool canClosedKan,
                                           bool canPromotedKan,
                                           bool canAbortiveDraw,
                                           TileSource tileSource,
                                           TileType invalidDiscardTile);
    }
}
