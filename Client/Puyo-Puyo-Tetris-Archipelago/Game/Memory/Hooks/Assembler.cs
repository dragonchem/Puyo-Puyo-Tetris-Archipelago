using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// Dynamic assembler used to translate instructions into raw bytes
    /// </summary>
    internal static class Assembler
    {
        /// <summary>
        /// Turns a list of instructions into a raw byte array
        /// </summary>
        /// <returns></returns>
        public static byte[] Assemble(nint address, params Instruction[] instructions)
        {
            // initialize writers
            ByteListCodeWriter writer = new ByteListCodeWriter();
            InstructionBlock block = new InstructionBlock(writer, instructions, (ulong)address);

            // Try to encode, 64 bits, Do not fix branches, we are building caves meaning we don't want jmp instructions altered, fail loud
            if (!BlockEncoder.TryEncode(64, block, out string? errorMessage, out _, BlockEncoderOptions.DontFixBranches)) throw new InvalidOperationException($"Encoding failed at {address}, error: {errorMessage}");
            return writer.bytes.ToArray();
        }

        /// <summary>
        /// Turn a list of instructions into a byte array, and label it
        /// </summary>
        /// <param name="address"></param>
        /// <param name="instructions"></param>
        /// <param name=""></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static byte[] AssembleCave(nint address, Iced.Intel.Assembler assembler)
        {
            // Initialize writer
            ByteListCodeWriter writer = new ByteListCodeWriter();

            // assembler contains all the instructions for the cave hook
            assembler.Assemble(writer, (ulong)address);
            return writer.bytes.ToArray();
        }

        /// <summary>
        /// Iced doesn't have a default byte list writer for some reason? so this wrapper is needed
        /// </summary>
        private sealed class ByteListCodeWriter : CodeWriter
        {
            public readonly List<byte> bytes = new List<byte>();
            public override void WriteByte(byte value)
            {
                bytes.Add(value);
            }
        }
    }
}
