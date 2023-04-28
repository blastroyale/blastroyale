using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// The input structure is a 24 bits vector. Start reading this vector from right to left.
	/// - The first bit is the isShooting flag.
	/// - The next 9 bits are the movement from 0 to 360
	/// - The last 9 bits are uncompressed int angle ranging from 0 to 360 to have precision on aim.
	/// - Remaining 5 bits control the movement range
	///
	/// Note:
	/// Be super mindful of adding new data on input.
	/// One extra byte on input, means one extra byte per frame per player.
	/// So 30 players = 30 bytes, 60 frames per second its 1.8KB every second.
	/// A match that lasts for a minute is 1MB per minute of inputs just for an extra byte.
	/// Bear in mind that the client caches those inputs and we might want to save them for replays too.
	/// </summary>
	public partial struct Input
	{
		private static readonly UInt16 MASK_5_BITS = (1 << 5) - 1;
		private static readonly UInt16 MASK_9_BITS = (1 << 9) - 1;

		private static FP MovementMagMult = FP._0_04;
		
		public int CompressedInput => B1 << 16 | B2 << 8 | B3;
		public FP MovementMagnitude => FPMath.Min((B1 >> 3) * MovementMagMult, FP._1);
		public FPVector2 Direction => DecodeVector((CompressedInput >> 1) & MASK_9_BITS, 1);
		public FPVector2 AimingDirection => DecodeVector(CompressedInput >> 10 & MASK_9_BITS, 1);
		public bool IsShooting => (CompressedInput & 1) == 1;

		/// <summary>
		/// Called from client. Sets the compressed input from client input.
		/// </summary>
		public void SetInput(FPVector2 aim, FPVector2 movement, bool isShooting, FP movementRangePercentage)
		{
			var encodedMovement = (byte)Math.Floor(movementRangePercentage.AsDouble / 4d);
			var compressedInput = (encodedMovement << 19 | EncodeVector(aim, 1) << 10 | EncodeVector(movement, 1) << 1 | (isShooting ? 1 : 0));
			B1 = (byte)(compressedInput >> 16);
			B2 = (byte) (compressedInput >> 8);
			B3 = (byte) compressedInput;
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