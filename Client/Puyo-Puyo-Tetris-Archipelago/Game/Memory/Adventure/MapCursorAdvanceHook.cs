using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure
{
    /// <summary>
    /// Prevents level completion from incrementing stage counter
    /// </summary>
    public sealed class MapCursorAdvanceHook : ByteHook
    {
        private const ulong OriginalInstructionAddress = 0x1430CA653;

        public MapCursorAdvanceHook(IProcessMemory game, ILogger logger)
            : base("MapCursorAdvanceHook", unchecked((nint)OriginalInstructionAddress), game, logger, 3) // footprint = 3 (inc ax)
        { }

        // Turns the original increment into no-ops
        protected override Instruction[] BuildPatch(nint address)
        {
            return new[]
            {
                Instruction.Create(Code.Nopd),  // 0x90
                Instruction.Create(Code.Nopd),  // 0x90
                Instruction.Create(Code.Nopd),  // 0x90
            };
        }
    }
}
