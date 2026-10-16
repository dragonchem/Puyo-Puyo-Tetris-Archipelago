using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// Base class for creating hooks that use code caves to inject custom logic into functions at specific locations
    /// </summary>
    public abstract class CaveHook : CodeCave
    {
        protected CaveHook(string name, nint address, IProcessMemory game, ILogger logger) : base(name, address, game, logger)
        {
        }

        public override bool IsArmed()
        {
            if (CaveAddress == nint.Zero) return false;

            // Validate the detour at the site is still our jmp into the cave
            byte[] shouldBeRedirect = _game.ReadByteArray(HookAddress, HookLength);
            Decoder asmDecoder = Decoder.Create(64, shouldBeRedirect, (ulong)HookAddress.ToInt64());
            Instruction decodedFunc = asmDecoder.Decode();

            return decodedFunc.Mnemonic == Mnemonic.Jmp
                && decodedFunc.Op0Kind == OpKind.NearBranch64
                && decodedFunc.NearBranchTarget == (ulong)CaveAddress.ToInt64();
        }

        protected override void ArmHook()
        {
            // shared cave write (CodeCave)
            WriteCave(HookAddress);
            // + the detour
            _game.WriteByteArray(HookAddress, BuildDetour());
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
    }
}
