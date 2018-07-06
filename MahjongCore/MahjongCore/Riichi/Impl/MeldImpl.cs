// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi.Impl
{
    internal class MeldImpl : IMeld
    {
        // IMeld
        public Player          Owner      { get; internal set; } = Player.None;
        public Player          Target     { get; internal set; } = Player.None;
        public MeldState       State      { get; internal set; } = MeldState.None;
        public CalledDirection Direction  { get; internal set; } = CalledDirection.None;
        public ITile[]         Tiles      { get { return TilesRaw; } }

        public ITile CalledTile
        {
            get
            {
                return TilesRaw[0].Called ? TilesRaw[0] :
                       TilesRaw[1].Called ? TilesRaw[1] :
                       TilesRaw[2].Called ? TilesRaw[2] :
                       TilesRaw[3].Called ? TilesRaw[3] :
                                            null;
            }
        }

        public int RedDoraCount
        {
            get
            {
                return (TilesRaw[0].Type.IsRedDora() ? 1 : 0) +
                       (TilesRaw[1].Type.IsRedDora() ? 1 : 0) +
                       (TilesRaw[2].Type.IsRedDora() ? 1 : 0) +
                       (TilesRaw[3].Type.IsRedDora() ? 1 : 0);
            }
        }

        public void Promote(TileType kanTile, int kanTileSlot)
        {
            if (Global.CanAssert)
            {
                Global.Assert(State == MeldState.Pon);
                Global.Assert(Target != Player.None);
                Global.Assert(Direction != CalledDirection.None);
                Global.Assert(!TilesRaw[3].Type.IsTile());
            }

            State = MeldState.KanPromoted;

            TileImpl tileD = TilesRaw[3];
            tileD.Reset();
            tileD.Type = kanTile;
            tileD.Location = Location.Call;
            tileD.Ancillary = Owner;
            tileD.Slot = kanTileSlot;
        }

        // ICloneable
        public object Clone()
        {
            MeldImpl meld = new MeldImpl();
            meld.Set(this);
            return meld;
        }

        // IComparable<IMeld>
        public int CompareTo(IMeld other)
        {
            int value = Owner.CompareTo(other.Owner);                if (value != 0) { return value; }
            value = Target.CompareTo(other.Target);                  if (value != 0) { return value; }
            value = State.CompareTo(other.State);                    if (value != 0) { return value; }
            value = Direction.CompareTo(other.Direction);            if (value != 0) { return value; }
            value = TilesRaw[0].Type.CompareTo(other.Tiles[0].Type); if (value != 0) { return value; }
            value = TilesRaw[1].Type.CompareTo(other.Tiles[1].Type); if (value != 0) { return value; }
            value = TilesRaw[2].Type.CompareTo(other.Tiles[2].Type); if (value != 0) { return value; }
            value = TilesRaw[3].Type.CompareTo(other.Tiles[3].Type); if (value != 0) { return value; }
            return 0;
        }

        // MeldImpl
        internal TileImpl[] TilesRaw { get; set; } = new TileImpl[] { new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl() };

        public MeldImpl()
        {
            for (int i = 0; i < TilesRaw.Length; ++i)
            {
                TilesRaw[i].Slot = i;
                TilesRaw[i].Location = Location.Call;
            }
        }

        internal void Reset(bool skipConstantFields = false)
        {
            Target = Player.None;
            State  = MeldState.None;
            Direction = CalledDirection.None;
            TilesRaw[0].Reset(skipConstantFields);
            TilesRaw[1].Reset(skipConstantFields);
            TilesRaw[2].Reset(skipConstantFields);
            TilesRaw[3].Reset(skipConstantFields);

            if (!skipConstantFields)
            {
                Owner = Player.None;
            }
        }

        internal void Set(MeldImpl meld)
        {
            Owner = meld.Owner;
            Target = meld.Target;
            State = meld.State;
            Direction = meld.Direction;
            TilesRaw[0].Set(meld.TilesRaw[0]);
            TilesRaw[1].Set(meld.TilesRaw[1]);
            TilesRaw[2].Set(meld.TilesRaw[2]);
            TilesRaw[3].Set(meld.TilesRaw[3]);
        }

        internal void Set(MeldState state, TileImpl tileA, TileImpl tileB, TileImpl tileC, TileImpl tileD)
        {

        }

        internal void SortMeldTilesForClosedKan()
        {
            Global.Assert(State == MeldState.KanConcealed);

            // If there are any red tiles here then make sure they aren't on the edges.
            if (_Tiles[0].Type.IsRedDora())
            {
                if (_Tiles[1].Type.IsRedDora())
                {
                    // Swap the red dora into slot 2. Then we're done. Both 1 and 2 are red.
                    Swap(0, 2);
                    return;
                }
                else
                {
                    Swap(0, 1);
                }
            }

            // Two possibilities now: (Red = R, Normal = O)
            // OR??
            // OO??
            if (_Tiles[1].Type.IsRedDora())
            {
                // If 2 and 3 are normal, we're done. If both are red, then we're done. If 2 is red and 3 is normal, we're done.
                // Only if 2 is normal and 3 is red must we swap.
                if (!_Tiles[2].Type.IsRedDora() && _Tiles[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OO??.
            if (_Tiles[2].Type.IsRedDora())
            {
                Swap(1, 2);
                // We're at OROO or OROR. If 3 is red then swap it into 2 and we're done.
                if (_Tiles[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OOO?. If it's red swap it in.
            if (_Tiles[3].Type.IsRedDora())
            {
                Swap(1, 3);
            }
        }

        private void Swap(int a, int b)
        {
            TileImpl temp = TilesRaw[a];
            TilesRaw[a] = TilesRaw[b];
            TilesRaw[b] = temp;
        }
    }
}
