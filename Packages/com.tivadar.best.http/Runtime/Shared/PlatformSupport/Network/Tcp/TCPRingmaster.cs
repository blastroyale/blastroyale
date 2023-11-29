#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Net.Sockets;
using System.Threading;

using Best.HTTP.Shared.PlatformSupport.Network.DNS.Cache;
using Best.HTTP.Shared.PlatformSupport.Text;
using Best.HTTP.Shared.Logger;

namespace Best.HTTP.Shared.PlatformSupport.Network.Tcp
{
    /// <summary>
    /// Contains settings related to TCP Ringmaster, which manages and optimizes TCP connections.
    /// </summary>
    public sealed class TCPRingmasterSettings
    {
        /// <summary>
        /// The maximum number of simultaneous TCP racers. Racers are used to establish and manage connections.
        /// </summary>
        public int MaxSimultaneousRacers = 4;

        /// <summary>
        /// Determines whether to shuffle addresses before assigning racing lanes.
        /// </summary>
        public bool ShuffleAddresses = true;

        /// <summary>
        /// Callback to implement a custom address shuffle algorithm. When assigned, no plugin-defined shuffle algorithm will be executed.
        /// </summary>
        /// <remarks>It must be thread-safe.</remarks>
        public Action<TCPRaceParameters> CustomAddressShuffleAlgorithm;

        /// <summary>
        /// The granularity of cancellation checking for TCP races. It specifies the time interval for checking if a race should be canceled.
        /// </summary>
        public TimeSpan CancellationCheckingGranularity = TimeSpan.FromMilliseconds(100);
    }

    /// <summary>
    /// Represents the result of a TCP race competition, including the winning socket or an error.
    /// </summary>
    public sealed class TCPRaceResult
    {
        /// <summary>
        /// The socket that won the race competition, if available.
        /// </summary>
        public Socket WinningSocket;

        /// <summary>
        /// The error encountered during the race competition, if any.
        /// </summary>
        public Exception Error;

        /// <summary>
        /// Initializes a new instance of the <see cref="TCPRaceResult"/> class with the winning socket and an error, if any.
        /// </summary>
        /// <param name="socket">The winning socket of the race competition, if available.</param>
        /// <param name="ex">The error encountered during the race competition, if any.</param>
        public TCPRaceResult(Socket socket, Exception ex)
        {
            this.WinningSocket = socket;
            this.Error = ex;
        }
    }

    /// <summary>
    /// Represents a TCP race competition with parameters and status.
    /// </summary>
    sealed class Race
    {
        /// <summary>
        /// The parameters for the TCP race competition.
        /// </summary>
        public TCPRaceParameters Parameters;

        /// <summary>
        /// The index of the next address to connect to.
        /// </summary>
        public int NextAddressIndex;

        /// <summary>
        /// The number of running lanes in the competition.
        /// </summary>
        public int RunningLanes;
    }

    /// <summary>
    /// Represents a racing lane in a TCP race competition.
    /// </summary>
    sealed class RacingLane
    {
        /// <summary>
        /// The associated race and its parameters.
        /// </summary>
        public Race Race;

        /// <summary>
        /// The index of the address to connect to.
        /// </summary>
        public int AddressIndex;

        /// <summary>
        /// The index of the racing lane.
        /// </summary>
        public int LaneIndex;

        /// <summary>
        /// The socket used for the racing lane.
        /// </summary>
        public Socket Socket;
    }

    /// <summary>
    /// Contains parameters and settings for a TCP race competition to establish connections.
    /// </summary>
    public sealed class TCPRaceParameters
    {
        /// <summary>
        /// An array of DNS IP addresses to be used for racing to establish a connection.
        /// </summary>
        public DNSIPAddress[] Addresses;

        /// <summary>
        /// The hostname to connect to.
        /// </summary>
        public string Hostname;

        /// <summary>
        /// The port to connect to.
        /// </summary>
        public int Port;

        /// <summary>
        /// The cancellation token used to cancel the TCP race competition.
        /// </summary>
        public CancellationToken Token;

        /// <summary>
        /// A callback function to announce the winner of the TCP race competition.
        /// </summary>
        /// <param name="parameters">The TCPRaceParameters used for the race competition.</param>
        /// <param name="result">The result of the race competition, including the winning socket or an error.</param>
        public Action<TCPRaceParameters, TCPRaceResult> AnnounceWinnerCallback;

        /// <summary>
        /// Optional context for logging and tracking purposes.
        /// </summary>
        public LoggingContext Context;

        /// <summary>
        /// A user-defined tag associated with the TCP race parameters.
        /// </summary>
        public object Tag;

