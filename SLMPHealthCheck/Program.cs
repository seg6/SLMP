using SLMPUnitTests;

namespace SLMPHealthCheck {
    internal class Program {
        static ReadWordDeviceUnitTests readWordDeviceTests = new();
        static ReadBitDeviceUnitTests readBitDeviceUnitTests = new();

        static Action[] readWordDeviceActions = new Action[] {
            readWordDeviceTests.DDevice, readWordDeviceTests.WDevice,
            readWordDeviceTests.ZDevice, readWordDeviceTests.RDevice,
            readWordDeviceTests.ZRDevice, readWordDeviceTests.SDDevice
        };
        static Action[] readBitDeviceActions = new Action[] {
            readBitDeviceUnitTests.XDevice, readBitDeviceUnitTests.YDevice,
            readBitDeviceUnitTests.MDevice, readBitDeviceUnitTests.LDevice,
            readBitDeviceUnitTests.FDevice, readBitDeviceUnitTests.VDevice,
            readBitDeviceUnitTests.BDevice, readBitDeviceUnitTests.SMDevice
        };

        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine($"Usage: program <slmp_ip:string> <slmp_port:int>");
                return;
            }
            Utils.SLMP_ADDR = args[0];
            Utils.SLMP_PORT = int.Parse(args[1]);

            TestRunner testRunner = new();
            testRunner.AddTestGroup("Read Word Device Tests", readWordDeviceActions);
            testRunner.AddTestGroup("Read Bit Device Tests", readBitDeviceActions);

            Console.WriteLine($"SLMP ADDR: {Utils.SLMP_ADDR}");
            Console.WriteLine($"SLMP PORT: {Utils.SLMP_PORT}");
            testRunner.RunTests();
        }
    }

    public class TestRunner {
        private Dictionary<string, Action[]> _testGroups;

        public TestRunner() {
            _testGroups = new();
        }

        public void AddTestGroup(string groupName, Action[] actions) {
            _testGroups.Add(groupName, actions);
        }

        public void RunTests() {
            foreach (KeyValuePair<string, Action[]> entry in _testGroups) {
                string groupName = entry.Key;
                Action[] tests = entry.Value;
                Console.WriteLine($"Test Group: {groupName}\n");

                foreach (Action test in tests) {
                    Console.WriteLine($"Running Test `{test.Method.Name}`...");

                    try {
                        test();
                        Console.WriteLine("Passed");
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("Failed");
                    }
                }

                Console.WriteLine(new string('-', 30));
            }
        }
    }
}