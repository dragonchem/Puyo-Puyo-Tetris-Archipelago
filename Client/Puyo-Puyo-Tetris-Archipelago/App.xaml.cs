using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;
using Puyo_Puyo_Tetris_Archipelago.Interfaces;
using Puyo_Puyo_Tetris_Archipelago.Views;
using Puyo_Puyo_Tetris_Archipelago.Views.Classes;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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

            // Set log level
#if DEBUG
            const LogEventLevel DefaultLevel = LogEventLevel.Debug;
#else
            const LogEventLevel DefaultLevel = LogEventLevel.Information;
#endif
            LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch(DefaultLevel);

            // Create logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Debug()
                .CreateLogger();

            // List of all dependency injection services available
            ServiceCollection serviceCollection = new ServiceCollection();

            // All classes we want to have as singletons
            serviceCollection.AddSingleton<NavigationService>();
            serviceCollection.AddSingleton<GameBridge>();
            serviceCollection.AddSingleton<GameLoop>();

            // Memory modules
            serviceCollection.AddSingleton<AdventureMemoryModule>();

            // State modules
            serviceCollection.AddSingleton<AdventureState>();

            // Pollables
            serviceCollection.AddSingleton<IPollable>(services => services.GetRequiredService<AdventureState>());

            // Add the various usercontrols / views to the DI to make them DI aware
            serviceCollection.AddSingleton<APDevMenu>();
            serviceCollection.AddSingleton<APSetupView>();
            serviceCollection.AddSingleton<APTrackerView>();

            // Add MainWindow as a singleton so we can use dependency injection in it, this cascades to all usercontrols
            serviceCollection.AddSingleton<MainWindow>();

            serviceCollection.AddLogging(builder => {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

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
