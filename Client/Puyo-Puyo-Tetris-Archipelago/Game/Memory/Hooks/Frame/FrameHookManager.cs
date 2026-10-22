using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Frame
{
    /// <summary>
    /// Manages all the global per-frame hooks
    /// </summary>
    public sealed class FrameHookManager : IPollable
    {
        private readonly IGameBridge _game;
        private readonly ILogger<FrameHookManager> _logger;

        /// <summary>
        /// Code caves for the functions we want to call
        /// </summary>
        private List<CodeCave>? _frameHooks = null;

        /// <summary>
        /// The detour hooks we want to call
        /// </summary>
        private List<IGameHook>? _hooks = null;

        public FrameHookManager(IGameBridge game, ILogger<FrameHookManager> logger)
        {
            _game = game;
            _logger = logger;
        }


        private void ProgressHook(IGameHook hook)
        {
            switch (hook.GetState())
            {
                case HookState.Unknown:
                    hook.FindOriginalBytes();
                    break;
                case HookState.Ready:
                    hook.Arm();
                    break;
                case HookState.Degraded:
                    hook.Disarm();
                    break;
                case HookState.Armed:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Poll()
        {
            // Check that game is attached
            if (!_game.Attached || _game.Game == null)
            {
                DestroyAll();
                return;
            }

            if (_hooks == null) _hooks = BuildHooks();

            foreach (IGameHook hook in _hooks)
            {
                ProgressHook(hook);
            }
        }

        /// <summary>
        /// The per-frame list. Add a detour-less <see cref="CodeCave"/> here to run new logic each frame, nothing
        /// else. Empty for now.
        /// </summary>
        private List<CodeCave> BuildFrameHooks(IProcessMemory game, ILogger logger) => new()
        {
            new ActivateMapRefreshHook(game, logger)
        };

        /// <summary>
        /// Builds the detour we use to trigger the code caves
        /// </summary>
        private List<IGameHook> BuildHooks()
        {
            IProcessMemory g = _game.Game ?? throw new InvalidOperationException("Can't build hooks when game is not attached");

            _frameHooks = BuildFrameHooks(g, _logger);

            if (_frameHooks.Count == 0) return new List<IGameHook>();

            return new List<IGameHook>()
            {
                new FrameDispatcherHook(g, _logger, _frameHooks),  // runs the frame hooks every frame
            };
        }

        public void DestroyAll()
        {
            if (_hooks != null)
            {
                foreach (IGameHook hook in _hooks)
                {
                    hook.Disarm();
                    hook.Free();   // the dispatcher frees its frame hook caves here
                }
                _hooks = null;
            }

            _frameHooks = null;
        }
    }
}
