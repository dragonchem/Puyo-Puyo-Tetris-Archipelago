using Microsoft.Extensions.DependencyInjection;
using Puyo_Puyo_Tetris_Archipelago.Events;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Puyo_Puyo_Tetris_Archipelago
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Dependency injection setup for WPF (default always set, surpresses warning)
        public IServiceProvider _serviceProvider = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // List of all dependency injection services available
            ServiceCollection serviceCollection = new ServiceCollection();

            // All classes we want to have as singletons
            serviceCollection.AddSingleton<ClientEvents>();

            // Add MainWindow as a singleton so we can use dependency injection in it, this cascades to all usercontrols
            serviceCollection.AddSingleton<MainWindow>();

            // Builds the list into actual dependency injection services
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Go to mainwindow
            MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dispose of the service provider when the application exits, prevents garbage in ram
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // Exit after disposal
            base.OnExit(e);
        }
    }
}
