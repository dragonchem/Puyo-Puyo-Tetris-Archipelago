using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// Contains all logic for creating and managing code caves
    /// </summary>
    public abstract class CodeCave : GameHook
    {
        /// <summary>
        /// Minimal amount of bytes needed for a cave redirect to be possible
        /// </summary>
        protected const int MinimalDetourLength = 5;

        /// <summary>
        /// Cave length, always exactly 4kb due to memory paging
        /// </summary>
        protected const int CaveLength = 0x1000;

        /// <summary>
        /// Pointer to the start of the cave, zero if unset
        /// </summary>
        public nint CaveAddress = nint.Zero;

        protected CodeCave(string name, nint address, IProcessMemory game, ILogger logger)
            : base(name, address, game, logger)
        {
        }

        /// <summary>
        /// Allocates and writes the cave
        /// </summary>
        /// <param name="near"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public nint WriteCave(nint near)
        {
            // already written, return the existing cave
            if (CaveAddress != nint.Zero) return CaveAddress;

            // reserve a page close enough that a rel32 can reach it
            CaveAddress = _game.AllocNear(near, CaveLength);

            // assemble the body
            Iced.Intel.Assembler assembler = new Iced.Intel.Assembler(64);
            BuildCave(assembler);
            byte[] code = Assembler.AssembleCave(CaveAddress, assembler);

            // validate the body fits the page
            if (code == null || code.Length == 0) throw new InvalidOperationException($"Cave {Name} zero or null");
            if (code.Length > CaveLength) throw new InvalidOperationException($"Cave {Name} too long, {code.Length} is bigger than {CaveLength}");

            // write the body into the target
            _game.WriteByteArray(CaveAddress, code);
            return CaveAddress;
        }


        public override bool IsArmed() => CaveAddress != nint.Zero;

        protected override void ArmHook() => WriteCave(HookAddress);

        /// <summary>
        /// Frees the cave, does nothing if it was never written
        /// </summary>
        protected override void FreeHook()
        {
            if (CaveAddress == nint.Zero) return;
            _game.FreeMemory(CaveAddress);
            CaveAddress = nint.Zero;
        }

        /// <summary>
        /// Return the actual assembly instructions for the cave
        /// </summary>
        protected abstract void BuildCave(Iced.Intel.Assembler assembler);
    }
}
