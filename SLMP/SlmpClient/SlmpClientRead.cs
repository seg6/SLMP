namespace SLMP {
    public partial class SlmpClient {
        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public bool ReadBitDevice(string addr) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadBitDevice(data.Item1, data.Item2);
        }

        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        /// <returns></returns>
        public bool[] ReadBitDevice(string addr, ushort count) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadBitDevice(data.Item1, data.Item2, count);
        }

        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Bit address.</param>
        public bool ReadBitDevice(Device device, ushort addr) {
            return ReadBitDevice(device, addr, 1)[0];
        }

        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The bit device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public bool[] ReadBitDevice(Device device, ushort addr, ushort count) {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Bit)
                throw new ArgumentException("provided device is not a bit device");

            SendReadDeviceCommand(device, addr, count);
            List<byte> response = ReceiveResponse();
            List<bool> result = new();

            response.ForEach(delegate (byte a) {
                result.Add((a & 0x10) != 0);
                result.Add((a & 0x01) != 0);
            });

            return result.GetRange(0, count).ToArray();
        }

        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public ushort ReadWordDevice(string addr) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadWordDevice(data.Item1, data.Item2);
        }

        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address as a string.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(string addr, ushort count) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadWordDevice(data.Item1, data.Item2, count);
        }

        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Word address.</param>
        public ushort ReadWordDevice(Device device, ushort addr) {
            return ReadWordDevice(device, addr, 1)[0];
        }

        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(Device device, ushort addr, ushort count) {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Word)
                throw new ArgumentException("provided device is not a word device");

            SendReadDeviceCommand(device, addr, count);
            List<byte> response = ReceiveResponse();
            List<ushort> result = new();

            // if the length of the response isn't even
            // then the response is invalid and we can't
            // construct an array of `ushort`s from it
            if (response.Count % 2 != 0)
                throw new InvalidDataException("While reading words: data section of the response is uneven");

            // word data is received in little endian format
            // which means the lower byte of a word comes first
            // and upper byte second
            response
                .Chunk(2)
                .ToList()
                .ForEach(n => result.Add((ushort)(n[1] << 8 | n[0])));

            return result.ToArray();
        }

        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Starting address of the null terminated string as a string.</param>
        /// <param name="len">Length of the string.</param>
        public string ReadString(string addr, ushort len) {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadString(data.Item1, data.Item2, len);
        }

        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address of the null terminated string.</param>
        /// <param name="len">Length of the string.</param>
        public string ReadString(Device device, ushort addr, ushort len) {
            ushort wordCount = (ushort)((len % 2 == 0 ? len : len + 1) / 2);
            List<char> buffer = new();

            foreach (ushort word in ReadWordDevice(device, addr, wordCount)) {
                buffer.Add((char)(word & 0xff));
                buffer.Add((char)(word >> 0x8));
            }

            return string.Join("", buffer.GetRange(0, len));
        }

        /// <summary>
        /// Read from a `WordDevice` to create a C# structure.
        /// The target structure can only contain very primitive data types.
        /// </summary>
        /// <typeparam name="T">The `Struct` to read.</typeparam>
        /// <param name="addr">Starting address of the structure data in the string format.</param>
        public T? ReadStruct<T>(string addr) where T : struct {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadStruct<T>(data.Item1, data.Item2);
        }

        /// <summary>
        /// Read from a `WordDevice` to create a C# structure.
        /// The target structure can only contain very primitive data types.
        /// </summary>
        /// <typeparam name="T">The `Struct` to read.</typeparam>
        /// <param name="device">The device to read from..</param>
        /// <param name="addr">Starting address of the structure data.</param>
        public T? ReadStruct<T>(Device device, ushort addr) where T : struct {
            Type structType = typeof(T);
            ushort[] words = ReadWordDevice(
                device, addr, (ushort)SlmpStruct.GetStructSize(structType));

            return SlmpStruct.FromWords(structType, words) as T?;
        }
    }
}
