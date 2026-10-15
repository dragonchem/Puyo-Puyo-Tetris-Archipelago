using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure
{
    /// <summary>
    /// Replace the function that determines if a stage should be shown as dark or light
    /// </summary>
    public sealed class StageGrayOutHook : CaveHook
    {
        private const ulong OriginalInstructionAddress = 0x143076486;

        /// <summary>
        /// Address of the vanilla instruction right after our footprint, where we resume
        /// </summary>
        private const ulong ResumeAddress = 0x14307648C;

        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// r14 = this node's tag table entry (tag base + node * 8), set at 0x14307647E
        /// rbx = this node's render object
        ///
        /// Variables:
        /// eax = node flags byte, reduced to the lit boolean in al
        ///
        /// Addresses:
        /// 0x1405992F8 = tag table base
        /// ____________________________________________________
        /// lea   r14, [r8+rax*8]            ; r14 = tag table base + node * 8 (the node's 8-byte entry)
        /// sete  sil                        ; unrelated flag from an earlier compare
        /// movzx eax, byte ptr [r14]        ; eax = flags byte[node*8+0]  <-  this is the start of the 6 bytes we replace with our cave
        /// and   al, 1                      ; bit0 = vanilla "available / lit" (also part of our footprint)
        /// je    0x143076566                ; bit0 == 0 -> draw the stage locked / frontier <- Resume
        /// mov   [rbx+0x74], al             ; else mark the stage lit (al = 1)
        /// mov   byte ptr [rbx+0x75], 0     ; clear the secondary flag
        /// jmp   0x14307659B                ; continue the renderer

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public StageGrayOutHook(IProcessMemory game, ILogger logger) : base("StageGrayOutHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 6; // original instructions to replace are 6 bytes long
        }

        /// Cave variables:
        /// eax = node flags, reduced to the bit31 lit boolean in al
        /// al  = lit flag (0 or 1), stored by the downstream mov [rbx+0x74], al
        /// r14 = tag table entry pointer (input, left untouched)
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // re-key the lit flag from bit0 to our bit31 (r14 = tag entry, untouched by our footprint; eax is vanilla scratch here)
            a.movzx(eax, __byte_ptr[r14 + 3]); // read the high byte of the flags dword, bit31 sits in bit7
            a.shr(al, 7);      // al = bit31, 0 or 1 (the shift also sets ZF when the result is 0, stage locked)
            a.jmp(ResumeAddress); // jump back to vanilla code, the je reads the ZF we just set
        }
    }
}
