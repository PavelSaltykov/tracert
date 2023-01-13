using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Tracert
{
    public static class TracertUtility
    {
        private static readonly Ping pingSender = new();
        private const int bufferSize = 32;
        private static readonly byte[] buffer = new byte[bufferSize];
        private static readonly PingOptions options = new() { DontFragment = true };
        private static readonly Stopwatch stopwatch = new();

        public const int NumberOfPackets = 3;
        public const int DefaultMaxHops = 30;
        public const int DefaultTimeout = 4000;

        public const string TimeoutMessage = "Request timed out.";

        public static void PrintTraceRoute(string target, 
            int maxHops = DefaultMaxHops, int timeout = DefaultTimeout)
        {
            if (maxHops <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHops));

            if (timeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            void PrintNodes()
            {
                for (int ttl = 1; ttl <= maxHops; ++ttl)
                {
                    Console.Write($"  {ttl}");
                    options.Ttl = ttl;

                    for (int i = 0; i < NumberOfPackets; ++i)
                    {
                        stopwatch.Restart();
                        var reply = pingSender.Send(target, timeout, buffer, options);
                        stopwatch.Stop();

                        switch (reply.Status)
                        {
                            case IPStatus.TtlExpired:
                            case IPStatus.TimeExceeded:
                            case IPStatus.Success:
                                Console.Write($"\t{stopwatch.ElapsedMilliseconds} ms");
                                if (i == NumberOfPackets - 1)
                                {
                                    Console.WriteLine($"\t{reply.Address}");
                                    if (reply.Status == IPStatus.Success)
                                        return;
                                }
                                break;
                            default:
                                Console.Write("\t*");
                                if (i == NumberOfPackets - 1)
                                    Console.WriteLine($"\t{TimeoutMessage}");
                                break;
                        }
                    }
                }
            }

            Console.WriteLine($"Tracing route to {target}");
            Console.WriteLine($"over a maximum of {maxHops} hops:\n");
            PrintNodes();
            Console.WriteLine("\nTrace complete.");
        }
    }
}
