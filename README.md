# SLMP

SLMP is a protocol used for access from an external device to an SLMP-compatible device through the Ethernet. SLMP communications are available among devices that can transfer messages by SLMP. (personal computers, human machine interface and others.)

This project implements a client library that supports a subset of the functionality described in the [SLMP reference manual](https://dl.mitsubishielectric.com/dl/fa/document/manual/plc/sh080956eng/sh080956engl.pdf), mainly regarding reading from and writing to `Device`s.

## Examples

Currently supported devices:
- D, W, R, Z, ZR, SD
- X, Y, M, L, F, V, B, SM 

Keep in mind that some of these devices might not be available for use on your targeted SLMP-compatible device.

### Connecting to and Disconnecting from an SLMP Server
```C#
SlmpConfig cfg = new SlmpConfig("192.168.3.39", 6000) {
    ConnTimeout = 1000,
    RecvTimeout = 1000,
    SendTimeout = 1000,
};
SlmpClient plc = new SlmpClient(cfg);

plc.Connect();
plc.Disconnect();
```

### Reading from/writing into device registers

There are a couple of ways to describe a read/write operation, you can either describe the target device with a string, e.g, `"D200", "M200", "SD0"` or directly with the `Device` enum. See the method prototypes below to get an idea of how the API operates.

```C#
bool ReadBitDevice(string addr)
bool ReadBitDevice(Device device, ushort addr)
bool[] ReadBitDevice(string addr, ushort count)
bool[] ReadBitDevice(Device device, ushort addr, ushort count)

ushort ReadWordDevice(string addr)
ushort ReadWordDevice(Device device, ushort addr)
ushort[] ReadWordDevice(string addr, ushort count)
ushort[] ReadWordDevice(Device device, ushort addr, ushort count)

string ReadString(string addr, ushort len)
string ReadString(Device device, ushort addr, ushort len)

void WriteBitDevice(string addr, bool data)
void WriteBitDevice(string addr, bool[] data)
void WriteBitDevice(Device device, ushort addr, bool data)
void WriteBitDevice(Device device, ushort addr, bool[] data)

void WriteWordDevice(string addr, ushort data)
void WriteWordDevice(string addr, ushort[] data)
void WriteWordDevice(Device device, ushort addr, ushort data)
void WriteWordDevice(Device device, ushort addr, ushort[] data)

void WriteString(string addr, string text)
void WriteString(Device device, ushort addr, string text)

T? ReadStruct<T>(string addr) where T : struct
T? ReadStruct<T>(Device device, ushort addr) where T : struct

void WriteStruct<T>(string addr, T data) where T : struct
void WriteStruct<T>(Device device, ushort addr, T data) where T : struct
```

### Working with structures
```C#
public struct ExampleStruct {
    public bool boolean_word;              // 2 bytes, 1 word
    public int signed_double_word;         // 4 bytes, 2 words
    public uint unsigned_double_word;      // 4 bytes, 2 words
    public short short_signed_word;        // 2 bytes, 1 word
    public ushort ushort_unsigned_word;    // 2 bytes, 1 word
    [SlmpString(Length = 6)]
    public string even_length_string;      // 6 bytes, 3 words (there's an extra 0x0000 right after the string in the plc memory)
    [SlmpString(Length = 5)]
    public string odd_length_string;       // 5 bytes, 3 words (upper byte of the 3rd word is 0x00)
}

// Reading structures
var data = plc.ReadStruct<ExampleStruct>("D200");
var data = plc.ReadStruct<ExampleStruct>(Device.D, 200);

// Writing structures
var data = new ExampleStruct {
    boolean_word = true,
    signed_double_word = -273,
    // ...
};
plc.WriteStruct("D200", data);
plc.WriteStruct(Device.D, 200, data);
```

## Testing with Mock Server

The project includes a mock SLMP server for testing and development:

1. Start the mock server:
   ```bash
   dotnet run --project SLMP.MockServer
   ```
   The server will start on `localhost:2000` by default.

2. In another terminal, run the examples:
   ```bash
   dotnet run --project SLMP.Examples
   ```
   This will connect to the mock server and run various tests including:
   - Sequential word device writes and reads
   - Batch operations and data integrity checks
   - String write/read operations
   - Structure serialization/deserialization
   - Bit device pattern operations
