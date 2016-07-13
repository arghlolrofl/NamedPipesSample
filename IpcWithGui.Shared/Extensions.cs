using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace IpcWithGui {
    public static class Extensions {
        private static UnicodeEncoding streamEncoding;

        static Extensions() {
            streamEncoding = new UnicodeEncoding();
        }

        public static async Task<string> ReadBytes(this PipeStream stream) {
            int len;

            len = stream.ReadByte() * 256;
            len += stream.ReadByte();

            if (len <= 0)
                return null;

            byte[] inBuffer = new byte[len];
            await stream.ReadAsync(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public static async Task<int> SendBytes(this PipeStream stream, string message) {
            byte[] outBuffer = streamEncoding.GetBytes(message);
            int len = outBuffer.Length;

            if (len > UInt16.MaxValue)
                len = (int)UInt16.MaxValue;

            stream.WriteByte((byte)(len / 256));
            stream.WriteByte((byte)(len & 255));

            await stream.WriteAsync(outBuffer, 0, len);
            await stream.FlushAsync();

            return outBuffer.Length + 2;
        }
    }

}
