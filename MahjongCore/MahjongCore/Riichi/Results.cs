// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using System.Collections.Generic;

namespace MahjongCore.Riichi
{
    public enum WinType
    {
        Ron,
        Tsumo,
        Draw
    }

    public interface ICandidateHand
    {
        int Dora         { get; }
        int UraDora      { get; }
        int RedDora      { get; }
        int Han          { get; }
        int Fu           { get; }
        int YakumanCount { get; }
        IList<Yaku> Yaku { get; }
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
        bool           Limit            { get; } // True if mangan or better.
    }

    public interface IGameResults
    {
        int       FinalPointsPlayer1    { get; } // Includes Yakitori
        int       FinalPointsPlayer2    { get; }
        int       FinalPointsPlayer3    { get; }
        int       FinalPointsPlayer4    { get; }
        int       YakitoriDeltaPlayer1  { get; }
        int       YakitoriDeltaPlayer2  { get; }
        int       YakitoriDeltaPlayer3  { get; }
        int       YakitoriDeltaPlayer4  { get; }
        Placement FinalPlacementPlayer1 { get; }
        Placement FinalPlacementPlayer2 { get; }
        Placement FinalPlacementPlayer3 { get; }
        Placement FinalPlacementPlayer4 { get; }
        float     FinalScorePlayer1     { get; }
        float     FinalScorePlayer2     { get; }
        float     FinalScorePlayer3     { get; }
        float     FinalScorePlayer4     { get; }

        int GetPoints(Player p);
        int GetPoints(Placement p);
        float GetScore(Player p);
        float GetScore(Placement p);
        Player GetPlayer(Placement p);
    }
}