        public override string ToString()
        {
            var sb = StringBuilderPool.Get(2 + this.Addresses.Length);
            sb.Append('[');
            for (int i = 0; i < this.Addresses.Length; ++i)
            {
                sb.Append(this.Addresses[i]);

                if (i < this.Addresses.Length - 1)
                    sb.Append(", ");
            }
            sb.Append(']');
            var ips = StringBuilderPool.ReleaseAndGrab(sb);
            
            return $"[{nameof(TCPRaceParameters)} \"{this.Hostname}:{this.Port}\" Addresses({this.Addresses.Length}): {ips}]";
        }
    }

    /// <summary>
    /// <para>TCPRingmaster provides a method called <see cref="TCPRingmaster.StartCompetion(TCPRaceParameters)"/>, which is used to initiate a competition among multiple TCP connections.
    /// Each connection attempt races against the others to establish a connection, and the first successful connection is considered the winner.</para>
    /// The class allows to specify a callback function (through <see cref="TCPRaceParameters.AnnounceWinnerCallback"/>) that gets invoked when a winning connection is established or when the competition is canceled.
    /// This callback can be used to take action based on the competition outcome.
    /// <para>Additionally it includes logic for optimizing the order in which connection attempts are made:
    /// <list type="bullet">
    ///     <item><description>It can shuffle the order of addresses to improve the chances of quickly finding a working address.</description></item>
    ///     <item><description>It handles scenarios where some addresses may not be working and prioritizes working addresses.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    [Best.HTTP.Shared.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstruction]
    public static class TCPRingmaster
    {
        /// <summary>
        /// Starts a TCP race competition to establish connections based on the provided parameters.
        /// </summary>
        /// <param name="parameters">The parameters and settings for the TCP race competition.</param>
        public static void StartCompetion(TCPRaceParameters parameters)
        {
            // Initial idea:
            // create Min(racers.Addresses.Length, Options.MaxSimultaneousRacers) lanes
            //  and increase RunningLanes
            // randomize adresses
            // assign one address to each lane, put the remaining addresses' index to the IndexQueue
            // start connecting for each lane

            var options = HTTPManager.PerHostSettings.Get(parameters.Hostname);

            Race race = new Race
            {
                Parameters = parameters,

                NextAddressIndex = 0,
                RunningLanes = Math.Min(parameters.Addresses.Length, options.TCPRingmasterSettings.MaxSimultaneousRacers),
            };

            // Shuffle addresses?

            var algo = options.TCPRingmasterSettings.CustomAddressShuffleAlgorithm;
            if (algo != null)
            {
                try
                {
                    algo(parameters);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(TCPRingmaster), $"{nameof(options.TCPRingmasterSettings.CustomAddressShuffleAlgorithm)}({parameters})", ex, parameters.Context);
                }
            }
            else
            {
                // This helps in cases where the DNS query returns with a lot of addresses whos the first half isn't working.
                // In this case, after shuffling there's better chance that a working address can be found in the first MaxSimultaneousRacers lanes.
                if (options.TCPRingmasterSettings.ShuffleAddresses)
                    Shuffle(parameters.Addresses);
            }

            // Move non-working addresses to the end of the array, we will give them another chance
            // but others will have higher priority
            int nonWorkingCount = 0;
            for (int i = 0; i < parameters.Addresses.Length - 1; i++)
            {
                var address = parameters.Addresses[i];

                if (!address.IsWorkedLastTime)
                {
                    Array.Copy(parameters.Addresses, i + 1, parameters.Addresses, i, parameters.Addresses.Length - i - 1);
                    parameters.Addresses[parameters.Addresses.Length - 1] = address;

                    if (++nonWorkingCount >= parameters.Addresses.Length)
                        break;

                    i--;
                }
            }
            
            if (HTTPManager.Logger.IsDiagnostic)
                HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(StartCompetion)}({parameters}) - creating {race.RunningLanes} lane(s)", parameters.Context);

            for (; race.NextAddressIndex < race.RunningLanes; race.NextAddressIndex++)
            {
                var address = race.Parameters.Addresses[race.NextAddressIndex];
                var socket = new Socket(address.IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                var lane = new RacingLane
                {
                    AddressIndex = race.NextAddressIndex,
                    LaneIndex = race.NextAddressIndex,
                    Race = race,
                    Socket = socket
                };

                socket.BeginConnect(address.IPAddress, parameters.Port, OnLaneFinished, lane);
            }

            if (parameters.Token != CancellationToken.None)
                Extensions.Timer.Add(new Extensions.TimerData(options.TCPRingmasterSettings.CancellationCheckingGranularity, race, CheckForCanceled));
        }

