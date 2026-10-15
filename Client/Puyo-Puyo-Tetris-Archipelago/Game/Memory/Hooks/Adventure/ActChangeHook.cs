using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using static Iced.Intel.AssemblerRegisters;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure
{
    /// <summary>
    /// Replace the function that changes which act you're on, redirect to closest available act in that direction
    /// </summary>
    public sealed class ActChangeHook : CaveHook
    {
        private const ulong OriginalInstructionAddress = 0x1400E6864;

        /// <summary>
        /// Address of the vanilla function tail
        /// </summary>
        private const ulong ResumeAddress = 0x1400E686B;

        /// Original instructions:
        /// ___________________________________________________
        /// Inputs:
        /// cx = intended destination node (flat index the game wants to jump to in the new act)
        ///
        /// Variables (read after we return):
        /// rdx.dl = destination act, patched on the stack at [rsp+0x48]
        /// r8.r8b = destination stage, patched on the stack at [rsp+0x28]
        ///
        /// Addresses:
        /// 0x140598ED0 = current cursor position index
        /// 0x1405992F8 = tag table base
        /// 0x1400E6340 = vanilla continuation that commits the act change
        /// ____________________________________________________
        /// cmp   r10b, dl                   ; clamp the stage column to the destination act's range
        /// jbe   +clamp-done                ; column already within range, skip the clamp
        /// movzx edx, r10b                  ; edx = stage count of the act
        /// cmp   edx, 0xA                   ; more than 10 stages?
        /// jl    +clamp-done                ; if fewer, keep it
        /// mov   edx, 0xA                   ; otherwise cap at 10
        /// dec   cx                         ; +clamp-done: start folding (act, stage) into a flat node index
        /// movzx r8d, r10b                  ; r8b = destination stage (read again after we return)
        /// movzx eax, cx                    ; eax = act - 1
        /// shl   ax, 2                      ; * 4
        /// add   cx, ax                     ; cx = (act - 1) * 5
        /// add   cx, cx                     ; cx = (act - 1) * 10
        /// dec   cx                         ;
        /// add   cx, dx                     ; cx = flat destination node = (act - 1) * 10 + stage
        /// movzx edx, r9b                   ; dl = destination act (read again after we return)
        /// mov word ptr [0x140598ED0], cx   ; writes the intended destination node into the cursor  <-  this is the line we want to replace with our cave
        /// call  0x1400E6340                ; vanilla continuation, reads rdx.dl / r8.r8b as the act/stage to display <- Resume
        /// mov   rax, [rbx+0xC8]            ; load the act-change sub-object
        /// mov   rcx, [rax+0x28]            ; load a handler pointer from it
        /// test  rcx, rcx                   ; null check
        /// je    +no-handler               ; if null, skip the tail call
        /// add   rsp, 0x20                  ; free this frame's shadow space
        /// pop   rbx                        ; restore rbx
        /// jmp   0x1400F72E0                ; tail-call the handler
        /// add   rsp, 0x20                  ; +no-handler: free this frame's shadow space
        /// pop   rbx                        ; restore rbx
        /// ret                              ; return

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public ActChangeHook(IProcessMemory game, ILogger logger) : base("ActChangeHook", unchecked((nint)OriginalInstructionAddress), game, logger)
        {
            HookLength = 7; // original instruction to replace is 7 bytes long
        }

        /// Cave variables:
        /// rsi  = tag table base pointer
        /// r11d = working destination node (starts as the intended one, ends as the remapped one)
        /// ebp  = destination act
        /// ebx  = destination column
        /// r8d  = search direction, +1 or -1
        /// r9d  = best node found so far
        /// r10d = best cost found so far
        /// ecx  = outer loop act index
        /// edx  = inner loop stage index
        /// edi  = base cost for the current act (act distance in the travel direction)
        /// r13d = flat index of the node under inspection
        /// r12d = disposable scratch for the cost math
        /// eax  = disposable
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // Dest already out of range, just write the vanilla value
            Label Keep = a.CreateLabel();

            // Direction resolved, continue setup
            Label DirSet = a.CreateLabel();

            // Top of the outer loop over acts
            Label ActLoop = a.CreateLabel();

            // Act distance already non-negative, skip the fold
            Label BasePositive = a.CreateLabel();

            // Top of the inner loop over stages
            Label StageLoop = a.CreateLabel();

            // Node is granted (or node 0), evaluate its cost
            Label KeepNode = a.CreateLabel();

            // Advance to the next stage / next act
            Label StageNext = a.CreateLabel();

            // Commit the remapped destination and restore registers
            Label Restore = a.CreateLabel();

            // save all registers and flags, we are running inside a live function
            a.pushfq();        // save flags
            a.push(rax);       // save rax
            a.push(rbx);       // save rbx
            a.push(rcx);       // save rcx
            a.push(rdx);       // save rdx (its dl is the destination act we patch later)
            a.push(rsi);       // save rsi
            a.push(rdi);       // save rdi
            a.push(rbp);       // save rbp
            a.push(r8);        // save r8 (its r8b is the destination stage we patch later)
            a.push(r9);        // save r9
            a.push(r10);       // save r10
            a.push(r11);       // save r11
            a.push(r12);       // save r12
            a.push(r13);       // save r13

            // load the tag table base pointer into rsi
            a.mov(rsi, (ulong)StaticMemoryLocation.AdventureModeTagTable); // tag table base

            // take the game's intended destination node, bail if it is out of range
            a.movzx(r11d, cx); // r11d = intended destination node
            a.cmp(r11d, 100);  // past the last node (10 acts * 10 stages)?
            a.jae(Keep);       // if yes, leave it to vanilla, don't index past the table

            // split the destination node into act (ebp) and column (ebx)
            a.mov(eax, r11d);  // eax = intended node
            a.xor(edx, edx);   // clear the high dividend
            a.mov(ecx, 10);    // divide by 10 (stages per act)
            a.div(ecx);        // eax = dest act, edx = dest column
            a.mov(ebp, eax);   // ebp = destination act
            a.mov(ebx, edx);   // ebx = destination column

            // read the current cursor and reduce it to the act we are leaving
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // read 16 bit value at AdventureModeCursorGlobal
            a.movzx(eax, __word_ptr[rax]); // store into eax as 32 bit value (current cursor node)
            a.xor(edx, edx);   // clear the high dividend
            a.mov(ecx, 10);    // divide by 10
            a.div(ecx);        // eax = old act

            // direction of travel = sign of (dest act - old act)
            a.mov(r8d, 1);     // assume we are moving forward
            a.cmp(ebp, eax);   // dest act vs old act
            a.jge(DirSet);     // dest act >= old act, keep +1
            a.mov(r8d, -1);    // otherwise we are moving backward
            a.Label(ref DirSet); // direction resolved

            // best so far: node 0 is always eligible below, so this is always overwritten before use
            a.mov(r10d, 0x7fffffff); // best cost = +infinity

            // outer loop over all acts, ecx = current act index from 0
            a.xor(ecx, ecx);   // ecx = 0
            a.Label(ref ActLoop); // top of the act loop

            // act distance in the travel direction, folded so "wrong way" acts cost more
            a.mov(eax, ecx);   // eax = this act index
            a.sub(eax, ebp);   // eax = this act - dest act
            a.imul(eax, r8d);  // * direction, so same-direction acts are non-negative
            a.test(eax, eax);  // is it already >= 0?
            a.jns(BasePositive); // if yes, keep it
            a.neg(eax);        // wrong direction: make it positive
            a.add(eax, 10);    // and push it past every same-direction act
            a.Label(ref BasePositive); // act distance is now non-negative
            a.mov(edi, eax);   // edi = base cost for this act

            // inner loop over the 10 stages, edx = current stage index from 0
            a.xor(edx, edx);   // edx = 0
            a.Label(ref StageLoop); // top of the stage loop

            // r13d = flat node index = act * 10 + stage
            a.mov(eax, ecx);   // eax = act index
            a.imul(eax, eax, 10); // * 10
            a.add(eax, edx);   // + stage index
            a.mov(r13d, eax);  // r13d = flat node index
            a.movsxd(rax, eax); // sign-extend for use as a table index

            // node 0 (1-1) is always eligible, otherwise require bit31
            a.test(r13d, r13d); // is this node 0?
            a.jz(KeepNode);    // hard allow 0, softlock prevention
            a.test(__byte_ptr[rsi + rax * 8 + 3], 0x80); // is bit31 set? (bit31 = our custom "stage unlocked" gate, otherwise unread by the game)
            a.jz(StageNext);   // locked, skip this node
            a.Label(ref KeepNode); // node is eligible, score it

            // column distance = abs(stage - dest column)
            a.mov(eax, edx);   // eax = this stage
            a.sub(eax, ebx);   // - dest column
            a.mov(r12d, eax);  // copy for the sign mask
            a.sar(r12d, 31);   // r12d = all-ones if negative, else 0
            a.xor(eax, r12d);  // conditional bit-flip
            a.sub(eax, r12d);  // eax = abs(stage - dest column)

            // total cost = base act cost * 16 + column distance (act distance dominates)
            a.mov(r12d, edi);  // r12d = base act cost
            a.shl(r12d, 4);    // * 16
            a.add(r12d, eax);  // + column distance

            // keep it only if strictly cheaper (ties keep the lower index, scanned first)
            a.cmp(r12d, r10d); // new cost vs best cost
            a.jae(StageNext);  // not cheaper, skip
            a.mov(r10d, r12d); // new best cost
            a.mov(r9d, r13d);  // new best node

            // advance the inner loop, then the outer loop
            a.Label(ref StageNext); // next stage
            a.inc(edx);        // stage += 1
            a.cmp(edx, 10);    // done all 10 stages?
            a.jb(StageLoop);   // if not, loop
            a.inc(ecx);        // act += 1
            a.cmp(ecx, 10);    // done all 10 acts?
            a.jb(ActLoop);     // if not, loop

            // remap the destination to the best node we found (node 0 guarantees r9d was set)
            a.mov(r11d, r9d);  // r11d = remapped node

            // write the remapped node into the cursor
            a.mov(cx, r11w);   // cx = remapped node
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // rax = cursor position pointer
            a.mov(__word_ptr[rax], cx); // write the destination node value into the cursor

            // split the remapped node into act / stage and patch the downstream stack slots
            a.movzx(eax, r11w); // eax = remapped node
            a.xor(edx, edx);   // clear the high dividend
            a.mov(ecx, 10);    // divide by 10
            a.div(ecx);        // eax = act, edx = stage
            a.inc(eax);        // display act is 1-based
            a.inc(edx);        // display stage is 1-based
            a.mov(__byte_ptr[rsp + 0x48], al); // saved rdx slot -> dl becomes the destination act
            a.mov(__byte_ptr[rsp + 0x28], dl); // saved r8 slot  -> r8b becomes the destination stage
            a.jmp(Restore);    // done remapping

            // keep path: write the unmodified intended node, leave vanilla's act/stage untouched
            a.Label(ref Keep); // reached only when the intended node was out of range
            a.mov(cx, r11w);   // cx = original intended node
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeCursorGlobal); // rax = cursor position pointer
            a.mov(__word_ptr[rax], cx); // write the destination node value into the cursor

            // restore every register and the flags, then resume vanilla
            a.Label(ref Restore); // shared restore tail
            a.pop(r13);        // restore r13
            a.pop(r12);        // restore r12
            a.pop(r11);        // restore r11
            a.pop(r10);        // restore r10
            a.pop(r9);         // restore r9
            a.pop(r8);         // restore r8 (r8b now holds the destination stage)
            a.pop(rbp);        // restore rbp
            a.pop(rdi);        // restore rdi
            a.pop(rsi);        // restore rsi
            a.pop(rdx);        // restore rdx (dl now holds the destination act)
            a.pop(rcx);        // restore rcx
            a.pop(rbx);        // restore rbx
            a.add(rsp, 8);     // drop the saved rax slot (rax is disposable)
            a.popfq();         // restore flags
            a.mov(rax, ResumeAddress); // load address of the vanilla continuation
            a.jmp(rax);        // jump back to vanilla code
        }
    }
}
