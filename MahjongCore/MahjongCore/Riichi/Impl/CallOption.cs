// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi
{
    public class CallOption
    {
        public CalledDirection Called;
        public MeldState Type;
        public TileType TileA;
        public TileType TileB;
        public TileType TileC;
        public TileType TileD;
        public int SlotA;
        public int SlotB;
        public int SlotC;
        public int SlotD = -1;

        private CallOption(CalledDirection called, MeldState type, TileType tA, TileType tB, TileType tC, TileType tD, int sA, int sB, int sC)
        {
            Called = called;
            Type = type;
            TileA = tA;
            TileB = tB;
            TileC = tC;
            TileD = tD;
            SlotA = sA;
            SlotB = sB;
            SlotC = sC;
        }

        private CallOption(TileType tA, TileType tB, TileType tC, TileType tD, int sA, int sB, int sC, int sD)
        {
            Called = CalledDirection.None;
            Type = MeldState.KanConcealed;
            TileA = tA;
            TileB = tB;
            TileC = tC;
            TileD = tD;
            SlotA = sA;
            SlotB = sB;
            SlotC = sC;
            SlotD = sD;
        }

        public TileType GetTile(int slot)
        {
            RiichiGlobal.Assert((slot >= 0) && (slot < 4));
            return (slot == 0) ? TileA :
                   (slot == 1) ? TileB :
                   (slot == 2) ? TileC :
                                 TileD;
        }

        public int GetSlot(int slot)
        {
            RiichiGlobal.Assert((slot >= 0) && (slot < 4));
            return (slot == 0) ? SlotA :
                   (slot == 1) ? SlotB :
                   (slot == 2) ? SlotC :
                                 SlotD;
        }

        public bool IsEqual(Meld m)
        {
            // Just check the first three.
            return (Type == m.State) &&
                   TileA.IsEqual(m.Tiles[0].Tile) &&
                   TileB.IsEqual(m.Tiles[1].Tile) &&
                   TileC.IsEqual(m.Tiles[2].Tile);
        }

        public static CallOption GetInstruction(MeldState state, TileType tA, TileType tB, TileType tC, TileType tD)
        {
            return new CallOption(CalledDirection.None, state, tA, tB, tC, tD, -1, -1, -1);
        }

        public static CallOption GetClosedKan(TileType tA, TileType tB, TileType tC, TileType tD, int sA, int sB, int sC, int sD)
        {
            return new CallOption(tA, tB, tC, tD, sA, sB, sC, sD);
        }

        public static CallOption GetChii(TileType tileCalled, TileType tileLo, TileType tileHi, int slotA, int slotB)
        {
            return new CallOption(CalledDirection.Left, MeldState.Chii, tileCalled, tileLo, tileHi, TileType.None, slotA, slotB, -1);
        }

        public static CallOption GetPon(CalledDirection Called, TileType tileCalled, TileType tileA, TileType tileB, int slotA, int slotB)
        {
            RiichiGlobal.Assert(Called != CalledDirection.None);
            return (Called == CalledDirection.Left)   ? new CallOption(CalledDirection.Left, MeldState.Pon, tileCalled, tileA, tileB, TileType.None, slotA, slotB, -1) :
                   (Called == CalledDirection.Across) ? new CallOption(CalledDirection.Across, MeldState.Pon, tileA, tileCalled, tileB, TileType.None, slotA, slotB, -1) :
                                                        new CallOption(CalledDirection.Right, MeldState.Pon, tileA, tileB, tileCalled, TileType.None, slotA, slotB, -1);
        }

        public static CallOption GetKan(CalledDirection Called, TileType tileCalled, TileType tileA, TileType tileB, TileType tileC, int slotA, int slotB, int slotC) // One of them might be like, a red 5. So we want to know which one.
        {
            RiichiGlobal.Assert(Called != CalledDirection.None);
            return (Called == CalledDirection.Left)   ? new CallOption(CalledDirection.Left, MeldState.KanOpen, tileCalled, tileA, tileB, tileC, slotA, slotB, slotC) :
                   (Called == CalledDirection.Across) ? new CallOption(CalledDirection.Across, MeldState.KanOpen, tileA, tileCalled, tileB, tileC, slotA, slotB, slotC) :
                                                        new CallOption(CalledDirection.Right, MeldState.KanOpen, tileA, tileB, tileC, tileCalled, slotA, slotB, slotC);
        }

        // ICloneable
        public CallOption Clone()
        {
            CallOption co = new CallOption(TileA, TileB, TileC, TileD, SlotA, SlotB, SlotC, SlotD);
            co.Called = Called;
            co.Type = Type;
            return co;
        }
    }
}
