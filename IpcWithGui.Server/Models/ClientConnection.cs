using System.IO.Pipes;

namespace IpcWithGui.Server.Models {
    public class ClientConnection {
        public string ClientId { get; private set; }
        public PipeStream PipeStream { get; private set; }

        public ClientConnection(string clientId, PipeStream stream) {
            ClientId = clientId;
            PipeStream = stream;
        }
    }
}
