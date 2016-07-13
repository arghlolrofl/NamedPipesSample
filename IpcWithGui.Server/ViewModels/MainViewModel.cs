using IpcWithGui.Server.Models;
using IpcWithGui.Shared;
using Microsoft.Practices.Prism.Commands;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace IpcWithGui.Server.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static MainViewModel _instance;

        #region Fields

        private Dispatcher _dispatcher;
        private string _logString;
        private DelegateCommand _startServerCommand;
        private DelegateCommand _stopServerCommand;
        private ObservableCollection<string> _clients;
        private Dictionary<string, Thread> _clientThreads;
        private Thread _serverThread;

        #endregion

        #region Properties

        public string LogString {
            get { return _logString; }
            set { _logString = value; RaisePropertyChanged(); }
        }

        public DelegateCommand StartServerCommand {
            get { return _startServerCommand ?? (_startServerCommand = new DelegateCommand(StartServerCommand_OnExecute)); }
            set { _startServerCommand = value; }
        }

        public DelegateCommand StopServerCommand {
            get { return _stopServerCommand ?? (_stopServerCommand = new DelegateCommand(StopServerCommand_OnExecute)); }
            set { _stopServerCommand = value; }
        }

        public ObservableCollection<string> Clients {
            get { return _clients ?? (_clients = new ObservableCollection<string>()); }
            set { _clients = value; RaisePropertyChanged(); }
        }

        public Dictionary<string, Thread> ClientThreads {
            get { return _clientThreads ?? (_clientThreads = new Dictionary<string, Thread>()); }
            set { _clientThreads = value; }
        }

        #endregion


        public MainViewModel() {
            _logger.Trace("Begin CTOR");

            _dispatcher = Dispatcher.CurrentDispatcher;
            _instance = this;

            _logger.Trace("End CTOR");
        }

        private void StartServerCommand_OnExecute() {
            Task serverTask = new Task(new Action(async () => {
                _logger.Trace("Begin StartServerCommand_OnExecute");

                string clientId = String.Empty;
                AsyncPipeServer server = new AsyncPipeServer();

                _logger.Debug("Starting server");

                while (true) {
                    ClientConnection client = await server.Listen(Config.PipeName);

                    if (client.ClientId == Config.ShutdownCommand)
                        break;
                    else
                        Client_OnConnected(client);
                }

                _logger.Trace("End StartServerCommand_OnExecute");
            }));

            serverTask.Start();
        }

        private void Client_OnConnected(ClientConnection client) {
            _logger.Trace("Begin Client_OnConnected");

            AsyncClientHandler handler = new AsyncClientHandler(client.ClientId);
            handler.Disconnected += Client_OnDisconnected;

            Thread t = new Thread(handler.HandleClient);

            _dispatcher.BeginInvoke(new Action(() => {
                Clients.Add(client.ClientId);
                ClientThreads.Add(client.ClientId, t);
            }));

            t.Start(client.PipeStream);

            _logger.Trace("End Client_OnConnected");
        }

        private void Client_OnDisconnected(object sender, EventArgs e) {
            _logger.Trace("Begin Client_OnDisconnected");

            AsyncClientHandler handler = sender as AsyncClientHandler;
            if (Clients.Any(c => c == handler.ClientId)) {
                _dispatcher.BeginInvoke(new Action(() => {
                    Clients.Remove(handler.ClientId);
                }));
            }

            _logger.Trace("End Client_OnDisconnected");
        }

        private void StopServerCommand_OnExecute() {
            _logger.Trace("Begin StopServerCommand_OnExecute");

            ThreadStart ts = async delegate {
                _logger.Debug("Closing client threads");
                foreach (var item in ClientThreads) {
                    Thread runningThread = item.Value;
                    if (runningThread.IsAlive) {
                        _logger.Debug("Aborting thread: " + runningThread.ManagedThreadId);
                        runningThread.Abort();

                        if (Clients.Any(c => c == item.Key)) {
                            await _dispatcher.BeginInvoke(new Action(() => {
                                Clients.Remove(Clients.Single(c => c == item.Key));
                            }));
                        }
                    }
                }

                _logger.Trace("Establishing fake client shutdown connection");

                NamedPipeClientStream client = new NamedPipeClientStream(Config.PipeName);
                client.Connect(500);

                _logger.Debug("Sending server shutdown request");
                await client.SendBytes(Config.ShutdownCommand);
                _logger.Trace("Shutdown request sent");
            };

            Thread t = new Thread(ts);
            t.Start();

            _logger.Trace("End StopServerCommand_OnExecute");
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
