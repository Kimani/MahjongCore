// [Ready Design Corps] - [Mahjong Core] - Copyright 2019

using MahjongCore.Common;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    internal class BoardTemplateImpl : IBoardTemplate
    {
        // IBoardTemplate
        public IReadOnlyList<ITile> Wall         { get; private set; }
        public IReadOnlyList<ITile> Hand1        { get; private set; }
        public IReadOnlyList<ITile> Hand2        { get; private set; }
        public IReadOnlyList<ITile> Hand3        { get; private set; }
        public IReadOnlyList<ITile> Hand4        { get; private set; }
        public IReadOnlyList<ITile> Discards1    { get; private set; }
        public IReadOnlyList<ITile> Discards2    { get; private set; }
        public IReadOnlyList<ITile> Discards3    { get; private set; }
        public IReadOnlyList<ITile> Discards4    { get; private set; }
        public IReadOnlyList<IMeld> Melds        { get; private set; }
        public int                  DiscardCount { get; private set; }

        // BoardTemplateImpl
        public BoardTemplateImpl(
            IReadOnlyList<ITile> wall,
            IReadOnlyList<ITile> hand1,
            IReadOnlyList<ITile> hand2,
            IReadOnlyList<ITile> hand3,
            IReadOnlyList<ITile> hand4,
            IReadOnlyList<ITile> discards1,
            IReadOnlyList<ITile> discards2,
            IReadOnlyList<ITile> discards3,
            IReadOnlyList<ITile> discards4,
            IReadOnlyList<IMeld> melds,
            int discardCount)
        {
            Wall = CommonHelpers.SafeCopy(wall);
            Hand1 = CommonHelpers.SafeCopy(hand1);
            Hand2 = CommonHelpers.SafeCopy(hand2);
            Hand3 = CommonHelpers.SafeCopy(hand3);
            Hand4 = CommonHelpers.SafeCopy(hand4);
            Discards1 = CommonHelpers.SafeCopy(discards1);
            Discards2 = CommonHelpers.SafeCopy(discards2);
            Discards3 = CommonHelpers.SafeCopy(discards3);
            Discards4 = CommonHelpers.SafeCopy(discards4);
            Melds = CommonHelpers.SafeCopy(melds);
            DiscardCount = discardCount;
        }
    }
}
