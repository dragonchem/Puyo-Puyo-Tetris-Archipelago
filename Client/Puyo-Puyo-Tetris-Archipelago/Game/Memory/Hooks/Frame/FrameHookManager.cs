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
        private readonly IHookProvider _hookProvider;

        /// <summary>
        /// The detour hooks we want to call
        /// </summary>
        private List<IGameHook>? _hooks = null;

        public FrameHookManager(IGameBridge game, ILogger<FrameHookManager> logger, IHookProvider hookProvider)
        {
            _game = game;
            _logger = logger;
            _hookProvider = hookProvider;
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

            if (_hooks == null) _hooks = _hookProvider.Build(_game.Game);

            foreach (IGameHook hook in _hooks)
            {
                ProgressHook(hook);
            }
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
        }
    }
}
