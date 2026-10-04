using Puyo_Puyo_Tetris_Archipelago.Controls;
using Puyo_Puyo_Tetris_Archipelago.Enums.Client;
using Puyo_Puyo_Tetris_Archipelago.Events;
using System.Windows;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientEvents _clientEvents;
        
        // Classes in constructor are injected by dependency injection
        public MainWindow(ClientEvents clientEvents)
        {
            // Initializes the window for rendering
            InitializeComponent();

            // Save the injected classes to private variables
            _clientEvents = clientEvents;

            // Show setup screen
            ViewRoot.Children.Add(new APSetupControl());

            // Subscribe to events
            _clientEvents.OnViewChange += OnViewChange;
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