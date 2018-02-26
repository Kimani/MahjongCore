// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi.Impl
{
    public class MeldImpl : IMeld
    {
        public MeldState       State             { get; private set; }
        public ExtendedTile[]  Tiles             { get; private set; }  = new ExtendedTile[] { new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile() };
        public int             SourceDiscardSlot { get; set; }

        public int RedDoraCount
        {
            get
            {
                return (Tiles[0].Tile.IsRedDora() ? 1 : 0) +
                       (Tiles[1].Tile.IsRedDora() ? 1 : 0) +
                       (Tiles[2].Tile.IsRedDora() ? 1 : 0) +
                       (Tiles[3].Tile.IsRedDora() ? 1 : 0);
            }
        }

        public TileType CalledTile
        {
            get
            {
                return Tiles[0].Called ? Tiles[0].Tile :
                       Tiles[1].Called ? Tiles[1].Tile :
                       Tiles[2].Called ? Tiles[2].Tile :
                       Tiles[3].Called ? Tiles[3].Tile :
                                         TileType.None;
            }
        }

        public MeldImpl()
        {
            Reset();
        }

        public MeldImpl(MeldState state, CalledDirection direction, TileType a, TileType b, TileType c, TileType d)
        {
            State = state;
            Tiles[0].Tile = a;
            Tiles[1].Tile = b;
            Tiles[2].Tile = c;
            Tiles[3].Tile = d;

            if      (direction == CalledDirection.Left)   { Tiles[0].Called = true; }
            else if (direction == CalledDirection.Across) { Tiles[1].Called = true; }
            else if (direction == CalledDirection.Right)  { Tiles[state.GetTileCount() - 1].Called = true; }
        }

        public MeldImpl(MeldState state, ExtendedTile a, ExtendedTile b, ExtendedTile c, ExtendedTile d)
        {
            State = state;
            Tiles[0] = a;
            Tiles[1] = b;
            Tiles[2] = c;
            Tiles[3] = d;
        }

        public void Reset()
        {
            SourceDiscardSlot = -1;
            State = MeldState.None;
            Tiles[0].Reset();
            Tiles[1].Reset();
            Tiles[2].Reset();
            Tiles[3].Reset();
        }

        public void CopyFrom(MeldImpl m)
        {
            SourceDiscardSlot = m.SourceDiscardSlot;
            State = m.State;
            Tiles[0].Set(m.Tiles[0]);
            Tiles[1].Set(m.Tiles[1]);
            Tiles[2].Set(m.Tiles[2]);
            Tiles[3].Set(m.Tiles[3]);
        }

        public void SortMeldTilesForClosedKan()
        {
            RiichiGlobal.Assert(State == MeldState.KanConcealed);

            // If there are any red tiles here then make sure they aren't on the edges.
            if (Tiles[0].Tile.IsRedDora())
            {
                if (Tiles[1].Tile.IsRedDora())
                {
                    // Swap the red dora into slot 2. Then we're done. Both 1 and 2 are red.
                    Swap(0, 2);
                    return;
                }
                else
                {
                    Swap(1, 2);
                }
            }

            // Two possibilities now: (Red = R, Normal = O)
            // OR??
            // OO??
            if (Tiles[1].Tile.IsRedDora())
            {
                // If 2 and 3 are normal, we're done. If both are red, then we're done. If 2 is red and 3 is normal, we're done.
                // Only if 2 is normal and 3 is red must we swap.
                if (!Tiles[2].Tile.IsRedDora() && Tiles[3].Tile.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OO??.
            if (Tiles[2].Tile.IsRedDora())
            {
                Swap(1, 2);
                // We're at OROO or OROR. If 3 is red then swap it into 2 and we're done.
                if (Tiles[3].Tile.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OOO?. If it's red swap it in.
            if (Tiles[3].Tile.IsRedDora())
            {
                Swap(1, 3);
            }
        }

        public bool Equals(IMeld m)
        {
            return (State == m.State) &&
                   (Tiles[0].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[0].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[0].Tile.IsEqual(m.Tiles[2].Tile))) &&
                   (Tiles[1].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[1].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[1].Tile.IsEqual(m.Tiles[2].Tile))) &&
                   (Tiles[2].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[2].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[2].Tile.IsEqual(m.Tiles[2].Tile)));
        }

        private void Swap(int a, int b)
        {
            ExtendedTile temp = Tiles[a];
            Tiles[a] = Tiles[b];
            Tiles[b] = temp;
        }

        // ICloneable
        public object Clone()
        {
            MeldImpl m = new MeldImpl();
            m.CopyFrom(this);
            return m;
        }
    }
}
