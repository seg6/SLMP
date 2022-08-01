using System.Net;
using System.Net.Sockets;

namespace SLMP.MockServer {
    public class MockSlmpServer {
        private readonly int _port;
        private readonly Dictionary<string, ushort[]> _wordDevices;
        private readonly Dictionary<string, bool[]> _bitDevices;
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _running;

        public MockSlmpServer(int port) {
            _port = port;
            _wordDevices = new Dictionary<string, ushort[]> {
                ["D"] = new ushort[10000],
                ["W"] = new ushort[10000],
                ["R"] = new ushort[10000],
                ["Z"] = new ushort[256],
                ["ZR"] = new ushort[10000],
                ["SD"] = new ushort[10000]
            };
            _bitDevices = new Dictionary<string, bool[]> {
                ["X"] = new bool[10000],
                ["Y"] = new bool[10000],
                ["M"] = new bool[10000],
                ["L"] = new bool[10000],
                ["F"] = new bool[10000],
                ["V"] = new bool[10000],
                ["B"] = new bool[10000],
                ["SM"] = new bool[10000]
            };
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues() {
            _wordDevices["D"][0] = 1234;
            _wordDevices["D"][1] = 5678;
            _wordDevices["D"][100] = 9999;

            StoreString("D", 200, "HELLO SLMP");

            _bitDevices["M"][0] = true;
            _bitDevices["M"][10] = true;
            _bitDevices["Y"][0] = true;
        }

        private void StoreString(string device, int addr, string text) {
            var words = _wordDevices[device];
            for (int i = 0; i < text.Length; i += 2) {
                ushort word = (ushort)text[i];
                if (i + 1 < text.Length)
                    word |= (ushort)(text[i + 1] << 8);
                words[addr + i / 2] = word;
            }
            if (text.Length % 2 == 0)
                words[addr + text.Length / 2] = 0;
        }

        public async Task StartAsync() {
            if (_running) return;

            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);

            try {
                _listener.Start();
                _running = true;

                Console.WriteLine($"Mock SLMP Server started on port {_port}");
                Console.WriteLine("Press Ctrl+C to stop");

                while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                    try {
                        var tcpClient = await _listener.AcceptTcpClientAsync();
                        _ = Task.Run(async () => await HandleClientAsync(tcpClient, _cancellationTokenSource.Token),
                                   _cancellationTokenSource.Token);
                    } catch (ObjectDisposedException) {
                        break;
                    } catch (InvalidOperationException) when (_cancellationTokenSource.Token.IsCancellationRequested) {
                        break;
                    } catch (Exception ex) {
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Failed to start server: {ex.Message}");
                throw;
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token) {
            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            Console.WriteLine($"Client connected: {endpoint}");

            try {
                using (client) {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;

                    var stream = client.GetStream();
                    var buffer = new byte[4096];

                    while (client.Connected && !token.IsCancellationRequested) {
                        try {
                            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            if (bytesRead == 0) break;

                            var response = ProcessRequest(buffer, bytesRead);
                            if (response != null) {
                                await stream.WriteAsync(response, 0, response.Length, token);
                                await stream.FlushAsync(token);
                            }
                        } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                            break;
                        } catch (IOException ex) {
                            Console.WriteLine($"Client {endpoint} I/O error: {ex.Message}");
                            break;
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Client {endpoint} error: {ex.Message}");
            } finally {
                Console.WriteLine($"Client {endpoint} disconnected");
            }
        }

        private byte[]? ProcessRequest(byte[] buffer, int length) {
            if (length < 11) return null;

            int pos = 7;
            var dataLen = buffer[pos] | (buffer[pos + 1] << 8);
            pos += 2;
            pos += 2;

            var command = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 2;
            var subcommand = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 2;

            Console.WriteLine($"Command: 0x{command:X4}, Subcommand: 0x{subcommand:X4}");

            switch (command) {
                case 0x0401:
                    return ProcessDeviceRead(buffer, pos, subcommand);
                case 0x1401:
                    return ProcessDeviceWrite(buffer, pos, subcommand);
                case 0x0619:
                    return ProcessSelfTest(buffer, pos);
                default:
                    return CreateErrorResponse(0x0001);
            }
        }

        private byte[] ProcessDeviceRead(byte[] buffer, int pos, ushort subcommand) {
            var addr = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 3;
            var deviceCode = buffer[pos++];
            var count = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));

            var deviceName = GetDeviceName(deviceCode);
            Console.WriteLine($"Read {deviceName} @ {addr}, Count: {count}");

            if (subcommand == 0x0001) {
                return CreateBitReadResponse(deviceName, addr, count);
            } else {
                return CreateWordReadResponse(deviceName, addr, count);
            }
        }

        private byte[] ProcessDeviceWrite(byte[] buffer, int pos, ushort subcommand) {
            var addr = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 3;
            var deviceCode = buffer[pos++];
            var count = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 2;

            var deviceName = GetDeviceName(deviceCode);
            Console.WriteLine($"Write {deviceName} @ {addr}, Count: {count}");

            if (subcommand == 0x0001) {
                WriteBitDevice(deviceName, addr, buffer, pos, count);
            } else {
                WriteWordDevice(deviceName, addr, buffer, pos, count);
            }

            return CreateWriteResponse();
        }

        private void WriteBitDevice(string deviceName, ushort addr, byte[] buffer, int pos, ushort count) {
            if (!_bitDevices.ContainsKey(deviceName)) return;

            var device = _bitDevices[deviceName];
            for (int i = 0; i < count; i++) {
                int byteIndex = i / 2;
                int bitIndex = i % 2;
                bool value = bitIndex == 0
                    ? (buffer[pos + byteIndex] & 0x10) != 0
                    : (buffer[pos + byteIndex] & 0x01) != 0;
                device[addr + i] = value;
            }
        }

        private void WriteWordDevice(string deviceName, ushort addr, byte[] buffer, int pos, ushort count) {
            if (!_wordDevices.ContainsKey(deviceName)) return;

            var device = _wordDevices[deviceName];
            for (int i = 0; i < count; i++) {
                device[addr + i] = (ushort)(buffer[pos + i * 2] | (buffer[pos + i * 2 + 1] << 8));
            }
        }

        private byte[] ProcessSelfTest(byte[] buffer, int pos) {
            Console.WriteLine("Self Test - parsing request");

            var testDataLen = (ushort)(buffer[pos] | (buffer[pos + 1] << 8));
            pos += 2;

            var testData = new byte[testDataLen + 2];
            testData[0] = (byte)(testDataLen & 0xFF);
            testData[1] = (byte)(testDataLen >> 8);
            Array.Copy(buffer, pos, testData, 2, testDataLen);

            Console.WriteLine($"Echo test data: {string.Join(" ", testData.Select(b => $"{b:X2}"))}");
            var response = new List<byte>();
            response.AddRange(new byte[] { 0xd0, 0x00 });
            response.AddRange(new byte[] { 0x00, 0xff, 0xff, 0x03, 0x00 });
            response.AddRange(BitConverter.GetBytes((ushort)(2 + testData.Length)));
            response.AddRange(new byte[] { 0x00, 0x00 });
            response.AddRange(testData);
            return response.ToArray();
        }

        private byte[] CreateBitReadResponse(string deviceName, ushort addr, ushort count) {
            var response = new List<byte>();
            response.AddRange(new byte[] { 0xd0, 0x00 });
            response.AddRange(new byte[] { 0x00, 0xff, 0xff, 0x03, 0x00 });

            var dataBytes = (count + 1) / 2;
            response.AddRange(BitConverter.GetBytes((ushort)(2 + dataBytes)));
            response.AddRange(new byte[] { 0x00, 0x00 });

            if (_bitDevices.ContainsKey(deviceName)) {
                var device = _bitDevices[deviceName];
                for (int i = 0; i < count; i += 2) {
                    byte value = 0;
                    if (addr + i < device.Length && device[addr + i])
                        value |= 0x10;
                    if (i + 1 < count && addr + i + 1 < device.Length && device[addr + i + 1])
                        value |= 0x01;
                    response.Add(value);
                }
            }

            return response.ToArray();
        }

        private byte[] CreateWordReadResponse(string deviceName, ushort addr, ushort count) {
            var response = new List<byte>();
            response.AddRange(new byte[] { 0xd0, 0x00 });
            response.AddRange(new byte[] { 0x00, 0xff, 0xff, 0x03, 0x00 });
            response.AddRange(BitConverter.GetBytes((ushort)(2 + count * 2)));
            response.AddRange(new byte[] { 0x00, 0x00 });

            if (_wordDevices.ContainsKey(deviceName)) {
                var device = _wordDevices[deviceName];
                for (int i = 0; i < count; i++) {
                    if (addr + i < device.Length) {
                        response.AddRange(BitConverter.GetBytes(device[addr + i]));
                    } else {
                        response.AddRange(new byte[] { 0x00, 0x00 });
                    }
                }
            }

            return response.ToArray();
        }

        private byte[] CreateWriteResponse() {
            var response = new List<byte>();
            response.AddRange(new byte[] { 0xd0, 0x00 });
            response.AddRange(new byte[] { 0x00, 0xff, 0xff, 0x03, 0x00 });
            response.AddRange(new byte[] { 0x02, 0x00 });
            response.AddRange(new byte[] { 0x00, 0x00 });
            return response.ToArray();
        }

        private byte[] CreateErrorResponse(ushort errorCode) {
            var response = new List<byte>();
            response.AddRange(new byte[] { 0xd0, 0x00 });
            response.AddRange(new byte[] { 0x00, 0xff, 0xff, 0x03, 0x00 });
            response.AddRange(new byte[] { 0x02, 0x00 });
            response.AddRange(BitConverter.GetBytes(errorCode));
            return response.ToArray();
        }

        private string GetDeviceName(byte deviceCode) {
            return deviceCode switch {
                0xa8 => "D",
                0xb4 => "W",
                0xaf => "R",
                0xcc => "Z",
                0xb0 => "ZR",
                0xa9 => "SD",
                0x9c => "X",
                0x9d => "Y",
                0x90 => "M",
                0x92 => "L",
                0x93 => "F",
                0x94 => "V",
                0xa0 => "B",
                0x91 => "SM",
                _ => "Unknown"
            };
        }

        public void Stop() {
            if (!_running) return;

            _running = false;
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            Console.WriteLine("\nServer stopped");
        }
    }
}
