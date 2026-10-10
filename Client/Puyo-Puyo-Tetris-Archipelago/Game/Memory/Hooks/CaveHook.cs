using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System.Net.Security;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// Base class for creating hooks that use code caves to inject custom logic into functions at specific locations
    /// </summary>
    public abstract class CaveHook : GameHook
    {
        /// <summary>
        /// Minimal amount of bytes needed for a cave redirect to be possible
        /// </summary>
        private const int MinimalDetourLength = 5;

        /// <summary>
        /// Cave length, always exactly 4kb due to memory paging
        /// </summary>
        private const int CaveLength = 0x1000;

        /// <summary>
        /// Pointer to the start of the cave, zero if unset
        /// </summary>
        public nint CaveAddress = nint.Zero;
        protected CaveHook(string name, nint address, IProcessMemory game, ILogger logger) : base(name, address, game, logger)
        {
        }

        public override bool IsArmed()
        {
            // If zero, it's not set
            if (CaveAddress == nint.Zero) return false;

            // Validate that redirect is the correct bytes
            byte[] shouldBeRedirect = _game.ReadByteArray(HookAddress, HookLength);
            Decoder asmDecoder = Decoder.Create(64, shouldBeRedirect, (ulong)HookAddress.ToInt64());
            Instruction decodedFunc = asmDecoder.Decode();

            // instruction is exactly: jmp addr (direct near jump)
            return decodedFunc.Mnemonic == Mnemonic.Jmp
                && decodedFunc.Op0Kind == OpKind.NearBranch64
                && decodedFunc.NearBranchTarget == (ulong)CaveAddress.ToInt64();
        }

        protected override void ArmHook()
        {
            // Allocate memory if no cave allocated yet
            if (CaveAddress == nint.Zero) CaveAddress = _game.AllocNear(HookAddress, CaveLength);

            // Build cave
            Iced.Intel.Assembler assembler = new Iced.Intel.Assembler(64);
            BuildCave(assembler);
            byte[] newInstructions = Assembler.AssembleCave(CaveAddress, assembler);

            // Validate cave
            if (newInstructions == null || newInstructions.Length == 0) throw new InvalidOperationException("Cave zero or null");
            if (newInstructions.Length > CaveLength) throw new InvalidOperationException($"Cave {Name} too long, {newInstructions.Length} is bigger than {CaveLength}");

            // Write cave
            _game.WriteByteArray(CaveAddress, newInstructions);

            // Write detour
            _game.WriteByteArray(HookAddress, BuildDetour());
        }

        protected override void FreeHook()
        {
            if (CaveAddress == nint.Zero) return;
            _game.FreeMemory(CaveAddress);
            CaveAddress = nint.Zero;
        }

        /// <summary>
        /// Builds the jump instruction that redirects to the cave
        /// </summary>
        internal byte[] BuildDetour()
        {
            int padding = (int)HookLength - MinimalDetourLength;

            // Create instruction at hookAddress
            Instruction[] detourInstructions = new Instruction[1 + padding];

            // Jumps to cave
            detourInstructions[0] = Instruction.CreateBranch(Code.Jmp_rel32_64, (ulong)CaveAddress);

            // Pad the rest with no-ops
            for (int i = 1; i < detourInstructions.Length; i++)
                detourInstructions[i] = Instruction.Create(Code.Nopd);

            // Assemble the instructions
            byte[] hookInstructions = Assembler.Assemble(HookAddress, detourInstructions);

            // Validate
            if (hookInstructions.Length > HookLength) throw new InvalidOperationException($"Detour for {Name} is {hookInstructions.Length} bytes long, but footprint is {HookLength}");

            return hookInstructions;
        }

        /// <summary>
        /// Return the actual assembly instructions for the cave
        /// </summary>
        protected abstract void BuildCave(Iced.Intel.Assembler assembler);
    }
}
