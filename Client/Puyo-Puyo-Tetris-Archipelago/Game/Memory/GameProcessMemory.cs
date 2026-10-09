using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    /// <summary>
    /// Basic wrapper for ProcessMemory for unit testing
    /// </summary>
    public class GameProcessMemory : IProcessMemory
    {
        // Internal ProcessMemory connected to the game
        private ProcessMemory _processMemory;

        // Default init
        public GameProcessMemory(string processName, bool trust)
        {
            _processMemory = new ProcessMemory(processName, false);
        }

        // Direct forward
        public byte[] ReadByteArray(IntPtr address, uint length)
        {
            return _processMemory.ReadByteArray(address, length);
        }

        // Direct forward
        public ulong ReadUInt64(IntPtr address)
        {
            return _processMemory.ReadUInt64(address);
        }

        // Direct forward (needs out statement, transferred to direct return)
        public IntPtr Traverse(IntPtr address, params int[] offsets)
        {
            _processMemory.Traverse(address, offsets, out IntPtr results);
            return results;
        }

        // Direct forward
        public void WriteByteArray(IntPtr address, byte[] bytes)
        {
            _processMemory.WriteByteArray(address, bytes);
        }
    }
}
