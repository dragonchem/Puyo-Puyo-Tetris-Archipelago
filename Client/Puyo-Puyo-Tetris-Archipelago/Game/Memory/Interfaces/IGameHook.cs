using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces
{
    /// <summary>
    /// Wrapper for writing and reading hooks, makes sure the hook remains active and hardens against possible game-based RAM changes
    /// </summary>
    public interface IGameHook
    {
        /// <summary>
        /// Name of the hook / function it hooks into
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The address the hook lives at
        /// </summary>
        IntPtr Address { get; }

        /// <summary>
        /// Is the game and function ready to attach the hook?
        /// </summary>
        bool ReadyToAttach();

        /// <summary>
        /// Checks if the hook is armed or not
        /// </summary>
        /// <returns></returns>
        bool IsArmed();

        /// <summary>
        /// Gets the current state of the hook
        /// </summary>
        /// <returns></returns>
        HookState GetState();

        /// <summary>
        /// Activates the hook, returns success or failure
        /// </summary>
        /// <returns></returns>
        bool Arm();

        /// <summary>
        /// Deactivates the hook, does nothing if not currently armed
        /// </summary>
        void Disarm();

        /// <summary>
        /// Completely frees all memory currently set for the hook, does nothing if not currently armed
        /// </summary>
        void Free();

        /// <summary>
        /// Finds the original byte array before modifications
        /// </summary>
        void FindOriginalBytes();
    }
}
