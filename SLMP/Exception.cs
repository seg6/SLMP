namespace SLMP {
    /// <summary>
    /// This exception is thrown in the case where `send` and `recv` data
    /// functions are called but there's no valid connection to operate on.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class NotConnectedException : Exception {
        public NotConnectedException()
            : base("not connected to a server") { }
    }

    /// <summary>
    /// This exception is thrown to indicate that the library is trying
    /// to process invalid data.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidDataException : Exception {
        public InvalidDataException(string message)
            : base(message) { }
    }

    /// <summary>
    /// This exception encapsulates the SLMP End Code for the further
    /// inspection of the error happened in the server (PLC/SLMP-compatible device) side.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class SLMPException : Exception {
        public int SLMPEndCode { get; set; }
        public SLMPException(int endCode)
            : base($"Received non-zero SLMP EndCode: {endCode:X4}H") {
            SLMPEndCode = endCode;
        }
    }
}
