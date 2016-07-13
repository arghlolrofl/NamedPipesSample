using IpcWithGui.Client.ViewModels;
using System.Windows;

namespace IpcWithGui.Client.Views {
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
