using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Puyo_Puyo_Tetris_Archipelago.Game
{
    public class GameLoop : IDisposable
    {
        private readonly GameBridge _game;
        private readonly IEnumerable<IPollable> _pollables;
        private readonly ILogger<GameLoop> _logger;

        // Allows us to send cancellations
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Timer that ticks on WPF UI thread, so we can update ui in it's thread
        private DispatcherTimer? _dispatcherTimer;

        private bool running = false;


        public GameLoop(GameBridge game, ILogger<GameLoop> logger, IEnumerable<IPollable> pollables)
        {
            _game = game;
            _logger = logger;
            _pollables = pollables;
        }

        public void Start()
        {
            if (running) return;
            running = true;

            // Start attach loop
            _ = _game.RunAttachLoop(_cancellationTokenSource.Token);

            // Set up timer tick and start timer
            _dispatcherTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _dispatcherTimer.Tick += Tick;
            _dispatcherTimer.Start();
        }

        public void Tick(object? sender, EventArgs e)
        {
            foreach (IPollable pollable in _pollables)
            {
                try
                {
                    pollable.Poll();
                    _logger.LogDebug($"Polling {pollable.GetType().Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Poll failed for {pollable.GetType().Name}");
                }
            }
        }

        public void Stop()
        {
            _dispatcherTimer?.Stop();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public void Dispose() => Stop();
    }
}
