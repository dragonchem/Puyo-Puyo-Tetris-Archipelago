using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Translation;
using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Archipelago.Modules
{
    public class AdventureModule : IArchipelagoModule
    {
        private const int TotalStars = AdventureMemoryModule.StageCount * 3;

        private readonly IArchipelagoConnection _connection;
        private readonly AdventureState _adventureState;
        private readonly ItemLocations _itemLocations;
        private readonly ILogger _logger;

        // All 300 locations are stars; used for the goal count
        private readonly IReadOnlySet<long> _allStarLocationIds;
        
        // Map ids to indexes
        private long[]? _unlockItemId;
        private long[][]? _starLocationId;
        private bool _idsBuilt;

        // Slot data
        private IReadOnlyDictionary<string, object>? _rawSlotData;
        private SlotData? _slotData;
        private bool _goalSent;

        private readonly IGameBridge _game;
        private bool _sessionReset = false;

        public AdventureModule(
            IArchipelagoConnection connection,
            AdventureState adventureState,
            ItemLocations itemLocations,
            ILogger logger,
            IGameBridge game
        )
        {
            _connection = connection;
            _adventureState = adventureState;
            _itemLocations = itemLocations;
            _logger = logger;
            _game = game;

            _game.OnDetach += (_, _) => _sessionReset = false;

            _allStarLocationIds = new HashSet<long>(itemLocations.Locations.Values);
        }

        public void Sync()
        {
            // Build / refresh slot data, and validate
            RefreshSlotData();
            if (_slotData == null) return;

            // Get current state, and build ids
            AdventureStage[]? state = _adventureState.State;
            if (state == null) return;

            BuildIds(state);
            if (_unlockItemId == null) return;
            if (_starLocationId == null) return;

            if (!_sessionReset)
            {
                _adventureState.Reset();
                _sessionReset = true;
                return;
            }

            List<long> locationsToSend = new List<long>();

            // Set unlocked stages
            for (int i = 0; i < state.Length; i++)
            {
                // Unlocks a stage whose unlock item we have received
                long unlockItemId = _unlockItemId[i];
                if (unlockItemId == 0 || _connection.ReceivedItemIds.Contains(unlockItemId))
                    _adventureState.SetDesiredUnlocked(i, true);
                else if (!_connection.ReceivedItemIds.Contains(unlockItemId))
                    _adventureState.SetDesiredUnlocked(i, false);

                // Sets the stars if previously cleared and checks if stars are set by the game if not
                long[] starIds = _starLocationId[i];

                // First restore highest star known for stage
                int amountOfStars = 0;
                for (int s = 0; s < starIds.Length; s++)
                {
                    if (_connection.CheckedLocations.Contains(_starLocationId[i][s]))
                        amountOfStars = s;
                }

                if (amountOfStars > 0)
                    _adventureState.SetDesiredStars(i, amountOfStars);

                // Send cleared but unsent stars
                int clearedStars = state[i].Stars;

                for (int s = 1; s <= clearedStars; s++)
                {
                    long locId = starIds[s - 1];
                    if (_connection.AllLocations.Contains(locId) && !_connection.CheckedLocations.Contains(locId))
                        locationsToSend.Add(locId);
                }
            }

            // Send the locations to archipelago
            if (locationsToSend.Count > 0)
                _connection.SendLocations(locationsToSend);

            // Check if goal is sent
            if (_goalSent) return;

            // How many stars are currently cleared
            int starsCleared = 0;
            foreach (long id in _connection.CheckedLocations)
                if (_allStarLocationIds.Contains(id))
                    starsCleared++;

            // How many are needed to complete the game
            int neededToClear = (int)Math.Ceiling(_slotData.GoalPercentage / 100.0 * TotalStars);


            if (starsCleared >= neededToClear)
            {
                _connection.SendGoal();
                _goalSent = true;
                _logger.LogInformation(
                    "Goal reached: {Checked}/{Needed} stars checked.", starsCleared, neededToClear);
            }
        }


        /// <summary>
        /// Check if slot data has changed, re-load if it has
        /// </summary>
        private void RefreshSlotData()
        {
            IReadOnlyDictionary<string, object>? raw = _connection.SlotData;
            if (ReferenceEquals(raw, _rawSlotData)) return;

            _rawSlotData = raw;
            _slotData = raw == null ? null : new SlotData(raw);
            _goalSent = false;
            _sessionReset = false;
        }

        /// <summary>
        /// Builds the list of unlock item ids and star locations to indexes
        /// </summary>
        /// <param name="state"></param>
        private void BuildIds(AdventureStage[] state)
        {
            // If already built, simply return
            if (_idsBuilt) return;

            // Initialize
            _unlockItemId = new long[state.Length];
            _starLocationId = new long[state.Length][];

            for (int i = 0; i < state.Length; i++)
            {
                int act = state[i].Act;
                int stage = state[i].Stage;

                // Find item id for unlock, 1-1 always unlocked
                if (act == 1 && stage == 1) _unlockItemId[i] = 0;
                else _unlockItemId[i] = _itemLocations.Items[$"Unlock Stage {act}-{stage}"];

                // Build the star locations
                long[] stars = new long[3];
                for (int s = 0; s <= 2; s++)
                    stars[s] = _itemLocations.Locations[$"Stage {act}-{stage} - Star {s + 1}"];
                _starLocationId[i] = stars;
            }

            _idsBuilt = true;
        }
    }
}
