using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Modules;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Translation;
using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago
{
    /// <summary>
    /// Handles the creation and polling of all the archipelago modules
    /// </summary>
    public class ArchipelagoManager : IPollable
    {
        private readonly IArchipelagoConnection _connection;
        private readonly IReadOnlyList<IArchipelagoModule> _modules;
        private readonly ILogger _logger;
        private readonly IGameBridge _game;

        public ArchipelagoManager(
            IArchipelagoConnection connection,
            AdventureState adventureState,
            ItemLocations itemLocations,
            ILogger<AdventureModule> logger,
            IGameBridge game
        )
        {
            _connection = connection;
            _logger = logger;
            _game = game;

            _modules = new List<IArchipelagoModule> 
            {
                new AdventureModule(connection, adventureState, itemLocations, logger, _game),
            };
        }

        public void Poll()
        {
            if (!_connection.Connected) return;

            foreach (IArchipelagoModule module in _modules)
                module.Sync();
        }
    }
}
