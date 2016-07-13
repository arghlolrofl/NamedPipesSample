using IpcWithGui.Shared;
using NLog;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace IpcWithGui.Server.Models {
    public class AsyncPipeServer {
        private static bool _isActive = true;
        public static bool IsActive {
            get { return _isActive; }
            private set { _isActive = value; }
        }


        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public async Task<ClientConnection> Listen(string pipeName) {
            _logger.Trace("Begin Listen");
            try {
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                _logger.Info($"Wating for incoming connections");

                //await Task.Factory.FromAsync(pipeServer.BeginWaitForConnection, pipeServer.EndWaitForConnection, null);
                pipeServer.WaitForConnection();

                _logger.Info($"Client connected, reading handshake");
                string handshake = await pipeServer.ReadBytes();

                _logger.Trace("Verifying handshake");
                if (handshake == Config.Handshake) {
                    _logger.Trace($"Handshake accepted, requesting client ID");
                    await pipeServer.SendBytes("ID?");
                } else if (handshake == Config.ShutdownCommand) {
                    _logger.Debug("Shutdown request received, closing server stream");
                    IsActive = false;
                    pipeServer.Dispose();
                    pipeServer.Close();
                    return new ClientConnection(Config.ShutdownCommand, null);
                }

                _logger.Trace($"Reading client ID ...");
                string clientId = await pipeServer.ReadBytes();

                _logger.Debug($"Got ClientId '{clientId}', sending back acknowledgement");
                await pipeServer.SendBytes("OK");

                _logger.Trace("End Listen");
                return new ClientConnection(clientId, pipeServer);

            } catch (Exception ex) {
                _logger.Error(ex, "Error while waiting for incoming connections!");
            }

            return null;
        }
    }
}
