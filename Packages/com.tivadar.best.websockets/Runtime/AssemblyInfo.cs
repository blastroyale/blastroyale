using System.Reflection;

[assembly: AssemblyVersion("3.0.0")]
[assembly: AssemblyCompany("Tivadar György Nagy")]
[assembly: AssemblyCopyright("Copyright ©  2023 Tivadar György Nagy")]
[assembly: AssemblyDescription("Best WebSockets is a premier networking library for Unity, tailored specifically for seamless WebSocket integration. It's perfect for applications that require real-time, bi-directional communication such as chat applications, multiplayer games, and live interactive systems.")]

#if BESTHTTP_WITH_BURST
[assembly: Unity.Burst.BurstCompile(CompileSynchronously = true, OptimizeFor = Unity.Burst.OptimizeFor.Performance)]
#endif
