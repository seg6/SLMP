namespace SLMP.MockServer {
    class Program {
        static async Task Main(string[] args) {
            if (args.Contains("-h") || args.Contains("--help")) {
                Console.WriteLine("Usage: SLMP.MockServer [port]");
                Console.WriteLine("  port: TCP port to listen on (default: 2000)");
                return;
            }

            int port = 2000;
            if (args.Length > 0 && !int.TryParse(args[0], out port)) {
                Console.Error.WriteLine($"Invalid port: {args[0]}");
                Environment.Exit(1);
            }

            var server = new MockSlmpServer(port);

            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                server.Stop();
            };

            await server.StartAsync();
        }
    }
}