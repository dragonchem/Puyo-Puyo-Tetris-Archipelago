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
        public ProcessMemory _processMemory;
        private readonly string _processName;

        // Default init
        public GameProcessMemory(string processName, bool trust)
        {
            _processName = processName;
            _processMemory = new ProcessMemory(processName, false);
        }

        // Direct forward
        public byte[] ReadByteArray(nint address, uint length)
        {
            return _processMemory.ReadByteArray(address, length);
        }

        // Direct forward
        public ulong ReadUInt64(nint address)
        {
            return _processMemory.ReadUInt64(address);
        }

        // Direct forward (needs out statement, transferred to direct return)
        public nint Traverse(nint address, params int[] offsets)
        {
            _processMemory.Traverse(address, offsets, out nint results);
            return results;
        }

        // Direct forward
        public void WriteByteArray(nint address, byte[] bytes)
        {
            _processMemory.WriteByteArray(address, bytes);
        }

        // Allocate a page of memory for caves
        public nint AllocNear(nint address, int size)
        {
            // Load address into memory
            long baseAddress = _processMemory.getBaseAddress;

            // Get the base address of the page
            nint caveAddress = _processMemory.VirtualAlloc((nuint)size);

            // Validate
            if (caveAddress == nint.Zero) throw new InvalidOperationException("Cave allocation failed");

            return caveAddress;
        }

        public void FreeMemory(nint address)
        {
            _processMemory.VirtualFree(address);
        }
    }
}
