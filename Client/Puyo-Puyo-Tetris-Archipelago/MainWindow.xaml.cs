using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Views;
using Puyo_Puyo_Tetris_Archipelago.Views.Classes;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class MainWindow : Window
    {
        private readonly NavigationService _clientEvents;
        private readonly ILogger<MainWindow> _logger;
        private readonly GameLoop _gameLoop;

        private readonly APTrackerView _apTrackerView;
        private readonly APSetupView _apSetupView;
        private readonly APDevMenu _devMenu;
        private readonly IGameBridge _gameBridge;
        
        // Classes in constructor are injected by dependency injection
        public MainWindow(
            NavigationService clientEvents,
            ILogger<MainWindow> logger,
            GameLoop gameLoop,
            APDevMenu apDevMenu,
            APSetupView apSetupView,
            APTrackerView apTrackerView,
            IGameBridge gameBridge
        )
        {
            // Initializes the window for rendering
            InitializeComponent();

            // Save the injected classes to private variables
            _clientEvents = clientEvents;
            _logger = logger;
            _gameLoop = gameLoop;
            _devMenu = apDevMenu;
            _apSetupView = apSetupView;
            _apTrackerView = apTrackerView;
            _gameBridge = gameBridge;

            // Show setup screen
            ViewRoot.Children.Add(_apSetupView);

            // Subscribe to events
            _clientEvents.OnViewChange += OnViewChange;

            _logger.LogInformation("Initialized.");

            // Start main game loop
            _gameLoop.Start();

            _gameBridge.OnElevationRequired += OnElevationRequired;
        }

        private void OnElevationRequired(object? sender, EventArgs e)
        {
            // Show confirm to user
            var choice = MessageBox.Show(
                "Your game is running in Administrator mode, this client is not. Would you like to re-launch this client in Administrator mode? If no, you must launch the game in user mode.",
                "Game is running as Administrator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            // If denied, continue, user's own responsibility
            if (choice != MessageBoxResult.Yes) return;

            try
            {
                // Launch self, requesting admin
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath!,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // 1223 = Admin denied
            {
                return;
            }

            // Shut down self
            Application.Current.Shutdown();
        }

        private void OnViewChange(object? sender, ViewType viewType)
        {
            switch (viewType)
            {
                case ViewType.Setup:
                    ShowSetup();
                    break;
                case ViewType.Tracker:
                    ShowTracker();
                    break;
                case ViewType.DevMenu:
                    ShowDevMenu();
                    break;
                default:
                    throw new NotImplementedException($"Viewtype {viewType} is unknown.");
            }

            _logger.LogDebug($"View changed to {viewType}.");
        }

        public void ShowSetup()
        {
            ViewRoot.Children.Clear();
            ViewRoot.Children.Add(_apSetupView);
        }

        public void ShowDevMenu()
        {
            ViewRoot.Children.Clear();
            ViewRoot.Children.Add(_devMenu);
        }

        public void ShowTracker()
        {
            ViewRoot.Children.Clear();
            ViewRoot.Children.Add(_apTrackerView);
        }
    }
}