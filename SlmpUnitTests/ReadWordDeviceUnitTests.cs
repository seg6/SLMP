namespace SLMPUnitTests {
    [TestClass]
    public class ReadWordDeviceUnitTests {
        [TestMethod]
        public void DDevice() {
            Utils.ReadWordDeviceFromAddr("D0", 0);
            Utils.ReadWordDeviceFromAddr("D0", 10);
        }

        [TestMethod]
        public void WDevice() {
            Utils.ReadWordDeviceFromAddr("W0", 0);
            Utils.ReadWordDeviceFromAddr("W0", 10);
        }

        [TestMethod]
        public void RDevice() {
            Utils.ReadWordDeviceFromAddr("R0", 0);
            Utils.ReadWordDeviceFromAddr("R0", 10);
        }

        [TestMethod]
        public void ZDevice() {
            Utils.ReadWordDeviceFromAddr("Z0", 0);
        }

        [TestMethod]
        public void ZRDevice() {
            Utils.ReadWordDeviceFromAddr("ZR0", 0);
            Utils.ReadWordDeviceFromAddr("ZR0", 10);
        }

        [TestMethod]
        public void SDDevice() {
            Utils.ReadWordDeviceFromAddr("SD0", 0);
            Utils.ReadWordDeviceFromAddr("SD0", 10);
        }
    }
}