using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    /// <summary>
    /// The base class for complex memory reading and writing.
    /// Every GameMemoryModule is responsible for a specific part of memory, referring to a specific feature / game state.
    /// 
    /// </summary>
    public abstract class GameMemoryModule
    {
        protected readonly GameBridge _gameBridge;
        private readonly ILogger<GameMemoryModule> _logger;

        protected GameMemoryModule(GameBridge gameBridge, ILogger<GameMemoryModule> logger)
        {
            _gameBridge = gameBridge;
            _logger = logger;
        }

        /// <summary>
        /// Direct reference to the attached game memory
        /// </summary>
        protected ProcessMemory Game => _gameBridge.Game ?? throw new InvalidOperationException("Game is not attached, should not be called from module");

        /// <summary>
        /// Reads the value of a pointer from the game memory at the given pointer address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected IntPtr ReadPtr(IntPtr address) => (IntPtr)Game.ReadUInt64(address);

        /// <summary>
        /// If address is a pointer to a pointer, this will recursively go down the pointer chain and return the final pointer value
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="offsets"></param>
        /// <returns></returns>
        protected IntPtr? ResolvePointers(IntPtr baseAddress, params int[] offsets)
        {
            nint result;
            Game.Traverse(baseAddress, offsets, out result);
            return result;
        }

        /// <summary>
        /// Reads an array of primitives from the game memory at the given pointer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected T[] ReadArray<T>(IntPtr address, int count) where T : unmanaged // unmanaged = struct or primitive type
        {
            int size = Unsafe.SizeOf<T>();
            byte[] raw = Game.ReadByteArray(address, (uint)(size * count));
            T[] result = new T[count];
            MemoryMarshal.Cast<byte, T>(raw).Slice(0, count).CopyTo(result);
            return result;
        }

        /// <summary>
        /// Read a struct from the game memory at the given pointer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        protected T ReadStruct<T>(IntPtr address) where T : unmanaged // unmanaged = struct or primitive type
        {
            // Read raw bytes
            byte[] raw = Game.ReadByteArray(address, (uint)Unsafe.SizeOf<T>());

            // Convert to struct
            return MemoryMarshal.Read<T>(raw);
        }

        /// <summary>
        /// Turns a struct into a byte array and writes it to the game memory at the given pointer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        protected void WriteStruct<T>(IntPtr addr, in T value) where T : unmanaged
        {
            byte[] buffer = new byte[Marshal.SizeOf<T>()];
            MemoryMarshal.Write(buffer, in value);
            Game.WriteByteArray(addr, buffer);

            _logger.LogDebug($"Writing {buffer.ToString()} to {addr}");
        }
    }
}
