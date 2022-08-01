# SLMP
SLMP is a protocol used for access from an external device to an SLMP-compatible device through the Ethernet. SLMP communications are available among devices that can transfer messages by SLMP. (personal computers, human machine interface and others.)

This project implements a client library that supports a subset of the functionality described in the [SLMP reference manual](https://www.allied-automation.com/wp-content/uploads/2015/02/MITSUBISHI_manual_plc_iq-r_slmp.pdf), mainly regarding reading from and writing to `Device`s.

# Examples

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
```

### Reading structures
```C#
public struct ExampleStruct {
    public bool boolean_word;              // 2 bytes, 1 word
    public int signed_double_word;         // 4 bytes, 2 words
    public uint unsigned_double_word;      // 4 bytes, 2 words
    public short short_signed_word;        // 2 bytes, 1 word
    public ushort ushort_unsigned_word;    // 2 bytes, 1 word
    [SlmpString(length = 6)]
    public string even_length_string;      // 6 bytes, 3 words (there's an extra 0x0000 right after the string in the plc memory)
    [SlmpString(length = 5)]
    public string odd_length_string;       // 5 bytes, 3 words (upper byte of the 3rd word is 0x00)
}

plc.ReadStruct<ExampleStruct>("D200");
// or
plc.ReadStruct<ExampleStruct>(Device.D, 200);
```

## Funding
If you like this project and/or it's in any way useful to you, and you'd like to buy me a coffee, you can do it so from the link below!  

<a href="https://www.buymeacoffee.com/brkp" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-green.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a> 
