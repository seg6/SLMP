namespace SLMP {
    /// <summary>
    /// This class is intended to be passed to the `SlmpClient` class. It describes the required
    /// configuration for the SLMP client in an abstracted way, which is way better than having
    /// them as random attributes scattered around in the `SlmpClient` class.
    /// </summary>
    public class SlmpConfig {
        /// <summary>
        /// IP address of the target SLMP-compatible device
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// The port that SLMP server is configured to run on.
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Connection timeout.
        /// </summary>
        public int ConnTimeout { get; set; } = 1000;
        /// <summary>
        /// Receive timeout.
        /// </summary>
        public int RecvTimeout { get; set; } = 1000;
        /// <summary>
        /// Send timeout.
        /// </summary>
        public int SendTimeout { get; set; } = 1000;

        /// <summary>
        /// Initialize a new `SlmpConfig` class
        /// </summary>
        public SlmpConfig(string address, int port) {
            Address = address;
            Port = port;
        }
    }
}
