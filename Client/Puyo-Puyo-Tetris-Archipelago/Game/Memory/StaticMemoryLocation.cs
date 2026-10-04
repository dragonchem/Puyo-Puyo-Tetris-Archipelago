using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    public enum StaticMemoryLocation : ulong
    {
        /// <summary>
        /// Known non-zero location in PPT memory (based off PPT-Sandbox), used for sanity checking process memory
        /// </summary>
        PentiminoPtr = 0x140463F20
    }

    // Extends StaticMemoryLocation to allow for easy conversion to IntPtr
    public static class StaticMemoryLocationExtensions
    {
        /// <summary>
        /// Turns a StaticMemoryLocation into a pointer to that location in memory.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static IntPtr Ptr(this StaticMemoryLocation location) => new((long)location);
    }
}
