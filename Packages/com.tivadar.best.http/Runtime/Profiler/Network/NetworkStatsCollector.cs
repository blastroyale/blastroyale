using System;
using System.Threading;

namespace Best.HTTP.Profiler.Network
{
    public static class NetworkStatsCollector
    {
        public static long TotalNetworkBytesReceived { get => _totalNetworkBytesReceived; }
        private static long _totalNetworkBytesReceived;
        
        public static long TotalNetworkBytesSent { get => _totalNetworkBytesSent; }
        private static long _totalNetworkBytesSent;

        public static int TotalConnections { get => _totalConnections; }
        private static int _totalConnections;

        public static int OpenConnections { get => _openConnections; }
        private static int _openConnections;

        public static int BufferedToSend { get => _bufferedToSend; }
        private static int _bufferedToSend;

        public static int ReceivedAndUnprocessed { get => _receivedAndUnprocessed; }
        private static int _receivedAndUnprocessed;

        internal static void IncrementCurrentConnections()
        {
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _openConnections);
        }

        internal static void DecrementCurrentConnections() => Interlocked.Decrement(ref _openConnections);

        internal static void IncrementTotalNetworkBytesReceived(int amount) => Interlocked.Add(ref _totalNetworkBytesReceived, amount);

        internal static void IncrementTotalNetworkBytesSent(int amount) => Interlocked.Add(ref _totalNetworkBytesSent, amount);

        internal static void IncrementBufferedToSend(int amount) => Interlocked.Add(ref _bufferedToSend, amount);
        internal static void IncrementReceivedAndUnprocessed(int amount) => Interlocked.Add(ref _receivedAndUnprocessed, amount);

        internal static void ResetNetworkStats()
        {
            Interlocked.Exchange(ref _totalNetworkBytesReceived, 0);
            Interlocked.Exchange(ref _totalNetworkBytesSent, 0);

            Interlocked.Exchange(ref _totalConnections, 0);
            Interlocked.Exchange(ref _openConnections, 0);

            Interlocked.Exchange(ref _bufferedToSend, 0);
            Interlocked.Exchange(ref _receivedAndUnprocessed, 0);
        }
    }
}
