using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// The current state of a game function hook
    /// </summary>
    public enum HookState
    {
        /// <summary>
        /// Hook is not yet installed, nor do we know the location for the hook yet
        /// </summary>
        Unknown,
        /// <summary>
        /// Location for the hook found, not installed yet
        /// </summary>
        Ready,
        /// <summary>
        /// Hook installed and active
        /// </summary>
        Armed,
        /// <summary>
        /// Hook installed, but somehow differs from expected
        /// </summary>
        Degraded
    }
}
