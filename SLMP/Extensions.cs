using System.Collections;

namespace SLMP {
    /// <summary>
    /// A set of helper functions to perform common conversions.
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// Convert an array of `ushort`s to an array of `int`s.
        /// </summary>
        public static int[] AsIntArray(this ushort[] data) {
            int[] result = new int[data.Length];

            for (int i = 0; i < data.Length; i++) {
                result[i] = Convert.ToInt32(data[i]);
            }

            return result;
        }

        /// <summary>
        /// Convert an array of `ushort`s to a `byte` array.
        /// </summary>
        public static byte[] AsByteArray(this ushort[] data) {
            byte[] result = new byte[data.Length * 2];

            for (int i = 0; i < data.Length; i++) {
                result[i + 0] = Convert.ToByte((data[i] >> 0) & 0xff);
                result[i + 1] = Convert.ToByte((data[i] >> 8) & 0xff);
            }

            return result;
        }

        /// <summary>
        /// Convert an `ushort` to `BitArray`, LSB -> MSB.
        /// </summary>
        public static BitArray AsBitArray(this ushort data) {
            bool[] bits = new bool[16];

            for (int i = 0; i < 16; i++)
                bits[i] = ((data >> i) & 1) != 0;

            return new BitArray(bits);
        }
    }
}
