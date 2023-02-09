using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// The input structure is a 16 bits vector. Start reading this vector from right to left.
	/// - The first bit is the isShooting flag.
	/// - The next 6 bits are the movement, up to 45 which is compressed by a factor of 8.
	/// - The last 9 bits are uncompressed int angle ranging from 0 to 360 to have precision on aim.
	///
	/// Note:
	/// Be super mindful of adding new data on input.
	/// One extra byte on input, means one extra byte per frame per player.
	/// So 30 players = 30 bytes, 60 frames per second its 1.8KB every second.
	/// A match that lasts for a minute is 1MB per minute of inputs just for an extra byte.
	/// Bear in mind that the client caches those inputs and we might want to save them for replays too.
	/// </summary>
	public unsafe partial struct Input
	{
		private static readonly UInt16 MASK_6_BITS = (1 << 6) - 1;
		private static readonly UInt16 MASK_9_BITS = (1 << 9) - 1;
		
		public FPVector2 Direction => DecodeVector((CompressedInput >> 1) & MASK_6_BITS, 8);
		public FPVector2 AimingDirection => DecodeVector(CompressedInput >> 7 & MASK_9_BITS, 1);
		public bool IsShooting => (CompressedInput & 1) == 1;

		/// <summary>
		/// Called from client. Sets the compressed input from client input.
		/// </summary>
		public void SetInput(FPVector2 aim, FPVector2 movement, bool isShooting)
		{
			CompressedInput = (UInt16)(EncodeVector(aim, 1) << 7 | EncodeVector(movement, 8) << 1 | (isShooting ? 1 : 0));
		}
		
		private static int EncodeVector(FPVector2 dir, int divisor)
		{
			if (dir == FPVector2.Zero)
			{
				return byte.MinValue;
			}
			var angle = (FPVector2.RadiansSigned(FPVector2.Up, dir) * FP.Rad2Deg + 360) % 360;
			var encodedAngle = FPMath.RoundToInt(angle / divisor + 1);
			return encodedAngle;
		}

		private static FPVector2 DecodeVector(int encoded, int divisor)
		{
			if (encoded == byte.MinValue)
			{
				return FPVector2.Zero;
			}
			return FPVector2.Rotate(FPVector2.Up, ((encoded - 1) * divisor) * FP.Deg2Rad);
		}
	}
}