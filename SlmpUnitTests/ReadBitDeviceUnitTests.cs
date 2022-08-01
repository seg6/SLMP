// X = 0x9c,
// Y = 0x9d,
// M = 0x90,
// L = 0x92,
// F = 0x93,
// V = 0x94,
// B = 0xa0,
// SM = 0x91,

namespace SLMPUnitTests {
    [TestClass]
    public class ReadBitDeviceUnitTests {
        [TestMethod]
        public void XDevice() {
            Utils.ReadBitDeviceFromAddr("X0", 0);
            Utils.ReadBitDeviceFromAddr("X0", 10);
        }

        [TestMethod]
        public void YDevice() {
            Utils.ReadBitDeviceFromAddr("Y0", 0);
            Utils.ReadBitDeviceFromAddr("Y0", 10);
        }

        [TestMethod]
        public void MDevice() {
            Utils.ReadBitDeviceFromAddr("M0", 0);
            Utils.ReadBitDeviceFromAddr("M0", 10);
        }

        [TestMethod]
        public void LDevice() {
            Utils.ReadBitDeviceFromAddr("L0", 0);
            Utils.ReadBitDeviceFromAddr("L0", 10);
        }

        [TestMethod]
        public void FDevice() {
            Utils.ReadBitDeviceFromAddr("F0", 0);
            Utils.ReadBitDeviceFromAddr("F0", 10);
        }

        [TestMethod]
        public void VDevice() {
            Utils.ReadBitDeviceFromAddr("V0", 0);
            Utils.ReadBitDeviceFromAddr("V0", 10);
        }

        [TestMethod]
        public void BDevice() {
            Utils.ReadBitDeviceFromAddr("B0", 0);
            Utils.ReadBitDeviceFromAddr("B0", 10);
        }

        [TestMethod]
        public void SMDevice() {
            Utils.ReadBitDeviceFromAddr("SM0", 0);
            Utils.ReadBitDeviceFromAddr("SM0", 10);
        }
    }
}
