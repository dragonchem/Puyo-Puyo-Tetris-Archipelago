using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago.Views.Classes
{
    public class NavigationService
    {
        public NavigationService()
        {
            // Register global keydown on all window classes
            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.KeyDownEvent,
                new KeyEventHandler(GlobalKeyDown)
            );
        }

        private void GlobalKeyDown(object sender, KeyEventArgs e)
        {
            // If F9 pressed at any time, trigger view change event to devmenu
            if (e.Key == Key.F9) OnViewChange?.Invoke(this, ViewType.DevMenu);
        }

        // Events
        public event EventHandler<ViewType>? OnViewChange;
    }
}
