using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Input
	{
		// These numbers are used to distinct the states and can be anything except 0
		// because 0 is the default initial state 
		public const byte DownState = 1;
		public const byte ReleaseState = 2;
		
		public FPVector2 Direction
		{
			get => DecodeDirection(MoveDirectionEncoded);
			set => MoveDirectionEncoded = EncodeDirection(value);
		}
		
		public FPVector2 AimingDirection
		{
			get => DecodeDirection(AimingDirectionEncoded);
			set => AimingDirectionEncoded = EncodeDirection(value);
		}

		public bool IsMoveButtonDown => MoveDirectionEncoded > byte.MinValue;
		
		public bool IsShootButtonDown => AimButtonState == DownState;
		public bool IsShootButtonReleased => AimButtonState == ReleaseState;

		/// <summary>
		/// Encodes a FPVector2 direction to a single byte.
		/// </summary>
		/// <remarks>
		/// This is lossy encoding. The decoded angle will have a potential error of (+/-)1 degree and the
		/// magnitude of the original direction will be lost.
		/// </remarks>
		private static byte EncodeDirection(FPVector2 dir)
		{
			if (dir == FPVector2.Zero)
			{
				return byte.MinValue;
			}

			var angle = FPVector2.RadiansSigned(FPVector2.Up, dir) * FP.Rad2Deg;
				
			// make sure we are in the range [0, 360]
			angle = (angle + 360) % 360;
				
			// compress to 180 values and offset by 1 to allow for 0 = no input
			var encodedAngle = FPMath.RoundToInt((angle / 2) + 1);
			return (byte)encodedAngle;
		}

		private static FPVector2 DecodeDirection(byte encoded)
		{
			if (encoded == byte.MinValue)
			{
				return FPVector2.Zero;
			}

			var angle = ((int)encoded - 1) * 2;
			return FPVector2.Rotate(FPVector2.Up, angle * FP.Deg2Rad);
		}
	}
}