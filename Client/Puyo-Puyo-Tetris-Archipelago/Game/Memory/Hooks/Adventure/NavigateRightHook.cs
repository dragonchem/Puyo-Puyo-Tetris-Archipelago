using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure
{
    /// <summary>
    /// Replace the function for moving the cursor right on the adventure menu
    /// </summary>
    public sealed class NavigateRightHook : CaveHook
    {
        /// <summary>
        /// Address of the vanilla single step function tail
        /// </summary>
        private const ulong ResumeSingleStepAddress = 0x1430FAE65;
        /// <summary>
        /// Address of the specific part of the tail we want to run after 
        /// </summary>
        private const ulong ReturnSkippingTailAddress = 0x1430FAE8F;


        private const ulong OriginalInstructionAddress = 0x1430FAE5D;

        /// <summary>
        /// UI highlight moving function address
        /// </summary>
        private const ulong MoveSelectionAddress = 0x1400F7950;

        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// ebp = current act
        /// rsi = act's UI object
        /// edi = current stage
        /// r8w = intended new stage
        /// 
        /// Variables:
        /// eax = current cursor position
        /// rbx = unrelated unknown function
        /// rcx = UI selection sub-object, contents unknown
        /// edx = act number
        /// r9d = new stage
        /// 
        /// Addresses:
        /// 0x140598ED0 = current cursor position index
        /// 0x1400F7950 = UI cursor refresh function
        /// 0x1400F9070 = UI stage list refresh function
        /// ____________________________________________________
        /// movzx eax, word ptr [0x140598ED0] ; read the current cursor position
        /// mov   rbx, [rsp+0x50]             ; reload a saved register (unrelated)
        /// inc   r8w                         ; intended stage = current + 1
        /// mov word ptr [0x140598ED0], r8w   ; writes the intended new stage into the cursor position  <-  this is the line we want to replace with our cave
        /// mov   rcx, [rsi+0x28]             ; load the game's UI object for the selection <- ResumeSingleStep
        /// test  rcx, rcx                    ; null check (refreshes if not null)
        /// je    +first-refresh-skip         ; if null, skip the call below
        /// lea   r9d, [rdi+1]                ; new stage = current + 1
        /// movzx r8d, dil                    ; old stage = current
        /// movzx edx, bpl                    ; store act number in edx
        /// call  0x1400F7950                 ; move the highlighted selection (old -> new)
        /// lea   r8d, [rdi+1]                ; new stage = current + 1
        /// movzx edx, bpl                    ; store act number in edx
        /// mov   rcx, rsi                    ; store the act's UI object in rcx
        /// call  0x1400F9070                 ; second UI refresh for the new stage
        /// mov   rbp, [rsp+0x58]             ; restore saved registers <- ReturnSkippingTail
        /// mov   rsi, [rsp+0x60]             ; 
        /// add   rsp, 0x40                   ; free the function's variables
        /// pop   rdi                         ; restore saved register
        /// ret                               ; return

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public NavigateRightHook(IProcessMemory game, ILogger logger) : base("NavigateRightHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 8; // original instruction to replace is 8 bytes long
        }

        /// Cave variables:
        /// r9d = current node
        /// r9b = new node used for UI refresh
        /// r8d = new node
        /// r8b = old node used for UI refresh
        /// edx = first node of act
        /// r10d = last node of the act
        /// eax = disposable
        /// rax = disposable
        /// rcx = UI selection sub-object, contents unknown
        /// r11 = tag table base pointer
        /// dil = current stage (input)
        /// bpl = current act (input)
        /// r10b = destination stage
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // Top of loop function for node scan
            Label ScanNextNodeLoop = a.CreateLabel();

            // Node found function, called when valid node found, or 0 (1-1) to prevent crash / softlock
            Label NodeFound = a.CreateLabel();

            // If multistep skip, perform this, otherwise vanilla
            Label DoMultiStepSkip = a.CreateLabel();

            // Skips to return after the skip is done
            Label SkipDoneReturn = a.CreateLabel();

            // Ran if the move is blocked
            Label MoveBlockedReturn = a.CreateLabel();

            // set current cursor pos into r9d
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // read 16 bit value at AdventureModeCursorGlobal
            a.movzx(eax, __word_ptr[rax]); // store into eax as 32 bit value
            a.mov(r9d, eax); // copy value from eax into r9d (current node)

            // set the game's intended target node into r8d
            a.movzx(r8d, r8w); // target node = source node + 1

            // get the node range for this act
            a.lea(edx, __[rbp - 1]);
            a.lea(edx, __[rdx + rdx * 4]);
            a.add(edx, edx); // edx = (act - 1) * 10, the first node of this act
            a.lea(r10d, __[rdx + 9]); // r10d = first node + 9, the last node of this act

            // load the tag table base pointer into r11
            a.mov(r11, (ulong)StaticMemoryLocation.AdventureModeTagTable); // tag table base

            // loop right until valid tag or end of act
            a.Label(ref ScanNextNodeLoop); // mark top of scan loop
            a.cmp(r8d, r10d); // are we past the last node of the act?
            a.ja(MoveBlockedReturn); // if yes, go to MoveBlockedReturn
            a.test(r8d, r8d); // is target node 0 (1-1)?
            a.jz(NodeFound); // hard allow 0, softlock prevention
            a.test(__byte_ptr[r11 + r8 * 8 + 3], 0x80); // is bit31 set? (bit31 = our custom "stage unlocked" gate, otherwise unread by the game)
            a.jnz(NodeFound); // if yes, NodeFound
            a.inc(r8d); // if not, target node += 1
            a.jmp(ScanNextNodeLoop); // loop again

            // Node found, now decide whether to run vanilla or skip
            a.Label(ref NodeFound); // mark node found function
            a.mov(eax, r8d);
            a.sub(eax, r9d); // eax = target node - current node (amount moved)
            a.cmp(eax, 1); // is amount moved 1?
            a.jg(DoMultiStepSkip); // If yes, go to multi step skip

            // Single step, back to vanilla, just write cursor
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // rax = cursor position pointer
            a.mov(__word_ptr[rax], r8w); // write the destination node value into the cursor
            a.mov(rax, ResumeSingleStepAddress); // load address of the vanilla single step tail
            a.jmp(rax); // jump back to vanilla code

            // Multi step skip
            a.Label(ref DoMultiStepSkip); // only triggered when amount changed is over 2
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // rax = cursor position pointer
            a.mov(__word_ptr[rax], r8w); // write the destination node value into the cursor

            // Calculate the destination stage for the UI refresh (r10d unused beyond this point, disposable)
            a.mov(r10d, r8d);
            a.sub(r10d, edx);
            a.inc(r10d); // r10d = destination stage within the act, 1 to 10

            // get the UI subobject and null check
            a.mov(rcx, __[rsi + 0x28]); // rcx = selection sub-object, first argument
            a.test(rcx, rcx); // is it null?
            a.je(SkipDoneReturn); // if null, nothing to refresh

            // set up the refresh call arguments
            a.movzx(r8d, dil); // old stage = current
            a.movzx(r9d, r10b); // new stage = destination
            a.movzx(edx, bpl); // act number

            // align the stack and call the highlight-move helper
            a.mov(rax, rsp); // save the current rsp
            a.and(rsp, -16); // align the stack to 16 bytes, required by x64
            a.sub(rsp, 0x30); // reserve space for function we are about to call
            a.mov(__[rsp + 0x20], rax); // stash the saved rsp (function might overwrite rax)
            a.mov(rax, MoveSelectionAddress); // load address of the highlight move funcion
            a.call(rax); // call highlight move function
            a.mov(rsp, __[rsp + 0x20]); // restore rsp to pre-function value

            // return after the skip, also the null-check target
            a.Label(ref SkipDoneReturn); // reached from the skip body or the null check
            a.mov(rax, ReturnSkippingTailAddress); // load address of the vanilla epilogue
            a.jmp(rax); // return, skipping the single step tail

            // return without moving when the move was blocked
            a.Label(ref MoveBlockedReturn); // reached when the scan ran past the act
            a.mov(rax, ReturnSkippingTailAddress); // load address of the vanilla epilogue
            a.jmp(rax); // return, cursor left unchanged
        }
    }
}
