using IpcWithGui.Server.ViewModels;
using System.Windows;

namespace IpcWithGui.Server.Views {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow(MainViewModel vm) {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