        private static void OnLaneFinished(IAsyncResult ar)
        {
            var lane = ar.AsyncState as RacingLane;

            try
            {
                // Lane callback logic:
                //  Increase CompletedRacers
                //  If there's an error completing the socket, or isn't connected,
                //      pick the next index and start to connect
                //      if no more racers (CompletedRacers == racers.Addresses.Length) and no more running lanes, call callback with error
                //      decrease RunningLanes
                //  Else If callback isn't null, call callback

                lane.Socket.EndConnect(ar);

                var callback = lane.Race.Parameters.AnnounceWinnerCallback;
                callback = Interlocked.CompareExchange(ref lane.Race.Parameters.AnnounceWinnerCallback, null, callback);

                if (callback != null)
                {
                    var runningLanes = Interlocked.Decrement(ref lane.Race.RunningLanes);

                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) - Winner lane! Address: {lane.Race.Parameters.Addresses[lane.AddressIndex]}, remaining lanes: {runningLanes}", lane.Race.Parameters.Context);

                    try
                    {
                        callback.Invoke(lane.Race.Parameters, new TCPRaceResult(lane.Socket, null));
                    }
                    catch (Exception ex)
                    {
                        HTTPManager.Logger.Exception(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}({lane.LaneIndex}) - callback", ex, lane.Race.Parameters.Context);
                    }
                }
                else
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) - Late lane, disconnecting...", lane.Race.Parameters.Context);

                    // The callback is already called before this lane finished, we have no use for this socket, have to disconnect.
                    lane.Socket.Shutdown(SocketShutdown.Both);
                    lane.Socket.BeginDisconnect(false, OnSocketDisconnect, lane);
                }
            }
            catch (Exception ex)
            {
                lane.Socket.Dispose();

                DNSCache.ReportAsNonWorking(lane.Race.Parameters.Hostname, lane.Race.Parameters.Addresses[lane.AddressIndex].IPAddress, lane.Race.Parameters.Context);

                var nextIndex = Interlocked.Increment(ref lane.Race.NextAddressIndex);
                if (nextIndex < lane.Race.Parameters.Addresses.Length)
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) - Couldn't connect to address, grabbing new index: {nextIndex}", lane.Race.Parameters.Context);

                    lane.AddressIndex = nextIndex;
                    lane.Socket = new Socket(lane.Race.Parameters.Addresses[lane.AddressIndex].IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    lane.Socket.BeginConnect(lane.Race.Parameters.Addresses[lane.AddressIndex].IPAddress, lane.Race.Parameters.Port, OnLaneFinished, lane);
                }
                else
                {
                    // no more address to try, decrase the running lanes
                    var runningLanes = Interlocked.Decrement(ref lane.Race.RunningLanes);
                    if (runningLanes == 0)
                    {
                        // If no more lanes, try to call the callback.
                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) Race is over, calling callback if it's still available", lane.Race.Parameters.Context);

                        var callback = lane.Race.Parameters.AnnounceWinnerCallback;
                        callback = Interlocked.CompareExchange(ref lane.Race.Parameters.AnnounceWinnerCallback, null, callback);

                        try
                        {
                            callback?.Invoke(lane.Race.Parameters, new TCPRaceResult(null, ex));
                        }
                        catch(Exception nex)
                        {
                            HTTPManager.Logger.Exception(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) - callback", nex, lane.Race.Parameters.Context);
                        }
                    }
                    else if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnLaneFinished)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex}) Run out of free racers, there are still running racers({runningLanes}), retiring this lane.", lane.Race.Parameters.Context);
                }
            }
        }

        private static void OnSocketDisconnect(IAsyncResult ar)
        {
            var lane = ar.AsyncState as RacingLane;
            try
            {
                lane.Socket.EndDisconnect(ar);
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception(nameof(TCPRingmaster), $"{nameof(OnSocketDisconnect)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex})", ex, lane.Race.Parameters.Context);
            }
            finally
            {
                Interlocked.Decrement(ref lane.Race.RunningLanes);
                lane.Socket.Dispose();

                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(OnSocketDisconnect)}(Lane: {lane.LaneIndex}, Addr.: {lane.AddressIndex})", lane.Race.Parameters.Context);
            }
        }

        private static void Shuffle(DNSIPAddress[] addresses)
        {
            var rand = new Random();
            int n = addresses.Length;
            while (n > 1)
            {
                int k = rand.Next(n--);
                (addresses[n], addresses[k]) = (addresses[k], addresses[n]);
            }
        }

        private static bool CheckForCanceled(DateTime now, object context)
        {
            var query = context as Race;

            if (query.Parameters.Token.IsCancellationRequested)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Verbose(nameof(TCPRingmaster), $"{nameof(CheckForCanceled)}({query}) - Token.IsCancellationRequested!", query.Parameters.Context);

                var callback = query.Parameters.AnnounceWinnerCallback;

                callback = Interlocked.CompareExchange<Action<TCPRaceParameters, TCPRaceResult>>(ref query.Parameters.AnnounceWinnerCallback, null, callback);

                try
                {
                    callback?.Invoke(query.Parameters, new TCPRaceResult(null, new TimeoutException("DNS Query Timed Out")));
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(TCPRingmaster), $"{nameof(CheckForCanceled)}({query}) - callback", ex, query.Parameters.Context);
                }
            }

            return query.Parameters.AnnounceWinnerCallback != null;
        }
    }
}
#endif
