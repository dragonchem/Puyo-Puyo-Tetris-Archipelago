using Puyo_Puyo_Tetris_Archipelago.Controls;
using System.Windows;
using System.Windows.Input;

namespace Puyo_Puyo_Tetris_Archipelago
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewRoot.Children.Add(new APSetupControl());

            Keyboard.AddKeyDownHandler(this, (sender, e) =>
            {
                if (e.Key == Key.F9) ShowDevMenu();
            });
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