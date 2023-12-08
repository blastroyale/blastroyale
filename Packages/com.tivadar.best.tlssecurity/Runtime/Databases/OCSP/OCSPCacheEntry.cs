#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

namespace Best.TLSSecurity.Databases.OCSP
{
    public enum Status : byte
    {
        Good,
        Revoked,
        Unknown
    }

    public sealed class OCSPCacheEntry
    {
        public Status Status { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime NextUpdate { get; set; }

        public DateTime LastUsed { get; set; }

        public override string ToString()
        {
            return $"[OCSPCacheEntry Status: {Status.ToString()}, ReceivedAt: {ReceivedAt.ToString(System.Globalization.CultureInfo.InvariantCulture)}, GeneratedAt: {GeneratedAt.ToString(System.Globalization.CultureInfo.InvariantCulture)}, NextUpdate: {NextUpdate.ToString(System.Globalization.CultureInfo.InvariantCulture)}, LastUsed: {LastUsed.ToString(System.Globalization.CultureInfo.InvariantCulture)}]";
        }
    }
}
#endif
