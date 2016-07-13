using IpcWithGui.Client.ViewModels;
using IpcWithGui.Client.Views;
using System;
using System.Windows;

namespace IpcWithGui.Client {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        MainViewModel _viewModel;

        private void Application_Startup(object sender, StartupEventArgs e) {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _viewModel = new MainViewModel();
            MainWindow w = new MainWindow(_viewModel);

            w.Show();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            _viewModel.HandleGlobalException(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            _viewModel.HandleGlobalException(e);
        }
    }
}
