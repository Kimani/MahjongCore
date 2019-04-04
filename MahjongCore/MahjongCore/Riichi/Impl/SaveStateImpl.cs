// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

using MahjongCore.Riichi.Impl.SaveState;
using System;
using System.Collections.Generic;

namespace MahjongCore.Riichi.Impl
{
    public class SaveStateImpl : ISaveState
    {
        // ISaveState
        public IGameSettings               Settings       { get; internal set; }
        public IDictionary<string, string> Tags           { get; internal set; } = new Dictionary<string, string>();
        public Round                       Round          { get; internal set; }
        public bool                        Lapped         { get; internal set; }
        public int                         Player1Score   { get; internal set; }
        public int                         Player2Score   { get; internal set; }
        public int                         Player3Score   { get; internal set; }
        public int                         Player4Score   { get; internal set; }
        public int                         TilesRemaining { get; internal set; }

        public ISaveState Clone() { return new SaveStateImpl(this); }
        public string Marshal()   { return SaveStateV3.Marshal(PopulateState(), Tags); }

        // IComparable<ISaveState>
        public int CompareTo(ISaveState other) { return (other is SaveStateImpl state) ? _State.CompareTo(state._State) : (_State.GetHashCode() - other.GetHashCode()); }

        // SaveStateImpl
        private string _State;

        internal SaveStateImpl(SaveStateImpl state)
        {
            _State = state._State;
            Round = state.Round;
            Lapped = state.Lapped;
            Player1Score = state.Player1Score;
            Player2Score = state.Player2Score;
            Player3Score = state.Player3Score;
            Player4Score = state.Player4Score;
            TilesRemaining = state.TilesRemaining;
            Settings = state.Settings.Clone() as IGameSettings;
            foreach (var tuple in state.Tags) { Tags.Add(tuple.Key, tuple.Value); }
        }

        internal SaveStateImpl(GameStateImpl state)
        {
            // Set everything. Tags don't get set here, the caller will need to set tags manually.
            _State = SaveStateV3.Marshal(state);
            Round = state.Round;
            Lapped = state.Lapped;
            Player1Score = state.Player1Hand.Score;
            Player2Score = state.Player2Hand.Score;
            Player3Score = state.Player3Hand.Score;
            Player4Score = state.Player4Hand.Score;
            TilesRemaining = state.TilesRemaining;
            Settings = state.Settings;
        }

        internal SaveStateImpl(string state)
        {
            _State = state;
            SaveStateV3.LoadCommon(state, this);
        }

        internal GameStateImpl PopulateState(GameStateImpl state = null)
        {
            GameStateImpl targetState = state ?? new GameStateImpl();

            if      (SaveStateV3.Matches(_State)) { SaveStateV3.LoadState(_State, targetState); }
            else if (SaveStateV2.Matches(_State)) { SaveStateV2.LoadState(_State, targetState); }
            else                                  { throw new Exception("Unrecognized state string"); }
            return targetState;
        }
    }
}
