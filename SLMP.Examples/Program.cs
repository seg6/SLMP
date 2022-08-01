using System.Diagnostics;

namespace SLMP.Examples {
    class Program {
        static void Main(string[] args) {
            if (args.Contains("-h") || args.Contains("--help")) {
                Console.WriteLine("Usage: SLMP.Examples [host] [port]");
                Console.WriteLine("  host: SLMP server address (default: localhost)");
                Console.WriteLine("  port: SLMP server port (default: 2000)");
                return;
            }

            var host = args.Length > 0 ? args[0] : "localhost";
            var port = args.Length > 1 ? int.Parse(args[1]) : 2000;

            var config = new SlmpConfig(host, port) {
                ConnTimeout = 1000,
                RecvTimeout = 1000,
                SendTimeout = 1000
            };

            try {
                RunBenchmarks(config);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void RunBenchmarks(SlmpConfig config) {
            var client = new SlmpClient(config);
            var sw = Stopwatch.StartNew();

            client.Connect();
            Console.WriteLine($"Connected in {sw.ElapsedMilliseconds}ms");

            Console.WriteLine("Running Self Test");
            var selfTestResult = client.SelfTest();
            Console.WriteLine($"Self Test: {(selfTestResult ? "PASS" : "FAIL")}");

            if (!selfTestResult) {
                Console.WriteLine("Self Test failed, stopping here");
                return;
            }

            BurstWriteTest(client);
            BatchReadTest(client);
            StringTests(client);
            StructTest(client);
            BitPatternTest(client);

            client.Disconnect();
            Console.WriteLine($"Disconnected");
        }

        static void BurstWriteTest(SlmpClient client) {
            Console.WriteLine("Testing sequential word device writes (D0-D99)");
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) {
                client.WriteWordDevice($"D{i}", (ushort)(i * 10));
            }
            Console.WriteLine($"  100 sequential writes: {sw.ElapsedMilliseconds}ms ({sw.ElapsedMilliseconds / 100.0:F2}ms/op)");
        }

        static void BatchReadTest(SlmpClient client) {
            Console.WriteLine("Testing batch read of 100 words (D0-D99) and data verification");
            var sw = Stopwatch.StartNew();
            var batch = client.ReadWordDevice("D0", 100);
            Console.WriteLine($"  Batch read performance: {sw.ElapsedMilliseconds}ms");

            bool valid = true;
            for (int i = 0; i < 100; i++) {
                if (batch[i] != i * 10) {
                    valid = false;
                    break;
                }
            }
            Console.WriteLine($"  Data integrity check: {(valid ? "PASS" : "FAIL")}");
        }

        static void StringTests(SlmpClient client) {
            var tests = new[] { "ASCII", "12345", "" };
            bool allPass = true;

            Console.WriteLine("Testing string write/read operations");
            foreach (var test in tests) {
                client.WriteString("D200", test);
                var result = client.ReadString("D200", (ushort)Math.Max(1, test.Length));
                var pass = result.StartsWith(test);
                allPass &= pass;
                Console.WriteLine($"  Write/read '{test}': {(pass ? "PASS" : "FAIL")}");
            }
            Console.WriteLine($"String operations: {(allPass ? "PASS" : "FAIL")}");
        }

        static void StructTest(SlmpClient client) {
            Console.WriteLine("Testing struct serialization/deserialization (ProcessData -> D500)");
            var data = new ProcessData {
                enabled = true,
                temperature = -273,
                pressure = 1013,
                alarm_code = 0xBEEF,
                batch_id = "BATCH_001"
            };

            client.WriteStruct("D500", data);
            var result = client.ReadStruct<ProcessData>("D500");

            if (result.HasValue) {
                var r = result.Value;
                var pass = r.temperature == data.temperature && r.pressure == data.pressure;
                Console.WriteLine($"  Struct roundtrip test: {(pass ? "PASS" : "FAIL")}");
            } else {
                Console.WriteLine("  Struct roundtrip test: FAIL");
            }
        }

        static void BitPatternTest(SlmpClient client) {
            Console.WriteLine("Testing bit device operations (M100-M163, every 4th bit set)");
            var pattern = new bool[64];
            for (int i = 0; i < 64; i++) {
                pattern[i] = (i & 3) == 0;
            }

            client.WriteBitDevice("M100", pattern);
            var read = client.ReadBitDevice("M100", 64);
            var pass = pattern.SequenceEqual(read);
            Console.WriteLine($"  Bit pattern roundtrip: {(pass ? "PASS" : "FAIL")}");
        }

        public struct ProcessData {
            public bool enabled;
            public int temperature;
            public ushort pressure;
            public ushort alarm_code;
            [SlmpString(Length = 10)]
            public string batch_id;
        }
    }
}
