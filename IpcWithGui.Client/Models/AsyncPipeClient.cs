using IpcWithGui.Shared;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IpcWithGui.Client.Models {
    public class AsyncPipeClient : INotifyPropertyChanged, IDisposable {
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Fields

        private int _connectTimeout;
        private readonly Guid _clientId;
        private NamedPipeClientStream _stream;

        #endregion

        #region Properties

        public bool IsConnected {
            get { return Stream.IsConnected; }
            set { RaisePropertyChanged(); }
        }

        public int ConnectTimeout {
            get { return _connectTimeout; }
            set { _connectTimeout = value; }
        }

        public Guid ClientId {
            get { return _clientId; }
        }

        public NamedPipeClientStream Stream {
            get { return _stream; }
            set { _stream = value; }
        }

        #endregion


        #region Initialization/Disposal

        public AsyncPipeClient(string pipeName, string serverName, int timeout = 3000) {
            _stream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            ConnectTimeout = timeout;
            _clientId = Guid.NewGuid();
        }

        public void Dispose() {
            _stream?.Close();
        }

        #endregion


        public async Task<bool> Connect(string handshake) {
            // The connect function will indefinitely wait for the pipe to become available
            // If that is not acceptable specify a maximum waiting time (in ms)
            _stream.Connect(ConnectTimeout);

            // When connected, send the handshake
            await _stream.SendBytes(handshake);

            // After sending the handshake, the server will request a client id ...
            string response = await _stream.ReadBytes();
            if (response != CommunicationProtocol.IdRequest)
                return false;

            // ... so, we send it
            await _stream.SendBytes(ClientId.ToString());

            // If the client has been registered successfully on the server side, we expect 
            // the response 'OK'
            response = await _stream.ReadBytes();
            if (response != CommunicationProtocol.Ok)
                return false;

            // We can now assume, that the client is connected to the server
            RaisePropertyChanged(nameof(IsConnected));
            return true;
        }

        public async Task<bool> Ping() {
            try {
                await _stream.SendBytes(CommunicationProtocol.Ping);

                //string response = await _stream.ReadBytes();
                //return response == CommunicationProtocol.Pong;
                return true;
            } catch (IOException) {
                return false;
            }
        }

        public async Task<string> Request(string requestString) {
            string response = null;

            try {
                // Send the request string to the server ...
                await _stream.SendBytes(requestString);

                // ... and return the response.
                response = await _stream.ReadBytes();
            } catch (IOException) {
                throw;
            }

            return response;
        }

        public async Task<bool> Disconnect() {
            // Notify the server that the client wants to disconnect
            await _stream.SendBytes(CommunicationProtocol.Disconnect);

            // Read the response, which sould be 'OK' ...
            string response = await _stream.ReadBytes();

            // If so, we can close the stream ...
            if (response == CommunicationProtocol.Ok) {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }

            // ... and notify the UI
            RaisePropertyChanged(nameof(IsConnected));

            return response == CommunicationProtocol.Ok;
        }
    }
}