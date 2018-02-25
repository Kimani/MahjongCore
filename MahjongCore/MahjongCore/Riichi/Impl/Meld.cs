// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Common.Attributes;
using MahjongCore.Riichi.Attributes;
using System;

namespace MahjongCore.Riichi
{
    public enum MeldType
    {
        None,
        Chii,
        Pon,
        Kan
    }

    public enum KanType
    {
        Open,
        Concealed,
        Promoted
    }

    public enum MeldState
    {
        [MeldTypeAttribute(MeldType.None), MeldTileCount(0), MeldOpen(false), MeldFlippedTileCount(0), MeldCalled(false), MeldCode(0), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  None,
        [MeldTypeAttribute(MeldType.Chii), MeldTileCount(3), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(1), MeldSimpleFu(0),  MeldNonSimpleFu(0)]  Chii,
        [MeldTypeAttribute(MeldType.Pon),  MeldTileCount(3), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(2), MeldSimpleFu(2),  MeldNonSimpleFu(4)]  Pon,
        [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(3), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanOpen,
        [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(false), MeldFlippedTileCount(2), MeldCalled(true),  MeldCode(4), MeldSimpleFu(16), MeldNonSimpleFu(32)] KanConcealed,
        [MeldTypeAttribute(MeldType.Kan),  MeldTileCount(4), MeldOpen(true),  MeldFlippedTileCount(0), MeldCalled(true),  MeldCode(5), MeldSimpleFu(8),  MeldNonSimpleFu(16)] KanPromoted
    }

    public static class MeldStateExtensionMethods
    {
        public static MeldType GetMeldType(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldTypeAttribute, MeldType>(ms); }
        public static bool     IsOpen(this MeldState ms)              { return EnumAttributes.GetAttributeValue<MeldOpen, bool>(ms); }
        public static bool     IsCalled(this MeldState ms)            { return EnumAttributes.GetAttributeValue<MeldCalled, bool>(ms); }
        public static int      GetTileCount(this MeldState ms)        { return EnumAttributes.GetAttributeValue<MeldTileCount, int>(ms); }
        public static int      GetFlippedTileCount(this MeldState ms) { return EnumAttributes.GetAttributeValue<MeldFlippedTileCount, int>(ms); }
        public static int      GetMeldCode(this MeldState ms)         { return EnumAttributes.GetAttributeValue<MeldCode, int>(ms); }
        public static int      GetMeldSimpleFu(this MeldState ms)     { return EnumAttributes.GetAttributeValue<MeldSimpleFu, int>(ms); }
        public static int      GetMeldNonSimpleFu(this MeldState ms)  { return EnumAttributes.GetAttributeValue<MeldNonSimpleFu, int>(ms); }

        public static bool TryGetMeldState(string value, out MeldState ms)
        {
            ms = MeldState.None;
            bool found = false;
            int code;
            if (int.TryParse(value, out code))
            {
                foreach (MeldState stateTest in Enum.GetValues(typeof(MeldState)))
                {
                    if (stateTest.GetMeldCode() == code)
                    {
                        found = true;
                        ms = stateTest;
                        break;
                    }
                }
            }
            return found;
        }
    }

    public enum CalledDirection
    {
        None,
        Left,
        Across,
        Right
    }

    public class Meld : ICloneable
    {
        public MeldState       State;
        public ExtendedTile[]  Tiles = new ExtendedTile[] { new ExtendedTile(), new ExtendedTile(), new ExtendedTile(), new ExtendedTile() };
        public int             TargetDiscardSlot = -1;

        public Meld()
        {
            Reset();
        }

        public Meld(MeldState state, CalledDirection direction, TileType a, TileType b, TileType c, TileType d)
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

        public Meld(MeldState state, ExtendedTile a, ExtendedTile b, ExtendedTile c, ExtendedTile d)
        {
            State = state;
            Tiles[0] = a;
            Tiles[1] = b;
            Tiles[2] = c;
            Tiles[3] = d;
        }

        public void Reset()
        {
            TargetDiscardSlot = -1;
            State = MeldState.None;
            Tiles[0].Reset();
            Tiles[1].Reset();
            Tiles[2].Reset();
            Tiles[3].Reset();
        }

        public void CopyFrom(Meld m)
        {
            TargetDiscardSlot = m.TargetDiscardSlot;
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

        /**
         * Multiple calls can be made that look similar that use or do not use red dora. This
         * returns the number of red dora that comprise the current call. Whether or not the
         * red dora should be revealed in this call versus a call that doesn't reveal the red
         * dora can be made.
         */
        public int RedDoraCount()
        {
            return (Tiles[0].Tile.IsRedDora() ? 1 : 0) +
                   (Tiles[1].Tile.IsRedDora() ? 1 : 0) +
                   (Tiles[2].Tile.IsRedDora() ? 1 : 0) +
                   (Tiles[3].Tile.IsRedDora() ? 1 : 0);
        }

        public bool Equals(Meld m)
        {
            return (State == m.State) &&
                   (Tiles[0].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[0].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[0].Tile.IsEqual(m.Tiles[2].Tile))) &&
                   (Tiles[1].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[1].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[1].Tile.IsEqual(m.Tiles[2].Tile))) &&
                   (Tiles[2].Tile.IsEqual(m.Tiles[0].Tile) || Tiles[2].Tile.IsEqual(m.Tiles[1].Tile) || (Tiles[2].Tile.IsEqual(m.Tiles[2].Tile)));
        }

        public TileType GetCalledTile()
        {
            return Tiles[0].Called ? Tiles[0].Tile :
                   Tiles[1].Called ? Tiles[1].Tile :
                   Tiles[2].Called ? Tiles[2].Tile :
                   Tiles[3].Called ? Tiles[3].Tile :
                                     TileType.None;
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
            Meld m = new Meld();
            m.CopyFrom(this);
            return m;
        }
    }
}
