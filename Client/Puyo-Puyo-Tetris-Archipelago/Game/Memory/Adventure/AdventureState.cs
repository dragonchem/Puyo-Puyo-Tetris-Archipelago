using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure.Adventure
{
    public class AdventureState : IPollable
    {
        private readonly AdventureMemoryModule _memoryModule;
        private readonly GameBridge _game;
        public AdventureStage[]? State;

        /// <summary>
        /// The actual list of stages that should be unlocked at this state
        /// </summary>
        private bool[] _desiredUnlocked = new bool[AdventureMemoryModule.StageCount];

        /// <summary>
        /// The desired list of stars, should be kept up to date, only used to restore stars in case of a restart
        /// </summary>
        private int[] _desiredStars = new int[AdventureMemoryModule.StageCount];

        /// <summary>
        /// Alerts whenever a stage's state gets changed, for both UI and AP
        /// </summary>
        public event EventHandler<AdventureStage[]>? StateChanged;

        public AdventureState(AdventureMemoryModule mem, GameBridge game)
        {
            _memoryModule = mem;
            _game = game;

            // 1-1 always unlocked
            _desiredUnlocked[0] = true;
        }

        /// <summary>
        /// Opens or closes a level
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cleared"></param>
        public void SetDesiredUnlocked(int index, bool cleared)
        {
            _desiredUnlocked[index] = cleared;
        }

        public void SetDesiredStars(int index, int stars)
        {
            if (stars < 0) throw new ArgumentException("Stars cannot be lower than 0");
            if (stars > 3) throw new ArgumentException("Stars cannot be higher than 3");

            _desiredStars[index] = stars;
        }

        /// <summary>
        /// Poll the memory for changes and trigger events if changes have occurred
        /// </summary>
        public void Poll()
        {
            // If game not attached, do not continue
            if (!_game.Attached) return;

            // Read current state of memory
            AdventureStage[] newState = _memoryModule.Read();

            // Set up the finalized state for saving into _State
            AdventureStage[] finalState = new AdventureStage[newState.Length];

            bool stageChanged = false;

            // If no last state known, we cannot determine changes
            if (State != null)
            {

                for (int i = 0; i < newState.Length; i++)
                {
                    // Get current and previous value for this stage
                    AdventureStage currStage = newState[i];
                    AdventureStage prevStage = State[i];

                    // Enforce stage clear status
                    if (currStage.GameCleared != true)
                    {
                        _memoryModule.SetGameCleared(i);
                        currStage = currStage with { GameCleared = true };
                    }

                    // Enforce stage unlocked status
                    if (currStage.ApUnlocked != _desiredUnlocked[i])
                    {
                        _memoryModule.SetApUnlocked(i, _desiredUnlocked[i]);
                        currStage = currStage with { ApUnlocked = _desiredUnlocked[i] };
                    }

                    // Enforce star status
                    if (_desiredStars[i] != currStage.Stars)
                    {
                        _memoryModule.SetStars(i, _desiredStars[i]);
                        currStage = currStage with { Stars =  _desiredStars[i] };
                    }

                    // If different, must raise event
                    if (!currStage.Equals(prevStage))
                    {
                        stageChanged = true;
                    }

                    finalState[i] = currStage;
                }
            }
            else
            {
                finalState = newState;
                stageChanged = true;
            }

            // Save the state
            State = finalState;

            if (stageChanged) StateChanged?.Invoke(this, finalState);
        }
    }
}
