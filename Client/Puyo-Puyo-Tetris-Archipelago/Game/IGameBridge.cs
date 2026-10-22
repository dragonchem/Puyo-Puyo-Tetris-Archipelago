using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game
{
    public interface IGameBridge
    {
        public event EventHandler? OnAttach;
        public event EventHandler? OnDetach;
        public IProcessMemory? Game { get; }
        public bool Attached { get; }
        public Task RunAttachLoop(CancellationToken cancellationToken);
        public bool IsGameRunning();
        public string? TryGetInstallDir(Process proc);
        public bool VerifyAttached();
    }
}
