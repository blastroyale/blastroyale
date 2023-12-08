#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

#if BESTHTTP_WITH_BURST
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86;
using static Unity.Burst.Intrinsics.Arm;
#endif

namespace Best.HTTP.Shared.TLS.Crypto.Impl
{
#if BESTHTTP_WITH_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal static class FastSalsa20EngineHelper
    {
#if BESTHTTP_WITH_BURST
        [Unity.Burst.BurstCompile]
        public unsafe static void ProcessBytes([Unity.Burst.NoAlias] byte* outBytes, int outOff, [Unity.Burst.NoAlias] byte* inBytes, int inOff, [Unity.Burst.NoAlias] byte* keyStream)
        {
            //for (int i = 0; i < 64; ++i)
            //    outBytes[idx + i + outOff] = (byte)(keyStream[i] ^ inBytes[idx + i + inOff]);

            if (Sse2.IsSse2Supported)
            {
                for (int offset = 0; offset < 64; offset += 16)
                {
                    var vin = Sse2.loadu_si128(inBytes + inOff + offset);
                    //var vout = Sse2.loadu_si128(outBytes + outOff + offset);
                    var vkeyStream = Sse2.loadu_si128(keyStream + offset);

                    var vout = Sse2.xor_si128(vkeyStream, vin);

                    Sse2.storeu_si128(outBytes + outOff + offset, vout);
                }
            }
            else if (Neon.IsNeonSupported)
            {
                for (int offset = 0; offset < 64; offset += 16)
                {
                    var vin = Neon.vld1q_u8(inBytes + inOff + offset);
                    var vkeyStream = Neon.vld1q_u8(keyStream + offset);

                    var vOut = Neon.veorq_u8(vkeyStream, vin);

                    Neon.vst1q_u8(outBytes + outOff + offset, vOut);
                }
            }
            else
            {
                ulong* pulOut = (ulong*)&outBytes[outOff];
                ulong* pulIn = (ulong*)&inBytes[inOff];
                ulong* pulKeyStream = (ulong*)keyStream;

                pulOut[0] = pulKeyStream[0] ^ pulIn[0];
                pulOut[1] = pulKeyStream[1] ^ pulIn[1];
                pulOut[2] = pulKeyStream[2] ^ pulIn[2];
                pulOut[3] = pulKeyStream[3] ^ pulIn[3];
            }
        }
#endif

    }
}
#endif
