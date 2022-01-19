using Photon.Deterministic;

namespace Quantum
{
	public unsafe static partial class BotSDKUtils
	{
		public static FP BotSDKGameTime(this Frame frame)
		{
			// Try to get a game time value from a user implementation
			// If the value is not greater thane zero, either because there is no implementation
			// or because it calculates the time wrongly, it will use the default Bot SDK
			// time polling calculus
			FP gameTime = -FP._1;
			CalculateBotSDKGameTime(frame, ref gameTime);
			if (gameTime >= FP._0)
			{
				return gameTime;
			}

			// We use division with integer in order to avoid accuracy issues with multiplications with DeltaTime
			return (FP)frame.Global->BotSDKData.ElapsedTicks / frame.SessionConfig.UpdateFPS;
		}

		public static FP BotSDKGameTime(this FrameThreadSafe frameThreadSafe)
		{
			FP gameTime = -FP._1;
			CalculateBotSDKGameTime(frameThreadSafe, ref gameTime);
			if (gameTime >= FP._0)
			{
				return gameTime;
			}

			Frame frame = (Frame)frameThreadSafe;
			return (FP)frame.Global->BotSDKData.ElapsedTicks / frame.SessionConfig.UpdateFPS;
		}

		// Method meant to be used for user implementation of the time counter, if needed
		// Store the calculation result on the gameTime variable. The result has to be greater than zero
		static partial void CalculateBotSDKGameTime(Frame frame, ref FP gameTime);
		static partial void CalculateBotSDKGameTime(FrameThreadSafe frameThreadSafe, ref FP gameTime);
	}
}
