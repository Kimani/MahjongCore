// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi.Impl
{
    internal class HandSortedEventArgsImpl      : HandSortedEventArgs      { public override Player           Player                  { get; internal set; }
                                                                             internal HandSortedEventArgsImpl(Player p)               { Player = p; } }
    internal class HandDiscardArgsImpl          : HandDiscardArgs          { public override ITile            Tile                    { get; internal set; }
                                                                             internal HandDiscardArgsImpl(ITile t)                    { Tile = t; } }
    internal class HandReachArgsImpl            : HandReachArgs            { public override ITile            Tile                    { get; internal set; }
                                                                             internal HandReachArgsImpl(ITile t)                      { Tile = t; } }
    internal class HandKanArgsImpl              : HandKanArgs              { public override IMeld            Meld                    { get; internal set; }
                                                                             internal HandKanArgsImpl(IMeld m)                        { Meld = m; } }
    internal class HandCallArgsImpl             : HandCallArgs             { public override IMeld            Meld                    { get; internal set; }
                                                                             internal HandCallArgsImpl(IMeld m)                       { Meld = m; } }
    internal class WallTilesPickedImpl          : WallTilesPicked          { public override ITile[]          Tiles                   { get; internal set; }
                                                                             internal WallTilesPickedImpl(ITile[] t)                  { Tiles = t; } }
    internal class DiscardRequestedArgsImpl     : DiscardRequestedArgs     { public override IDiscardInfo     Info                    { get; internal set; }
                                                                             internal DiscardRequestedArgsImpl(IDiscardInfo d)        { Info = d; } }
    internal class PostDiscardRequstedArgsImpl  : PostDiscardRequstedArgs  { public override IPostDiscardInfo Info                    { get; internal set; }
                                                                             internal PostDiscardRequstedArgsImpl(IPostDiscardInfo d) { Info = d; } }
    internal class MultiWinArgsImpl             : MultiWinArgs             { public override IWinResults[]    Results                 { get; internal set; }
                                                                             internal MultiWinArgsImpl(IWinResults[] r)               { Results = r; }    }
    internal class ExhaustiveDrawArgsImpl       : ExhaustiveDrawArgs       { public override IWinResults      Results                 { get; internal set; }
                                                                             internal ExhaustiveDrawArgsImpl(IWinResults r)           { Results = r; }    }
    internal class GameCompleteArgsImpl         : GameCompleteArgs         { public override IGameResults     Results                 { get; internal set; }
                                                                             internal GameCompleteArgsImpl(IGameResults r)            { Results = r; }  }
    internal class WinUndoneArgsImpl            : WinUndoneArgs            { public override Player           Player                  { get; internal set; }
                                                                             internal WinUndoneArgsImpl(Player p)                     { Player = p; } }
    internal class PlayerChomboArgsImpl         : PlayerChomboArgs         { public override Player           Player                  { get; internal set; }
                                                                             internal PlayerChomboArgsImpl(Player p)                  { Player = p; } }
    internal class DoraIndicatorFlippedArgsImpl : DoraIndicatorFlippedArgs { public override ITile            Tile                    { get; internal set; }
                                                                             internal DoraIndicatorFlippedArgsImpl(ITile t)           { Tile = t; } }

    internal class AbortiveDrawArgsImpl : AbortiveDrawArgs
    {
        public override AbortiveDrawType Type { get; internal set; }
        public override ITile            Tile { get; internal set; } // Tile might be null if not applicable.

        internal AbortiveDrawArgsImpl(AbortiveDrawType a, ITile t)
        {
            Type = a;
            Tile = t;
        }
    }

    internal class HandPickingTileArgsImpl : HandPickingTileArgs
    {
        public override Player Player { get; internal set; }
        public override int    Count  { get; internal set; }

        internal HandPickingTileArgsImpl(Player p, int c)
        {
            Player = p;
            Count = c;
        }
    }

    internal class HandTileAddedArgsImpl : HandTileAddedArgs
    {
        public override ITile[]    Tiles  { get; internal set; }
        public override TileSource Source { get; internal set; }

        internal HandTileAddedArgsImpl(ITile[] t, TileSource s)
        {
            Tiles = t;
            Source = s;
        }
    }

    internal class HandRonArgsImpl : HandRonArgs
    {
        public override Player      Player  { get; internal set; }
        public override IWinResults Results { get; internal set; }

        internal HandRonArgsImpl(Player p, IWinResults w)
        {
            Player = p;
            Results = w;
        }
    }

    internal class HandTsumoArgsImpl : HandTsumoArgs
    {
        public override Player      Player  { get; internal set; }
        public override IWinResults Results { get; internal set; }

        internal HandTsumoArgsImpl(Player p, IWinResults w)
        {
            Player = p;
            Results = w;
        }
    }

    internal class DiscardUndoneArgsImpl : DiscardUndoneArgs
    {
        public override Player   Player { get; internal set; }
        public override TileType Tile   { get; internal set; }

        internal DiscardUndoneArgsImpl(Player p, TileType t)
        {
            Player = p;
            Tile = t;
        }
    }

    internal class TilePickUndoneArgsImpl : TilePickUndoneArgs
    {
        public override Player Player { get; internal set; }
        public override ITile  Tile   { get; internal set; } // Will be the wall tile.

        internal TilePickUndoneArgsImpl(Player p, ITile t)
        {
            Player = p;
            Tile = t;
        }
    }

    internal class DecisionCancelledArgsImpl : DecisionCancelledArgs
    {
        public override Player Player { get; internal set; }
        public override IMeld  Meld   { get; internal set; }

        internal DecisionCancelledArgsImpl(Player p, IMeld m)
        {
            Player = p;
            Meld = m;
        }
    }
}
