using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Archipelago.Connection;
using Puyo_Puyo_Tetris_Archipelago.Views.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

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

        public APSetupView(
            ILogger<APSetupView> logger,
            NavigationService navigationService,
            IArchipelagoConnection apConnection
        )
        {
            InitializeComponent();

            _logger = logger;
            _navigationService = navigationService;
            _apConnection = apConnection;
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
