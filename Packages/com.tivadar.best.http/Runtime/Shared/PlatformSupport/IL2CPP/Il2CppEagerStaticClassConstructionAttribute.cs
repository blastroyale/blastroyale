using System;

namespace Best.HTTP.Shared.PlatformSupport.IL2CPP
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class Il2CppEagerStaticClassConstructionAttribute : Attribute
    {
    }
}
