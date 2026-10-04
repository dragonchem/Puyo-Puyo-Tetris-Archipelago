using Puyo_Puyo_Tetris_Archipelago.Game;
using Puyo_Puyo_Tetris_Archipelago.Game.Memory.Adventure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago.Views
{
    /// <summary>
    /// Interaction logic for DevMenu.xaml
    /// </summary>
    public partial class APDevMenu : UserControl
    {
        private readonly GameBridge _game;
        private readonly AdventureState _adventureState;
        public APDevMenu(GameBridge game, AdventureState adventureState)
        {
            InitializeComponent();
            _game = game;
            _adventureState = adventureState;

            RefreshAttachedStatus();

            _game.OnAttach += (_, _) => RefreshAttachedStatus();
            _game.OnDetach += (_, _) => RefreshAttachedStatus();

            _adventureState.StateChanged += (_, _) => RefreshAdventure();
        }

        public void RefreshAll()
        {
            RefreshAttachedStatus();
        }

        public void RefreshAttachedStatus()
        {
            if (_game != null && _game.Attached) AttachStatusText.Text = "ATTACHED";
            else AttachStatusText.Text = "DETACHED";
        }

        public void RefreshAdventure()
        {
            AdventureStages.ItemsSource = _adventureState.State;
        }

        private void Stage_Click(object sender, RoutedEventArgs e)
        {
            if (_adventureState.State == null) return;
            if (((FrameworkElement)sender).DataContext is AdventureStage s)
            {
                int index = (s.Act - 1) * 10 + (s.Stage - 1);
                _adventureState.SetDesiredCleared(index, !_adventureState.State[index].Cleared);
            }
        }

        private void Stage_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (_adventureState.State == null) return;
            if (((FrameworkElement)sender).DataContext is AdventureStage s)
            {
                int index = (s.Act - 1) * 10 + (s.Stage - 1);
                _adventureState.SetDesiredStars(index, (_adventureState.State[index].Stars + 1) % 4);
                e.Handled = true;
            }
        }
    }
}
