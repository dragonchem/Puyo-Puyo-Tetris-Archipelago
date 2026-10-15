using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using static Iced.Intel.AssemblerRegisters;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure
{
    /// <summary>
    /// Replaces the function called on mouse hover to select a stage
    /// </summary>
    public sealed class MouseStageHook : CaveHook
    {
        private const ulong OriginalInstructionAddress = 0x1430F9330;

        /// <summary>
        /// Address of the vanilla function tail
        /// </summary>
        private const ulong ResumeAddress = 0x1430F9335;
        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// rcx = AdventureMode* (the map object)
        /// edx = target column the mouse is hovering (0-based, added straight into the node index)
        ///
        /// Variables:
        /// r8w = current cursor node (read from the global)
        /// ebp = target column (param_2, saved across the body)
        /// r14 = AdventureMode* (param_1, saved across the body)
        /// sil = current act (1-based, derived from the cursor)
        /// dil = current column (1-based, derived from the cursor)
        ///
        /// Addresses:
        /// 0x140598ED0 = current cursor position index
        /// 0x1405992F8 = tag table base
        /// 0x1400F7950 = UI highlight-move helper
        /// ____________________________________________________
        /// mov   [rsp+0x10], rbp            ; save rbp to the caller's home space  <-  this is the line we want to replace with our cave
        /// mov   [rsp+0x18], rsi            ; save rsi to home space <- Resume
        /// mov   [rsp+0x20], rdi            ; save rdi to home space
        /// push  r14                        ; save r14
        /// sub   rsp, 0x40                  ; reserve local space
        /// movzx r8d, word ptr [0x140598ED0]; r8d = current cursor node
        /// xor   edi, edi                   ;
        /// mov   ebp, edx                   ; ebp = target column (param_2)
        /// mov   r14, rcx                   ; r14 = AdventureMode* (param_1)
        /// ...                              ; derive current act (sil, 1-based) and current column (dil) from the cursor
        /// ...                              ; play a hover sound if the map has an audio slot
        /// movzx ecx, sil                   ; ecx = current act (1-based)
        /// dec   cx                         ; act - 1
        /// movzx eax, cx                    ;
        /// shl   ax, 2                      ; * 4
        /// add   cx, ax                     ; * 5
        /// add   cx, cx                     ; * 10
        /// add   cx, bp                     ; + target column
        /// mov   [0x140598ED0], cx          ; new cursor = (act - 1) * 10 + column, the node the mouse selects
        /// mov   rcx, [r14+0x28]            ; load the UI selection sub-object
        /// test  rcx, rcx                   ; null check
        /// je    +skip-refresh             ; if null, nothing to refresh
        /// ...                              ; call 0x1400F7950 to move the on-screen highlight
        /// ...                              ; restore saved registers and tail-jmp to 0x1400E9070

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public MouseStageHook(IProcessMemory game, ILogger logger) : base("MouseStageSelectHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 5; // original instruction to replace is 5 bytes long
        }

        /// Cave variables:
        /// r8d = target column (copied from edx before we clobber it for the divide)
        /// eax = target node, then disposable
        /// rax = disposable
        /// ecx = divisor, then tag table base pointer
        /// rcx = tag table base pointer
        /// edx = remainder / tag table index
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // Target node is granted (or node 0), let the vanilla selection run
            Label Allow = a.CreateLabel();

            // save the registers we touch, everything is restored on both exits so vanilla sees its inputs
            a.push(rax);       // save rax
            a.push(rcx);       // save rcx (AdventureMode*, must survive to the resume)
            a.push(rdx);       // save rdx (its edx is the target column)
            a.push(r8);        // save r8

            // stash the target column before the divide clobbers edx
            a.mov(r8d, edx);   // r8d = target column (param_2)

            // current act = cursor / 10
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // read 16 bit value at AdventureModeCursorGlobal
            a.movzx(eax, __word_ptr[rax]); // store into eax as 32 bit value (current cursor node)
            a.xor(edx, edx);   // clear the high dividend
            a.mov(ecx, 10);    // divide by 10 (stages per act)
            a.div(ecx);        // eax = current act (0-based)

            // target node = act * 10 + column (matches the vanilla (act - 1) * 10 + column, act-1 == cursor/10)
            a.imul(eax, eax, 10); // act * 10 = first node of the act
            a.add(eax, r8d);   // + target column = target node

            // out of range -> let vanilla clamp / handle it, don't veto on garbage or index past the table
            a.cmp(eax, 100);   // past the last node?
            a.jae(Allow);      // if yes, allow

            // node 0 (1-1) is the start, always selectable
            a.test(eax, eax);  // is target node 0?
            a.jz(Allow);       // hard allow 0, softlock prevention

            // load the tag table base pointer into rcx
            a.mov(rcx, (ulong)StaticMemoryLocation.AdventureModeTagTable); // tag table base
            a.mov(edx, eax);   // edx = target node (zero-extends into rdx for the index)
            a.test(__byte_ptr[rcx + rdx * 8 + 3], 0x80); // is bit31 set? (bit31 = our custom "stage unlocked" gate, otherwise unread by the game)
            a.jnz(Allow);      // if yes, allow

            // locked: restore and return without moving the cursor (frame not built yet, bare ret is safe)
            a.pop(r8);         // restore r8
            a.pop(rdx);        // restore rdx
            a.pop(rcx);        // restore rcx
            a.pop(rax);        // restore rax
            a.ret();           // return, cursor left on the last granted node

            // granted: restore, replay the displaced instruction, and fall into the vanilla body
            a.Label(ref Allow); // reached when the target node is allowed
            a.pop(r8);         // restore r8
            a.pop(rdx);        // restore rdx
            a.pop(rcx);        // restore rcx
            a.pop(rax);        // restore rax
            a.mov(__qword_ptr[rsp + 0x10], rbp); // replay the displaced "mov [rsp+0x10], rbp"
            a.jmp(ResumeAddress); // jump back to vanilla code (rel32 reaches, cave is allocated near the site)
        }
    }
}
