using Iced.Intel;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using static Iced.Intel.AssemblerRegisters;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Frame
{
    /// <summary>
    /// Per frame cave that initializes the variables needed for map refresh, prevents crash
    /// </summary>
    public sealed class ActivateMapRefreshHook : CodeCave
    {
        private const ulong ActivateMapKick = 0x1400F72E0;

        private const ulong MapObjectVTable = 0x140371740; // vtable of the live map object; a stage repoints the global to a child whose +0xC8 is not a map sub-object

        private const int RefreshPendingOffset = 0xB0; // u16 "refresh pending" slot on AdventureMode
        private const int MapSubObjectOffset = 0xC8; // AdventureMode -> map sub-object
        private const int MapStateObjectOffset = 0x28; // map sub-object -> state object (the kick's argument)
        private const int MapGateOffset = 0x68; // state object -> gate (0 idle, 1/2 transitioning, 3 ready)

        // Unchecked -> compiler warning of overflow, intentional conversion of int64 to nint, nint is 64 here because we compile only x64
        public ActivateMapRefreshHook(IProcessMemory game, ILogger logger)
            : base("ActivateMapRefreshHook", unchecked((nint)ActivateMapKick), game, logger)
        {
        }

        /// Cave variables (all volatile, already saved by the dispatcher):
        /// rax = scratch / absolute-address holder / call target
        /// rcx = AdventureMode* , then the kick argument (state object)
        /// rdx = map sub-object, then state object
        /// ecx(low) = pending value while testing it
        protected override void BuildCave(Iced.Intel.Assembler a)
        {
            // Finish the function
            Label done = a.CreateLabel();

            // Adventure active? If not, the instance global is a dangling pointer -> never touch it.
            // (Same check as ModeReader.IsAdventure, inlined because a cave cannot call managed code.)
            a.mov(rax, (ulong)StaticMemoryLocation.CurrentModeGlobal);
            a.cmp(__dword_ptr[rax], (int)GameMode.Adventure);
            a.jne(done);

            // instance = [AdventureModeInstanceGlobal]
            a.mov(rax, (ulong)StaticMemoryLocation.AdventureModeInstanceGlobal);
            a.mov(rcx, __qword_ptr[rax]); // rcx = AdventureMode*
            a.test(rcx, rcx);
            a.je(done);

            // map screen live? inside a stage the global points at a different object whose
            // +0xC8 is not a map sub-object, so check the vtable before dereferencing anything below
            a.mov(rax, __qword_ptr[rcx]);   // rax = vtable [instance + 0]
            a.mov(rdx, MapObjectVTable);    // rdx = live map object's vtable
            a.cmp(rax, rdx);
            a.jne(done);                    // different object (in a stage / other screen) -> leave

            // pending = u16 [instance + 0xB0]; nothing requested -> leave
            a.movzx(eax, __word_ptr[rcx + RefreshPendingOffset]);
            a.test(eax, eax);
            a.je(done);

            // sub = [instance + 0xC8]
            a.mov(rdx, __qword_ptr[rcx + MapSubObjectOffset]);
            a.test(rdx, rdx);
            a.je(done);

            // state = [sub + 0x28]  (this is the kick's argument)
            a.mov(rdx, __qword_ptr[rdx + MapStateObjectOffset]);
            a.test(rdx, rdx);
            a.je(done);

            // Only kick from the idle state. gate == 3 -> the game services the pending write itself;
            // gate == 1/2 -> a transition is already in flight. Either way, do nothing.
            a.cmp(__dword_ptr[rdx + MapGateOffset], 0);
            a.jne(done);

            // Activate: call 0x1400F72E0(rcx = state). Entry rsp % 16 == 8 (dispatcher did `sub rsp,0x20; call`),
            // so reserve 0x28 to land 16-aligned at the call with 0x20 of shadow space.
            a.mov(rcx, rdx);  // rcx = state object
            a.sub(rsp, 0x28);
            a.mov(rax, ActivateMapKick);
            a.call(rax);
            a.add(rsp, 0x28);

            a.Label(ref done);
            a.ret(); // return to the dispatcher's per-hook call site
        }
    }
}
