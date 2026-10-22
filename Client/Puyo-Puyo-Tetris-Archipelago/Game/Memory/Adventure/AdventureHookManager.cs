using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory
{
    /// <summary>
    /// Manages the hooks for the adventure mode, registering and keeping their state up to date
    /// </summary>
    public sealed class AdventureHookManager : IPollable
    {
        private readonly IGameBridge _game;
        private readonly ILogger<AdventureHookManager> _logger;
        private readonly IHookProvider _hookProvider;
        private List<IGameHook>? _hooks = null;
        public AdventureHookManager(IGameBridge game, ILogger<AdventureHookManager> logger, IHookProvider hookProvider)
        {
            _game = game;
            _logger = logger;
            _hookProvider = hookProvider;
        }

        public void Poll()
        {
            // Check that game is attached
            if (!_game.Attached || _game.Game == null)
            {
                DestroyAll();
                return;
            }

            if (_hooks == null) _hooks = _hookProvider.Build(_game.Game).ToList();

            foreach (IGameHook hook in _hooks)
            {
                ProgressHook(hook);
            }
        }

        private void ProgressHook(IGameHook hook)
        {
            switch (hook.GetState())
            {
                case Hooks.HookState.Unknown:
                    hook.FindOriginalBytes();
                    break;
                case Hooks.HookState.Ready:
                    hook.Arm();
                    break;
                case Hooks.HookState.Degraded:
                    hook.Disarm();
                    break;
                case Hooks.HookState.Armed:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void DestroyAll()
        {
            if (_hooks == null) return;
            foreach (IGameHook hook in _hooks)
            {
                hook.Disarm();
                hook.Free();
            }
            _hooks = null;
        }
    }
}
