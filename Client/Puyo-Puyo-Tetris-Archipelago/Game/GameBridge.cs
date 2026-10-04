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
        private readonly ILogger<GameBridge> _logger;
        // The process name to target
        public const string ProcessName = "puyopuyotetris";
        // Reference to the game
        public ProcessMemory? Game { get; private set; }
        // Is the game currently attached
        public bool Attached { get; private set; } = false;
        // Game installation directory
        public string InstallDir { get; private set; } = string.Empty;

        // Events
        public event EventHandler? OnAttach;
        public event EventHandler? OnDetach;


        public GameBridge(ILogger<GameBridge> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Start the attach loop, which will periodically check if the game is running
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAttachLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Delay for 500ms before checking again, but allow cancellation (catch to avoid crash)
                try
                {
                    await Task.Delay(500, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (Attached)
                {
                    if (!VerifyAttached()) Detach();
                }
                else
                {
                    TryAttach();
                }
            }
        }

        /// <summary>
        /// Check if game is running
        /// </summary>
        /// <returns></returns>
        public static bool IsGameRunning()
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            return processes.Length > 0;
        }

        /// <summary>
        /// Try to return the installation directory, or null if not attached
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Verify the game is attached using a memory address that's never zero (pentimino pointer)
        /// </summary>
        public bool VerifyAttached()
        {
            if (Game == null) return false;
            try { return Game.ReadUInt64(StaticMemoryLocation.PentiminoPtr.Ptr()) != 0; }
            catch { return false; }
        }

        /// <summary>
        /// Tries to attach to the game process, if successful Game is no longer null
        /// </summary>
        private void TryAttach()
        {
            using Process? gameProcess = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (gameProcess == null) return;

            Game = new ProcessMemory(ProcessName, false);
            if (!VerifyAttached())
            {
                Game = null; // process exists but isn't the build we expect (or isn't ready yet)
                return;
            }

            Attached = true;
            InstallDir = TryGetInstallDir(gameProcess) ?? string.Empty;
            _logger.LogInformation("Successfully attached to game process {Pid}", gameProcess.Id);
            OnAttach?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Detach the game
        /// </summary>
        private void Detach()
        {
            Attached = false;
            Game = null;
            _logger.LogInformation("Game closed — detached");
            OnDetach?.Invoke(this, EventArgs.Empty);
        }
    }
}
