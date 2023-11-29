using System;

namespace Best.HTTP.Shared.PlatformSupport.IL2CPP
{
    /// <summary>
    /// https://docs.unity3d.com/Manual/ManagedCodeStripping.html
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly |
        AttributeTargets.Class |
        AttributeTargets.Constructor |
        AttributeTargets.Delegate |
        AttributeTargets.Enum |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Property |
        AttributeTargets.Struct)]
    public sealed class PreserveAttribute : Attribute {}
}
