using System.Windows;
using Esp32Controller.ViewModels;

namespace Esp32Controller.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            Closed += (s, e) => _viewModel.Dispose();
        }
    }
}