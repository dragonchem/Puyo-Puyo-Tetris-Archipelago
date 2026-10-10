using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure
{
    /// <summary>
    /// Manages the hooks for the adventure mode, registering and keeping their state up to date
    /// </summary>
    public sealed class AdventureHookManager : IPollable
    {
        private readonly GameBridge _game;
        private readonly ILogger<AdventureHookManager> _logger;
        private List<IGameHook>? _hooks = null;
        public AdventureHookManager(GameBridge game, Logger<AdventureHookManager> logger)
        {
            _game = game;
            _logger = logger;
        }

        public void Poll()
        {
            // Check that game is attached
            if (_game.Attached || _game.Game == null) return;

            _hooks = BuildHooks();
        }

        public void DestroyAll()
        {
            if (_hooks == null) return;
            foreach (IGameHook hook in _hooks)
            {
                hook.Disarm();
                hook.Free();
            }
        }

        private List<IGameHook> BuildHooks()
        {
            return new List<IGameHook>()
            {
                
            };
        }
    }
}
