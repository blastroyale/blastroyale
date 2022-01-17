using Photon.Deterministic;

namespace Quantum
{
	public unsafe class BotSDKTimerSystem : SystemMainThread
	{
		// ========== SystemMainThread INTERFACE ======================================================================

		public override void OnInit(Frame frame)
		{
			// We store the game's initial delta time so we can use it to further know
			// if the delta time was changed (i.e to create slowdown or speedups)
			frame.Global->BotSDKData.OriginalDeltaTime = FP._1 / frame.SessionConfig.UpdateFPS;
		}

		public override void Update(Frame frame)
		{
			BotSDKData* botSDKData = &frame.Global->BotSDKData;

			// If the delta time was not changed, this tick counts as 1
			if (botSDKData->OriginalDeltaTime == frame.DeltaTime)
			{
				botSDKData->ElapsedTicks += 1;
			}
			else
			{
				// If the delta time was changed, we accumulate it as a "partial tick" value
				// Once the partial tick value reaches at least 1 tick, we get the integer part of it
				// to add to actual elapsed ticks
				// Basically, if the simulation is running at half of the initial delta time, it will take double the time for one
				// elapsed tick to be counted
				botSDKData->ElapsedPartialTicks += frame.DeltaTime * frame.SessionConfig.UpdateFPS;
				if (botSDKData->ElapsedPartialTicks >= 1)
				{
					var integerPart = FPMath.FloorToInt(botSDKData->ElapsedPartialTicks);
					botSDKData->ElapsedTicks += integerPart;
					botSDKData->ElapsedPartialTicks -= integerPart;
				}
			}
		}
	}
}
