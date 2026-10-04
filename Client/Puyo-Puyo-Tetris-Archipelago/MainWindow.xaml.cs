using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Views;
using Puyo_Puyo_Tetris_Archipelago.Views.Classes;
using System.Windows;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NavigationService _clientEvents;
        private readonly ILogger<MainWindow> _logger;
        
        // Classes in constructor are injected by dependency injection
        public MainWindow(NavigationService clientEvents, ILogger<MainWindow> logger)
        {
            // Initializes the window for rendering
            InitializeComponent();

            // Save the injected classes to private variables
            _clientEvents = clientEvents;
            _logger = logger;

            // Show setup screen
            ViewRoot.Children.Add(new APSetupControl());

            // Subscribe to events
            _clientEvents.OnViewChange += OnViewChange;

            _logger.LogInformation("Initialized.");
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
            ViewRoot.Children.Add(new APSetupControl());
        }

        public void ShowDevMenu()
        {
            ViewRoot.Children.Clear();
            ViewRoot.Children.Add(new APDevMenu());
        }

        public void ShowTracker()
        {
            ViewRoot.Children.Clear();
            ViewRoot.Children.Add(new APTrackerControl());
        }
    }
}