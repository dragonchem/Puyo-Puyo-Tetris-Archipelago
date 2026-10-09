using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks
{
    /// <summary>
    /// Static byte writer, replaces instructions without redirecting to a cave, must be same footprint as vanilla
    /// </summary>
    public abstract class ByteHook : GameHook
    {
        // The byte array of the patch, used for validation
        private byte[] _patch;

        // Footprint = the length of the original instruction at address
        protected ByteHook(string name, nint address, IProcessMemory game, ILogger logger, uint footprint) : base(name, address, game, logger)
        {
            HookLength = footprint;
            _patch = AssemblePatch();
        }

        private byte[] AssemblePatch()
        {
            // Get the bytes for the path
            byte[] patch = Assembler.Assemble(HookAddress, BuildPatch(HookAddress));

            // Validate
            if (patch.Length != HookLength) throw new ArgumentException($"Hook {Name} is mismatched: Expected {HookLength} bytes but got {patch.Length}");
            return patch;
        }

        public override bool IsArmed()
        {
            return AddressEquals(_patch);
        }

        /// <summary>
        /// Write hook to memory
        /// </summary>
        protected override void ArmHook()
        {
            _game.WriteByteArray(HookAddress, _patch);
        }

        /// <summary>
        /// Clear the hook (if known)
        /// </summary>
        protected override void DisarmHook()
        {
            if (_originalBytes == null || _game == null) return;
            _game.WriteByteArray(HookAddress, _originalBytes);
        }

        /// <summary>
        /// Only call at initialization of hook, re-calling rewrites the hook
        /// TODO: harden to make sure it's vanilla code before writing _originalBytes
        /// </summary>
        public override void FindOriginalBytes()
        {
            _originalBytes = ReadHookLocation();
        }

        /// <summary>
        /// The actual patch itself, set in child class
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected abstract Instruction[] BuildPatch(nint address);
    }
}
