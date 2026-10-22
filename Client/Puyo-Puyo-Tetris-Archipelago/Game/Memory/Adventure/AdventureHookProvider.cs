using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Adventure;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure
{
    /// <summary>
    /// Provides all the game hooks for Adventure mode
    /// </summary>
    public sealed class AdventureHookProvider : IHookProvider
    {
        private readonly ILogger<AdventureHookProvider> _logger;

        public AdventureHookProvider(ILogger<AdventureHookProvider> logger)
        {
            _logger = logger;
        }

        public List<IGameHook> Build(IProcessMemory game) => new List<IGameHook>()
        {
            new NavigateRightHook(game, _logger),
            new NavigateLeftHook(game, _logger),
            new ActChangeHook(game, _logger),
            new MouseStageHook(game, _logger),
            new StageGrayOutHook(game, _logger),
            new StageNewSkipHook(game, _logger),
        };
    }
}
