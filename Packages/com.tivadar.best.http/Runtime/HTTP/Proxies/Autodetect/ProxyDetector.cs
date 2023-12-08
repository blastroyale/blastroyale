#if !UNITY_WEBGL || UNITY_EDITOR
using System;

using Best.HTTP.Shared;
using Best.HTTP.Hosts.Connections;
using Best.HTTP.Request.Timings;

namespace Best.HTTP.Proxies.Autodetect
{
    /// <summary>
    /// Interface for custom proxy-detection logic.
    /// </summary>
    public interface IProxyDetector
    {
        /// <summary>
        /// Receives the <see cref="HTTPRequest"/> instance this detector has to try to find a proxy.
        /// </summary>
        /// <param name="request"><see cref="HTTPRequest"/>instance to find a proxy for</param>
        /// <returns>A concrete <see cref="Proxy"/> implementation, or <c>null</c> if no proxy could be found.</returns>
        Proxy GetProxy(HTTPRequest request);
    }

    /// <summary>
    /// Possible detection modes the <see cref="ProxyDetector"/> can be in.
    /// </summary>
    public enum ProxyDetectionMode
    {
        /// <summary>
        /// In Continouos mode the ProxyDetector will check for a proxy for every request.
        /// </summary>
        Continouos,

        /// <summary>
        /// This mode will cache the first Proxy found and use it for consecutive requests.
        /// </summary>
        CacheFirstFound
    }

    /// <summary>
    /// Helper class to contain, manage and execute logic to detect available proxy on the network. It's a wrapper class to execute the various <see cref="IProxyDetector"/>s.
    /// </summary>
    public sealed class ProxyDetector
    {
        public static IProxyDetector[] GetDefaultDetectors() => new IProxyDetector[] {
                // HTTPManager.Proxy has the highest priority
                new ProgrammaticallyAddedProxyDetector(),

                // then comes the environment set
                new EnvironmentProxyDetector(),

                // .net framework's detector
                new FrameworkProxyDetector(),

#if UNITY_ANDROID && !UNITY_EDITOR
                new AndroidProxyDetector(),
#endif
            };

        private IProxyDetector[] _proxyDetectors;
        private ProxyDetectionMode _detectionMode;
        private bool _attached;

        public ProxyDetector()
            : this(ProxyDetectionMode.CacheFirstFound, GetDefaultDetectors())
        { }

        public ProxyDetector(ProxyDetectionMode detectionMode)
            :this(detectionMode, GetDefaultDetectors())
        { }

        public ProxyDetector(ProxyDetectionMode detectionMode, IProxyDetector[] proxyDetectors)
        {
            this._detectionMode = detectionMode;
            this._proxyDetectors = proxyDetectors;

            if (this._proxyDetectors != null)
                Reattach();
        }

        public void Reattach()
        {
            HTTPManager.Logger.Information(nameof(ProxyDetector), $"{nameof(Reattach)}({this._attached})");

            if (!this._attached)
            {
                RequestEventHelper.OnEvent += OnRequestEvent;
                this._attached = true;
            }
        }

        /// <summary>
        /// Call Detach() to disable ProxyDetector's logic to find and set a proxy.
        /// </summary>
        public void Detach()
        {
            HTTPManager.Logger.Information(nameof(ProxyDetector), $"{nameof(Detach)}({this._attached})");

            if (this._attached)
            {
                RequestEventHelper.OnEvent -= OnRequestEvent;
                this._attached = false;
            }
        }

        private void OnRequestEvent(RequestEventInfo @event)
        {
            // The Resend event is raised for every request when it's queued up (sent or redirected).
            if (@event.Event == RequestEvents.Resend && @event.SourceRequest.ProxySettings == null)
            {
                Uri uri = @event.SourceRequest.CurrentUri;

                if (uri.Scheme.Equals("file"))
                    return;

                @event.SourceRequest.Timing.StartNext(TimingEventNames.ProxyDetection);

                try
                {
                    for (int i = 0; i < this._proxyDetectors.Length; i++)
                    {
                        var detector = this._proxyDetectors[i];

                        if (detector == null)
                            continue;

                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"Calling {detector.GetType().Name}'s GetProxy", @event.SourceRequest.Context);

                        Proxy proxy = null;

#if ENABLE_PROFILER
                        using (var _ = new Unity.Profiling.ProfilerMarker($"{detector.GetType().Name}.GetProxy").Auto())
#endif
                            proxy = detector.GetProxy(@event.SourceRequest);

#if ENABLE_PROFILER
                        using (var _ = new Unity.Profiling.ProfilerMarker($"{detector.GetType().Name}.UseProxyForAddress").Auto())
#endif
                        if (proxy != null && proxy.UseProxyForAddress(uri))
                        {
                            if (HTTPManager.Logger.IsDiagnostic)
                                HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"[{detector.GetType().Name}] Proxy found: {proxy.Address} ", @event.SourceRequest.Context);

                            switch (this._detectionMode)
                            {
                                case ProxyDetectionMode.Continouos:
                                    @event.SourceRequest.ProxySettings.Proxy = proxy;
                                    break;

                                case ProxyDetectionMode.CacheFirstFound:
                                    HTTPManager.Proxy = @event.SourceRequest.ProxySettings.Proxy = proxy;

                                    HTTPManager.Logger.Verbose(nameof(ProxyDetector), $"Proxy cached in HTTPManager.Proxy!", @event.SourceRequest.Context);

                                    Detach();
                                    break;
                            }

                            return;
                        }
                    }

                    HTTPManager.Logger.Information(nameof(ProxyDetector), $"No Proxy for '{uri}'.", @event.SourceRequest.Context);
                }
                catch (Exception ex)
                {
                    if (HTTPManager.Logger.IsDiagnostic)
                        HTTPManager.Logger.Exception(nameof(ProxyDetector), $"GetProxyFor({@event.SourceRequest.CurrentUri})", ex, @event.SourceRequest.Context);
                }
                finally
                {
                    @event.SourceRequest.Timing.StartNext(TimingEventNames.Queued);
                }
            }
        }
    }
}

#endif
