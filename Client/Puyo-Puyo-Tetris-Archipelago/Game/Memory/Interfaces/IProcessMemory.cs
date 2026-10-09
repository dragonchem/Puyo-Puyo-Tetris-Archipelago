using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces
{
    /// <summary>
    /// ProcessMemory wrapper so that we can unit test
    /// </summary>
    public interface IProcessMemory
    {
        /// <summary>
        /// read length amount of bytes at address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        byte[] ReadByteArray(IntPtr address, uint length);

        /// <summary>
        /// write bytes at address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bytes"></param>
        void WriteByteArray(IntPtr address, byte[] bytes);

        /// <summary>
        /// Read unsigned 64 bit integer at address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        ulong ReadUInt64(IntPtr address);

        /// <summary>
        /// Goes through the entire stack of pointers at address to read a value
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        IntPtr Traverse(IntPtr address, params int[] offsets);

        /// <summary>
        /// Allocate x size within ~2GB of target in RAM, done so we can detour asm functions without it crashing
        /// </summary>
        /// <returns></returns>
        IntPtr AllocNear(IntPtr target, int size);

        /// <summary>
        /// Frees a range of memory, make sure to ONLY use this to clear custom memory allocation done through AllocNear
        /// </summary>
        /// <param name="target"></param>
        /// <param name="size"></param>
        void FreeMemory(IntPtr target);
    }
}
