namespace SLMP {
    /// <summary>
    /// This enum encodes the supported SLMP commands within this library.
    /// </summary>
    public enum Command {
        DeviceRead = 0x0401,
        DeviceWrite = 0x1401,
        SelfTest = 0x0619,
    }
}
