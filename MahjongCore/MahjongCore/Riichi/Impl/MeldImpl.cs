// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common;

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

        internal MeldImpl()
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

        internal void Set(IMeld meld)
        {
            Owner = meld.Owner;
            Target = meld.Target;
            State = meld.State;
            Direction = meld.Direction;
            TilesRaw[0].Set(meld.Tiles[0]);
            TilesRaw[1].Set(meld.Tiles[1]);
            TilesRaw[2].Set(meld.Tiles[2]);
            TilesRaw[3].Set(meld.Tiles[3]);
        }

        internal void Set(Player owner, MeldState state, TileImpl tileA, TileImpl tileB, TileImpl tileC, TileImpl tileD)
        {
            Owner = owner;
            State = state;

            TilesRaw[0].Set(tileA);
            TilesRaw[1].Set(tileB);
            TilesRaw[2].Set(tileC);
            TilesRaw[3].Set(tileD);

            if ((state == MeldState.Chii) || (tileA.Called))
            {
                Direction = CalledDirection.Left;
                Target = owner.GetPrevious();
            }
            else if (state != MeldState.None)
            {
                Global.Assert(tileA.Type.IsTile() && tileB.Type.IsTile() && tileC.Type.IsTile());
                Global.Assert((state.GetMeldType() != MeldType.Kan) || tileD.Type.IsTile());
                
                if (tileB.Called)
                {
                    Direction = CalledDirection.Across;
                    Target = owner.AddOffset(2);
                }
                else
                {
                    Direction = CalledDirection.Right;
                    Target = owner.GetNext();
                }
            }
        }

        internal void SortMeldTilesForClosedKan()
        {
            CommonHelpers.Check((State == MeldState.KanConcealed), ("Sorting closed kan tiles for a meld that isn't a closed kan! Found: " + State));

            // If there are any red tiles here then make sure they aren't on the edges.
            if (TilesRaw[0].Type.IsRedDora())
            {
                if (TilesRaw[1].Type.IsRedDora())
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
            if (TilesRaw[1].Type.IsRedDora())
            {
                // If 2 and 3 are normal, we're done. If both are red, then we're done. If 2 is red and 3 is normal, we're done.
                // Only if 2 is normal and 3 is red must we swap.
                if (!TilesRaw[2].Type.IsRedDora() && TilesRaw[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OO??.
            if (TilesRaw[2].Type.IsRedDora())
            {
                Swap(1, 2);
                // We're at OROO or OROR. If 3 is red then swap it into 2 and we're done.
                if (TilesRaw[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OOO?. If it's red swap it in.
            if (TilesRaw[3].Type.IsRedDora())
            {
                Swap(1, 3);
            }
        }

        private void Swap(int a, int b)
        {
            TileType temp = TilesRaw[a].Type;
            TilesRaw[a].Type = TilesRaw[b].Type;
            TilesRaw[b].Type = temp;
        }
    }
}
