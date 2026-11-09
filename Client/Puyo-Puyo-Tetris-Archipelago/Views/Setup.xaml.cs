using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection;
using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Views.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Puyo_Puyo_Tetris_Archipelago.Views
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class APSetupView : UserControl
    {
        private ILogger<APSetupView> _logger;
        private NavigationService _navigationService;
        private IArchipelagoConnection _apConnection;
        private IGameBridge _game;

        // Outline colors for the attach status, green when attached, red when not
        private static readonly Brush AttachedOutline = MakeBrush("#1C7F13");
        private static readonly Brush DetachedOutline = MakeBrush("#821313");

        public APSetupView(
            ILogger<APSetupView> logger,
            NavigationService navigationService,
            IArchipelagoConnection apConnection,
            IGameBridge game
        )
        {
            InitializeComponent();

            _logger = logger;
            _navigationService = navigationService;
            _apConnection = apConnection;
            _game = game;

            // Show the current attach state, then follow it as it changes
            RefreshAttachedStatus();
            _game.OnAttach += (_, _) => RefreshAttachedStatus();
            _game.OnDetach += (_, _) => RefreshAttachedStatus();
        }

        // Attach events come off the attach loop thread, so hop back to the UI thread to touch the text
        private void RefreshAttachedStatus()
        {
            Dispatcher.Invoke(() =>
            {
                if (_game.Attached)
                {
                    AttachedText.Text = "Attached";
                    AttachedText.OutlineBrush = AttachedOutline;
                }
                else
                {
                    AttachedText.Text = "Not Attached";
                    AttachedText.OutlineBrush = DetachedOutline;
                }
            });
        }

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Lock the button while the attempt is in flight
            ConnectButton.IsEnabled = false;
            object originalContent = ConnectButton.Content;
            ConnectButton.Content = "Connecting...";

            try
            {
                ConnectionInfo info = new ConnectionInfo()
                {
                    Host = HostTextBox.Text,
                    Port = PortTextBox.Text,
                    Slot = SlotTextBox.Text,
                    Password = PasswordBox.Password
                };

                // Offload off the UI thread so the window doesn't freeze while connecting
                bool connected = await Task.Run(() => _apConnection.Connect(info));

                // Only navigate once we're actually connected
                if (connected)
                    _navigationService.ChangeView(ViewType.Tracker);
            }
            catch (TaskCanceledException err)
            {
                _logger.LogError(err, "Failed to connect to Archipelago.");
                MessageBox.Show("Connection timed out.", "Connection failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception err)
            {
                _logger.LogError(err, "Failed to connect to Archipelago.");
                MessageBox.Show(err.Message, "Connection failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ConnectButton.Content = originalContent;
                ConnectButton.IsEnabled = true;
            }
        }
    }
}
