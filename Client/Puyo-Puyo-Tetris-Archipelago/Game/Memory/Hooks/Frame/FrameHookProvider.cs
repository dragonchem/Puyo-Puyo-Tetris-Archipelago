using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Puyo_Puyo_Tetris_Archipelago.Game.Memory.Hooks.Frame
{
    /// <summary>
    /// Provides all the hooks that need to run every frame
    /// </summary>
    public sealed class FrameHookProvider : IHookProvider
    {
        private readonly ILogger<FrameHookProvider> _logger;

        public FrameHookProvider(ILogger<FrameHookProvider> logger)
        {
            _logger = logger;
        }
        private List<CodeCave> BuildFrameHooks(IProcessMemory game) => new()
        {
            new ActivateMapRefreshHook(game, _logger)
        };

        public List<IGameHook> Build(IProcessMemory game)
        {
            List<CodeCave> frameHooks = BuildFrameHooks(game);

            if (frameHooks.Count == 0) return new List<IGameHook>();

            return new List<IGameHook>()
            {
                new FrameDispatcherHook(game, _logger, frameHooks),  // runs the frame hooks every frame
            };
        }
    }
}
