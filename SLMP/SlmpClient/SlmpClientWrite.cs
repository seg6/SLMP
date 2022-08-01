namespace SLMP {
    public partial class SlmpClient {
        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool data) {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteBitDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes an array of `bool`s to a given `BitDevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool[] data) {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteBitDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool data) {
            WriteBitDevice(device, addr, new bool[] { data });
        }

        /// <summary>
        /// writes an array of `bool`s to a given `bitdevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">the bitdevice to write.</param>
        /// <param name="addr">starting address.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool[] data) {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Bit)
                throw new ArgumentException("provided device is not a bit device");

            ushort count = (ushort)data.Length;
            List<bool> listData = data.ToList();
            List<byte> encodedData = new();

            // If the length of `data` isn't even, add a dummy
            // `false` to make the encoding easier. It gets ignored on the station side.
            if (count % 2 != 0)
                listData.Add(false);

            listData
                .Chunk(2)
                .ToList()
                .ForEach(a => encodedData.Add(
                    (byte)(Convert.ToByte(a[0]) << 4 | Convert.ToByte(a[1]))));

            SendWriteDeviceCommand(device, addr, count, encodedData.ToArray());
            ReceiveResponse();
        }

        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort data) {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteWordDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes an array of `ushort`s to a given `WordDevice`.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort[] data) {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteWordDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort data) {
            WriteWordDevice(device, addr, new ushort[] { data });
        }

        /// <summary>
        /// Writes an array of `ushort`s to a given `WordDevice`.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort[] data) {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Word)
                throw new ArgumentException("provided device is not a word device");

            ushort count = (ushort)data.Length;
            List<byte> encodedData = new();

            foreach (ushort word in data) {
                encodedData.Add((byte)(word & 0xff));
                encodedData.Add((byte)(word >> 0x8));
            }

            SendWriteDeviceCommand(device, addr, count, encodedData.ToArray());
            ReceiveResponse();
        }

        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(string addr, string text) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            WriteString(data.Item1, data.Item2, text);
        }

        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(Device device, ushort addr, string text) {
            // add proper padding to the string
            text += new string('\0', 2 - (text.Length % 2));
            List<ushort> result = new();

            System.Text.Encoding.ASCII.GetBytes(text.ToCharArray())
                .Chunk(2)
                .ToList()
                .ForEach(a => result.Add((ushort)(a[1] << 8 | a[0])));

            WriteWordDevice(device, addr, result.ToArray());
        }

        /// <summary>
        /// Writes a C# structure to the specified device address.
        /// The structure can only contain primitive data types supported by SLMP.
        /// </summary>
        /// <typeparam name="T">The struct type to write.</typeparam>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">The structure data to write.</param>
        public void WriteStruct<T>(string addr, T data) where T : struct {
            Tuple<Device, ushort> addressData = DeviceMethods.ParseDeviceAddress(addr);
            WriteStruct<T>(addressData.Item1, addressData.Item2, data);
        }

        /// <summary>
        /// Writes a C# structure to the specified device address.
        /// The structure can only contain primitive data types supported by SLMP.
        /// </summary>
        /// <typeparam name="T">The struct type to write.</typeparam>
        /// <param name="device">The device to write to.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="data">The structure data to write.</param>
        public void WriteStruct<T>(Device device, ushort addr, T data) where T : struct {
            ushort[] words = SlmpStruct.ToWords(data);
            WriteWordDevice(device, addr, words);
        }
    }
}
