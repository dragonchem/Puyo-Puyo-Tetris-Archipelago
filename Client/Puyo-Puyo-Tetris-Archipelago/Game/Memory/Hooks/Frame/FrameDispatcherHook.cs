using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using static Iced.Intel.AssemblerRegisters;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Frame
{
    /// <summary>
    /// Hook used to call all other per frame code caves
    /// </summary>
    public class FrameDispatcherHook : CaveHook
    {
        /// <summary>
        /// FUN_1457be310, the global per-frame tick
        /// </summary>
        private const ulong OriginalInstructionAddress = 0x1457BE310;

        /// <summary>
        /// Address of the vanilla function tail
        /// </summary>
        private const ulong ResumeAddress = 0x1457BE315;

        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// rcx = per-frame tick context, carried through so the resume sees exactly what vanilla would
        ///
        /// Addresses:
        /// 0x140598990 = task-list bucket head
        /// 0x1405989A0 / 0x140598978 / 0x140598A60 = task bucket bounds the pass walks
        /// ____________________________________________________
        /// mov   [rsp+8], rbx               ; save rbx to home space  <-  this is the start of the 5 bytes we replace with our cave
        /// mov   [rsp+0x10], rbp            ; save rbp to home space <- Resume
        /// mov   [rsp+0x18], rsi            ; save rsi to home space
        /// push  rdi                        ; save rdi
        /// push  r12                        ; save r12
        /// push  r13                        ; save r13
        /// push  r14                        ; save r14
        /// push  r15                        ; save r15

        private readonly IReadOnlyList<CodeCave> _frameHooks;

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public FrameDispatcherHook(IProcessMemory game, ILogger logger, IReadOnlyList<CodeCave> frameHooks)
            : base("FrameDispatcherHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 5; // original instruction to replace is 5 bytes long (mov [rsp+8],rbx, exact E9 rel32 fit)
            _frameHooks = frameHooks;
        }

        /// <summary>
        /// Write all hooks, and arm them
        /// </summary>
        protected override void ArmHook()
        {
            // Each frame hook gets its own cave, placed near the host so a rel32 reaches it
            foreach (CodeCave hook in _frameHooks) hook.WriteCave(HookAddress);

            base.ArmHook();
        }

        /// <summary>
        /// Free every frame hook cave alongside the dispatcher cave
        /// </summary>
        protected override void FreeHook()
        {
            foreach (CodeCave hook in _frameHooks) hook.Free();
            base.FreeHook();
        }

        /// Cave variables:
        /// rbp = pre-alignment frame marker (saved, so the job calls cannot lose our stack)
        /// rax = each frame hook's cave pointer, then disposable
        /// (needed for vanilla, we save: rcx, rdx, r8, r9, r10, r11, rax)
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // save flags and variables we need for 
            a.pushfq(); // save flags
            a.push(rax); // save rax
            a.push(rcx); // save rcx (per-frame tick context, must survive to the resume)
            a.push(rdx); // save rdx
            a.push(r8); // save r8
            a.push(r9); // save r9
            a.push(r10); // save r10
            a.push(r11); // save r11
            a.push(rbp); // save rbp (reused as the frame marker)

            // Keep an unaligned frame marker, then align the stack once for every call below
            a.mov(rbp, rsp); // rbp = pre-alignment rsp (saved for after calls)
            a.and(rsp, -16); // align the stack to 16 bytes, required by x64

            // Call each enabled frame hook through its own cave pointer
            foreach (CodeCave hook in _frameHooks)
            {
                a.mov(rax, (ulong)hook.CaveAddress); // rax = this frame hook's cave
                a.sub(rsp, 0x20); // reserve shadow space (rsp already 16-aligned)
                a.call(rax); // run the frame hook (it preserves the variables we saved, ends in ret to return)
                a.add(rsp, 0x20); // reclaim the shadow space
            }

            // Restore the stack, the variables and the flags
            a.mov(rsp, rbp); // drop the alignment padding
            a.pop(rbp); // restore rbp
            a.pop(r11); // restore r11
            a.pop(r10); // restore r10
            a.pop(r9); // restore r9
            a.pop(r8); // restore r8
            a.pop(rdx); // restore rdx
            a.pop(rcx); // restore rcx
            a.pop(rax); // restore rax
            a.popfq(); // restore flags

            // rbx and rsp are the untouched vanilla values, call the original instruction we replaced and go back
            a.mov(__qword_ptr[rsp + 8], rbx); // original instruction we still need to run
            a.jmp(ResumeAddress); // jump back to vanilla code (rel32 reaches, cave is allocated near the host)
        }
    }
}
