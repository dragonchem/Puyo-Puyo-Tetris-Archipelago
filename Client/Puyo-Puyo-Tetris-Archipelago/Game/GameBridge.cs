using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game
{
    public class GameBridge
    {
        private ILogger<GameBridge> _logger;
        // The process name to target
        public const string ProcessName = "puyopuyotetris";
        // Reference to the game
        public ProcessMemory? Game { get; private set; }
        // Is the game currently attached
        public bool Attached { get; private set; } = false;
        // Game installation directory
        public string InstallDir { get; private set; } = string.Empty;

        // Events
        public EventHandler? OnAttach { get; set; }
        public EventHandler? OnDetach { get; set; }


        public GameBridge(ILogger<GameBridge> logger)
        {
            _logger = logger;
        }

        public async Task RunAttachLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Attached) break;

                Process? gameProcess = Process.GetProcessesByName(ProcessName).FirstOrDefault();
                if (gameProcess == null) continue;

                Game = new ProcessMemory(ProcessName, false);
                if (VerifyAttached())
                {
                    Attached = true;
                    _logger.LogInformation($"Successfully attached to game process: {gameProcess.Id}");

                    InstallDir = TryGetInstallDir(gameProcess) ?? string.Empty;

                    OnAttach?.Invoke(this, EventArgs.Empty);

                    break;
                }
                else
                {
                    Game = null;
                    _logger.LogWarning("Failed to attach to game process. Retrying in 500ms...");

                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        // Check if game is running
        public static bool IsGameRunning()
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            return processes.Length > 0;
        }

        // Try to return the installation directory, or null if not attached
        public static string? TryGetInstallDir(Process proc)
        {
            try
            {
                string? exe = proc.MainModule?.FileName;
                if (string.IsNullOrEmpty(exe))
                {
                    return null;
                }
                return System.IO.Path.GetDirectoryName(exe);
            }
            catch
            {
                return null;
            }
        }

        // Verify the game is attached using a memory address that's never zero
        public bool VerifyAttached()
        {
            if (Game == null) return false;
            return Game.ReadInt32(StaticMemoryLocation.PentiminoPtr.Ptr()) != 0;
        }
    }
}
