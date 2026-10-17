using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure
{
    /// <summary>
    /// Replace the function that unlocks 1-2 (new!) when 1-1 is set to cleared
    /// </summary>
    public sealed class StageNewSkipHook : CaveHook
    {
        private const ulong OriginalInstructionAddress = 0x143076566;

        /// <summary>
        /// Address of the vanilla instruction right after our footprint, where we resume
        /// </summary>
        private const ulong ResumeAddress = 0x143076595;

        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// r12b = has new stage been placed var
        /// rbx  = this node's render object
        /// r14  = this node's tag table entry
        ///
        /// Addresses:
        /// 0x143076595 = plain locked path (clears the highlight / New! flags, node drawn fully grey)
        /// ____________________________________________________
        /// test  r12b, r12b                ; has a "new" stage been placed yet this pass?  <-  this is the start of the 5 bytes we replace with our cave
        /// jne   0x143076595               ; yes -> draw this locked node plain grey (also part of our footprint)
        /// movzx eax, byte ptr [r14+1]     ; first locked node: secondary flag byte
        /// mov   r12b, 1                   ; mark the "new" stage as placed
        /// ...                             ; sets [rbx+0x74]=1 (highlighted) and [rbx+0x75]=New! flag
        /// mov   word ptr [rbx+0x74], 0    ; plain locked node, highlight + New! cleared, fully grey <- Resume

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public StageNewSkipHook(IProcessMemory game, ILogger logger) : base("StageNewSkipHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 5; // original instructions to replace are 5 bytes long (test 3 + jne 2)
        }

        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // jump immediately, skipping the original test
            a.jmp(ResumeAddress); // jump back to vanilla code (rel32 reaches, cave is allocated near the site)
        }
    }
}
