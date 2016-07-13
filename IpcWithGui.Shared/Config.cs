namespace IpcWithGui.Shared {
    public class Config {
        private static string _pipeName = "TestPipe";
        public static string PipeName {
            get { return _pipeName; }
            private set { _pipeName = value; }
        }

        private static string _handshake = "abcdef123456";
        public static string Handshake {
            get { return _handshake; }
            private set { _handshake = value; }
        }

        private static string _serverName = ".";
        public static string ServerName {
            get { return _serverName; }
            private set { _serverName = value; }
        }

        private static string _shutdownCommand = "SHUTDOWN";
        public static string ShutdownCommand {
            get { return _shutdownCommand; }
            set { _shutdownCommand = value; }
        }


    }
}
