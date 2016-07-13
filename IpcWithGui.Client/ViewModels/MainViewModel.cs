using IpcWithGui.Client.Models;
using IpcWithGui.Shared;
using Microsoft.Practices.Prism.Commands;
using NLog;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Threading;

namespace IpcWithGui.Client.ViewModels {
    public class MainViewModel : INotifyPropertyChanged, IDisposable {
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static MainViewModel _instance;


        #region Fields

        private string _logString;
        private DelegateCommand _connectCommand;
        private DelegateCommand _clickCommand;

        private DelegateCommand _disconnectCommand;
        private AsyncPipeClient _pipeClient;
        private Timer _timer;

        #endregion

        #region Properties

        public DelegateCommand ClickCommand {
            get { return _clickCommand ?? (_clickCommand = new DelegateCommand(ClickCommand_OnExecute)); }
            set { _clickCommand = value; RaisePropertyChanged(); }
        }

        public DelegateCommand ConnectCommand {
            get { return _connectCommand ?? (_connectCommand = new DelegateCommand(ConnectCommand_OnExecute)); }
            set { _connectCommand = value; RaisePropertyChanged(); }
        }

        public DelegateCommand DisconnectCommand {
            get { return _disconnectCommand ?? (_disconnectCommand = new DelegateCommand(DisconnectCommand_OnExecute)); }
            set { _disconnectCommand = value; RaisePropertyChanged(); }
        }

        public string LogString {
            get { return _logString; }
            set { _logString = value; RaisePropertyChanged(); }
        }

        public AsyncPipeClient PipeClient {
            get { return _pipeClient ?? (_pipeClient = new AsyncPipeClient(Config.PipeName, Config.ServerName)); }
            set { _pipeClient = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Initialization/Disposal
        public MainViewModel() {
            _logger.Trace("Begin CTOR");
            _instance = this;

            _timer = new Timer(5000) {
                AutoReset = true,
                Enabled = false
            };
            _timer.Elapsed += PingTimer_OnElapsed;

            _logger.Trace("End CTOR");
        }

        public void Dispose() {
            _logger.Trace("Begin Dispose");
            PipeClient?.Dispose();
            _timer.Elapsed -= PingTimer_OnElapsed;
            _logger.Trace("End Dispose");
        }

        #endregion

        private async void ConnectCommand_OnExecute() {
            _logger.Trace("Begin ConnectCommand_OnExecute");
            _logger.Info($"Connecting to '{Config.ServerName}'");

            try {
                bool isConnected = await PipeClient.Connect(Config.Handshake);

                _logger.Debug("isConnected = " + isConnected);

                if (!isConnected) {
                    PipeClient.Dispose();
                    PipeClient = null;
                } else {
                    _timer.Start();
                }
            } catch (TimeoutException) {
                _logger.Error($"Timeout exceeded while trying to connect to server through '{Config.PipeName}'.");
            } catch (Exception ex) {
                _logger.Error(ex, "Connection to server failed");
            }

            _logger.Trace("End ConnectCommand_OnExecute");
        }

        private async void PingTimer_OnElapsed(object sender, ElapsedEventArgs e) {
            _logger.Trace("Begin PingTimer_OnElapsed");
            try {
                _timer.Stop();

                bool isConnected = await PipeClient.Ping();
                PipeClient.IsConnected = true;
            } catch (IOException) {
                PipeClient.IsConnected = false;
            } finally {
                _timer.Start();
            }

            _logger.Trace("End PingTimer_OnElapsed");
        }

        private async void DisconnectCommand_OnExecute() {
            _logger.Trace("Begin DisconnectCommand_OnExecute");
            try {
                await PipeClient.Disconnect();
            } finally {
                if (_timer.Enabled) _timer.Stop();
            }

            _logger.Trace("End DisconnectCommand_OnExecute");
        }

        private async void ClickCommand_OnExecute() {
            _logger.Trace("Begin ClickCommand_OnExecute");

            try {
                string response = await PipeClient.Request("REQUEST");
            } catch (TimeoutException) {
                _logger.Error($"Request timed out through '{Config.PipeName}'.");
            } catch (InvalidOperationException ioEx) {
                _logger.Error(ioEx, "Request error: " + ioEx.Message);
            } catch (Exception ex) {
                _logger.Error(ex, $"Request error.");
            }

            _logger.Trace("End ClickCommand_OnExecute");
        }

        #region Exception Handling

        internal void HandleGlobalException(UnhandledExceptionEventArgs e) {
            Exception ex = e.ExceptionObject as Exception;
            HandleGlobalException(ex);
        }

        internal void HandleGlobalException(DispatcherUnhandledExceptionEventArgs e) {
            HandleGlobalException(e.Exception);
        }

        internal void HandleGlobalException(Exception ex) {
            _logger.Error(ex, "Uncaught Exception: " + ex.GetType().Name);
        }

        #endregion

        #region Logging

        public static void LogToUi(string level, string message) {
            _instance.LogString += $"[{level}] {message}\n";
        }

        #endregion
    }
}
