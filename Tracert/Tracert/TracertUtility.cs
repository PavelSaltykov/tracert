using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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

        public static void PrintTraceRoute(string target, int maxHops = DefaultMaxHops, int timeout = DefaultTimeout)
        {
            if (maxHops <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHops));

            if (timeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            target = target.Trim();
            var targetIp = GetIPAddressOrNull(target);
            if (targetIp == null)
            {
                Console.WriteLine($"Unable to resolve target system name {target}.");
                return;
            }

            var hostName = GetHostNameOrNull(targetIp);
            var targetName = hostName != null ? $"{hostName} [{targetIp}]" : targetIp.ToString();

            Console.WriteLine($"Tracing route to {targetName}");
            Console.WriteLine($"over a maximum of {maxHops} hops:\n");
            PrintNodes(targetIp, maxHops, timeout);
            Console.WriteLine("\nTrace complete.");
        }

        private static void PrintNodes(IPAddress targetIp, int maxHops, int timeout)
        {
            for (int ttl = 1; ttl <= maxHops; ++ttl)
            {
                Console.Write($"  {ttl}");
                IPAddress? nodeAddress;

                try
                {
                    nodeAddress = GetNodeIPAddress(ttl, targetIp, timeout);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine($"\t{e.Message}");
                    return;
                }

                if (nodeAddress == null)
                {
                    Console.WriteLine($"\t{TimeoutMessage}");
                    continue;
                }

                var hostName = GetHostNameOrNull(nodeAddress);
                var nodeName = hostName != null ? $"{hostName} [{nodeAddress}]" : nodeAddress.ToString();
                Console.WriteLine($"\t{nodeName}");

                if (nodeAddress.Equals(targetIp))
                    return;
            }
        }

        private static IPAddress? GetNodeIPAddress(int ttl, IPAddress targetIp, int timeout)
        {
            options.Ttl = ttl;
            IPAddress? nodeAddress = null;

            for (int i = 0; i < NumberOfPackets; ++i)
            {
                stopwatch.Restart();
                var reply = pingSender.Send(targetIp, timeout, buffer, options);
                stopwatch.Stop();

                switch (reply.Status)
                {
                    case IPStatus.TtlExpired:
                    case IPStatus.TimeExceeded:
                    case IPStatus.Success:
                        Console.Write($"\t{stopwatch.ElapsedMilliseconds} ms");
                        nodeAddress = reply.Address;
                        break;
                    case IPStatus.TimedOut:
                        Console.Write("\t*");
                        break;
                    default:
                        var message = ((int)reply.Status) == 11050 ? "General Failure" : reply.Status.ToString();
                        throw new InvalidOperationException(message);
                }
            }
            return nodeAddress;
        }

        private static IPAddress? GetIPAddressOrNull(string host)
        {
            try
            {
                return Dns.GetHostAddresses(host).First();
            }
            catch (Exception e) when (e is ArgumentException || e is SocketException)
            {
                return null;
            }
        }

        private static string? GetHostNameOrNull(IPAddress ip)
        {
            try
            {
                return Dns.GetHostEntry(ip).HostName;
            }
            catch (Exception e) when (e is ArgumentException || e is SocketException)
            {
                return null;
            }
        }
    }
}
