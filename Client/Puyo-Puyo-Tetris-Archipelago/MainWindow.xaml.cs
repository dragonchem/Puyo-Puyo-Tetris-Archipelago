using Puyo_Puyo_Tetris_Archipelago.Views;
using System.Windows;

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
        }
    }
}