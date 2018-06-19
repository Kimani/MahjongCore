// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System;

namespace MahjongCore.Riichi.Impl
{
    public class MeldImpl : IMeld
    {
        // IMeld
        public Player          Owner      { get; internal set; } = Player.None;
        public Player          Target     { get; internal set; } = Player.None;
        public MeldState       State      { get; internal set; } = MeldState.None;
        public CalledDirection Direction  { get; internal set; } = CalledDirection.None;
        public ITile[]         Tiles      { get; private set; }  = new TileImpl[] { new TileImpl(), new TileImpl(), new TileImpl(), new TileImpl() };

        public ITile CalledTile
        {
            get
            {
                return Tiles[0].Called ? Tiles[0] :
                       Tiles[1].Called ? Tiles[1] :
                       Tiles[2].Called ? Tiles[2] :
                       Tiles[3].Called ? Tiles[3] :
                                         null;
            }
        }

        public int RedDoraCount
        {
            get
            {
                return (Tiles[0].Type.IsRedDora() ? 1 : 0) +
                       (Tiles[1].Type.IsRedDora() ? 1 : 0) +
                       (Tiles[2].Type.IsRedDora() ? 1 : 0) +
                       (Tiles[3].Type.IsRedDora() ? 1 : 0);
            }
        }

        public void Promote(TileType kanTile, int kanTileSlot)
        {
            if (Global.CanAssert)
            {
                Global.Assert(State == MeldState.Pon);
                Global.Assert(Target != Player.None);
                Global.Assert(Direction != CalledDirection.None);
                Global.Assert(!Tiles[3].Type.IsTile());
            }

            State = MeldState.KanPromoted;

            TileImpl tileD = GetTile(3);
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
            int value = Owner.CompareTo(other.Owner);             if (value != 0) { return value; }
            value = Target.CompareTo(other.Target);               if (value != 0) { return value; }
            value = State.CompareTo(other.State);                 if (value != 0) { return value; }
            value = Direction.CompareTo(other.Direction);         if (value != 0) { return value; }
            value = Tiles[0].Type.CompareTo(other.Tiles[0].Type); if (value != 0) { return value; }
            value = Tiles[1].Type.CompareTo(other.Tiles[1].Type); if (value != 0) { return value; }
            value = Tiles[2].Type.CompareTo(other.Tiles[2].Type); if (value != 0) { return value; }
            value = Tiles[3].Type.CompareTo(other.Tiles[3].Type); if (value != 0) { return value; }
            return 0;
        }

        // MeldImpl
        internal TileImpl GetTile(int index) { return (TileImpl)Tiles[index]; }

        internal void Set(MeldImpl meld)
        {
            Owner = meld.Owner;
            Target = meld.Target;
            State = meld.State;
            Direction = meld.Direction;
            GetTile(0).Set(meld.Tiles[0]);
            GetTile(1).Set(meld.Tiles[1]);
            GetTile(2).Set(meld.Tiles[2]);
            GetTile(3).Set(meld.Tiles[3]);
        }

        internal void SortMeldTilesForClosedKan()
        {
            Global.Assert(State == MeldState.KanConcealed);

            // If there are any red tiles here then make sure they aren't on the edges.
            if (Tiles[0].Type.IsRedDora())
            {
                if (Tiles[1].Type.IsRedDora())
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
            if (Tiles[1].Type.IsRedDora())
            {
                // If 2 and 3 are normal, we're done. If both are red, then we're done. If 2 is red and 3 is normal, we're done.
                // Only if 2 is normal and 3 is red must we swap.
                if (!Tiles[2].Type.IsRedDora() && Tiles[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OO??.
            if (Tiles[2].Type.IsRedDora())
            {
                Swap(1, 2);
                // We're at OROO or OROR. If 3 is red then swap it into 2 and we're done.
                if (Tiles[3].Type.IsRedDora())
                {
                    Swap(2, 3);
                }
                return;
            }

            // We're at OOO?. If it's red swap it in.
            if (Tiles[3].Type.IsRedDora())
            {
                Swap(1, 3);
            }
        }

        private void Swap(int a, int b)
        {
            ITile temp = Tiles[a];
            Tiles[a] = Tiles[b];
            Tiles[b] = temp;
        }
    }
}
