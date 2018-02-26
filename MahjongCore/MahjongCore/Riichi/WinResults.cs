// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Riichi
{
    public enum WinType
    {
        Ron,
        Tsumo
    }

    public interface IWinResults
    {
        Player         WinningPlayer    { get; }
        ICandidateHand WinningHand      { get; }
        WinType        Action           { get; }
        int            ScoreHi          { get; }
        int            ScoreLo          { get; }
        int            Player1Delta     { get; }
        int            Player2Delta     { get; }
        int            Player3Delta     { get; }
        int            Player4Delta     { get; }
        int            Player1PoolDelta { get; }
        int            Player2PoolDelta { get; }
        int            Player3PoolDelta { get; }
        int            Player4PoolDelta { get; }
        bool           Limit            { get; }
    }
}
