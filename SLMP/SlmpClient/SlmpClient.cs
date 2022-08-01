using System.Net.Sockets;

namespace SLMP {
    /// <summary>
    /// This class exposes functionality to connect and manage
    /// SLMP-compatible devices.
    /// </summary>
    public partial class SlmpClient : IDisposable {
        /// <summary>
        /// This `HEADER` array contains the shared (header) data between
        /// commands that are supported in this library.
        /// </summary>
        private readonly byte[] HEADER = {
            0x50, 0x00,     // subheader: no serial no.
            0x00,           // request destination network no.
            0xff,           // request destination station no.
            0xff, 0x03,     // request destination module I/O no.: 0x03ff (own station)
            0x00,           // request destination multidrop station no.
        };

        private SlmpConfig _config;
        private TcpClient? _client;
        private NetworkStream? _stream;

        /// <summary>Initializes a new instance of the <see cref="SlmpClient" /> class.</summary>
        /// <param name="cfg">The config.</param>
        public SlmpClient(SlmpConfig cfg) {
            _config = cfg;
        }

        /// <summary>Connects to the address specified in the config.</summary>
        /// <exception cref="System.TimeoutException">connection timed out</exception>
        public void Connect() {
            if (IsConnected()) return;
            Disconnect();

            _client = new TcpClient();

            if (!_client.ConnectAsync(_config.Address, _config.Port).Wait(_config.ConnTimeout))
                throw new TimeoutException("connection timed out");

            // connection is successful
            _client.SendTimeout = _config.SendTimeout;
            _client.ReceiveTimeout = _config.RecvTimeout;

            _stream = _client.GetStream();
        }

        /// <summary>
        /// Attempt to close the socket connection.
        /// </summary>
        public void Disconnect() {
            try { _stream?.Close(); } catch { }
            _stream = null;

            if (_client?.Connected == true) {
                try { _client.Client.Shutdown(SocketShutdown.Both); } catch { }
            }
            try { _client?.Close(); } catch { }
            _client = null;
        }

        public void Dispose() {
            Disconnect();
        }

        private bool InternalIsConnected() {
            return _stream != null && _client?.Connected == true;
        }

        /// <summary>
        /// Query the connection status.
        /// </summary>
        public bool IsConnected() {
            return InternalIsConnected() && SelfTest();
        }

        /// <summary>This function exists because `NetworkStream` doesn't have a `recv_exact` method.</summary>
        /// <param name="count">Number of bytes to receive.</param>
        private byte[] ReceiveBytes(int count) {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            int offset = 0, toRead = count;
            int read;
            byte[] buffer = new byte[count];

            while (toRead > 0 && (read = _stream!.Read(buffer, offset, toRead)) > 0) {
                toRead -= read;
                offset += read;
            }
            if (toRead > 0) throw new EndOfStreamException();

            return buffer;
        }

        /// <summary>Receives the response and returns the raw response data.</summary>
        /// <returns>Raw response data</returns>
        private List<byte> ReceiveResponse() {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            // read a single byte to determine
            // if a serial no. is included or not
            int value = _stream!.ReadByte();
            byte[] hdrBuf;
            switch (value) {
                // handle the case where we receive EOF
                // from the network stream
                case -1:
                    throw new EndOfStreamException("received EOF from the network stream");
                // if value is 0xd0, there's no serial no. included
                // in the response
                case 0xd0:
                    hdrBuf = ReceiveBytes(8);
                    break;
                // if value is 0xd4, there's a serial no. included
                // in the response
                case 0xd4:
                    hdrBuf = ReceiveBytes(12);
                    break;
                // in the case where we receive some other data, we mark it
                // as invalid and throw an `Exception`
                default:
                    throw new InvalidDataException($"while reading respoonse header: invalid start byte received: {value}");
            }

            // calculate the response data length
            int dataSize = hdrBuf[^1] << 8 | hdrBuf[^2];
            List<byte> responseBuffer = ReceiveBytes(dataSize).ToList();

            // if the encode isn't `0` then we know that we hit an error.
            int endCode = responseBuffer[1] << 8 | responseBuffer[0];
            if (endCode != 0)
                throw new SLMPException(endCode);

            responseBuffer.RemoveRange(0, 2);
            return responseBuffer;
        }

        /// <summary>
        /// Centralized method for sending data.
        /// </summary>
        /// <param name="data">Data to send</param>
        private void SendData(byte[] data) {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            _stream!.Write(data);
            _stream.Flush();
        }

        /// <summary>
        /// Issue a `SelfTest` command.
        /// </summary>
        public bool SelfTest() {
            try {
                SendSelfTestCommand();
                List<byte> response = ReceiveResponse();

                return response.Count == 6 &&
                       response.SequenceEqual(new byte[] { 0x04, 0x00, 0xde, 0xad, 0xbe, 0xef });
            } catch {
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Sends the `SelfTest` command.
        /// </summary>
        private void SendSelfTestCommand() {
            List<byte> rawRequest = HEADER.ToList();
            ushort cmd = (ushort)Command.SelfTest;
            ushort sub = 0x0000;

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): fixed size (12) for the read command
                0x0c, 0x00,
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                0x04, 0x00,
                0xde, 0xad, 0xbe, 0xef
            });

            SendData(rawRequest.ToArray());
        }

        /// <summary>Sends the read device command.</summary>
        /// <param name="device">The target device.</param>
        /// <param name="adr">The address</param>
        /// <param name="cnt">The count.</param>
        private void SendReadDeviceCommand(dynamic device, ushort adr, ushort cnt) {
            List<byte> rawRequest = HEADER.ToList();

            ushort cmd = (ushort)Command.DeviceRead;
            ushort sub = DeviceMethods.GetSubcommand(device);

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): fixed size (12) for the read command
                0x0c, 0x00,
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                (byte)(adr & 0xff), (byte)(adr >> 0x8),
                0x00,
                (byte)device,
                (byte)(cnt & 0xff), (byte)(cnt >> 0x8),
            });

            SendData(rawRequest.ToArray());
        }

        /// <summary>
        /// Sends the write device command.
        /// </summary>
        /// <param name="device">The target device</param>
        /// <param name="adr">The address.</param>
        /// <param name="cnt">Number of data points.</param>
        /// <param name="data">Data itself.</param>
        private void SendWriteDeviceCommand(dynamic device, ushort adr, ushort cnt, byte[] data) {
            List<byte> rawRequest = HEADER.ToList();

            ushort cmd = (ushort)Command.DeviceWrite;
            ushort sub = DeviceMethods.GetSubcommand(device);
            ushort len = (ushort)(data.Length + 0x000c);

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): (12 + data.Length)
                (byte)(len & 0xff), (byte)(len >> 0x8),
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                (byte)(adr & 0xff), (byte)(adr >> 0x8),
                0x00,
                (byte)device,
                (byte)(cnt & 0xff), (byte)(cnt >> 0x8),
            });
            rawRequest.AddRange(data);

            SendData(rawRequest.ToArray());
        }
    }
}
