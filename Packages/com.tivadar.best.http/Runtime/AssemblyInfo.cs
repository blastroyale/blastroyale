using System.Reflection;

[assembly: AssemblyVersion("3.0.0")]
[assembly: AssemblyCompany("Tivadar György Nagy")]
[assembly: AssemblyCopyright("Copyright ©  2023 Tivadar György Nagy")]
[assembly: AssemblyDescription("Best HTTP is a versatile and efficient HTTP client library for Unity, designed for making HTTP requests, handling responses, and providing advanced features such as asynchronous requests, compression, timing analysis, and more.")]

#if BESTHTTP_WITH_BURST
[assembly: Unity.Burst.BurstCompile(CompileSynchronously = true, OptimizeFor = Unity.Burst.OptimizeFor.Performance)]
#endif
