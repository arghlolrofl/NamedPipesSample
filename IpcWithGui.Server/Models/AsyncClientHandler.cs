using IpcWithGui.Shared;
using NLog;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace IpcWithGui.Server.Models {
    public class AsyncClientHandler : IDisposable {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        #region Events

        public event EventHandler Disconnected;

        #endregion

        #region Fields

        NamedPipeServerStream _stream;

        #endregion

        #region Properties

        public string ClientId { get; set; }

        #endregion


        #region Initialization/Disposal

        public AsyncClientHandler(string clientId) {
            ClientId = clientId;
        }

        public void Dispose() {
            _logger.Trace("Disposing client handler");
            _stream.Close();
            _logger.Trace("Disposed client handler");
        }

        #endregion


        public async void HandleClient(object pipeStream) {
            _logger.Trace("Begin HandleClient");

            _stream = pipeStream as NamedPipeServerStream;
            if (_stream == null)
                throw new ArgumentException("Error casting object parameter to 'NamedPipeServerStream'!");


            while (_stream.IsConnected && AsyncPipeServer.IsActive) {
                string request = await _stream.ReadBytes();

                if (String.IsNullOrEmpty(request)) {
                    Thread.Sleep(100);
                    continue;
                }

                await ProcessRequest(request);
            }

            _logger.Trace("End HandleClient");
        }

        private async Task ProcessRequest(string request) {
            _logger.Debug("Received request: " + request);

            switch (request) {
                case CommunicationProtocol.Disconnect:
                    await _stream.SendBytes(CommunicationProtocol.Ok);
                    await _stream.FlushAsync();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    break;
                case CommunicationProtocol.Ping:
                    //_logger.Debug("Sending back PONG");
                    //await _stream.SendBytes(CommunicationProtocol.Pong);
                    break;
                default:
                    await _stream.SendBytes("RESULTS");
                    break;
            }
        }
    }
}
