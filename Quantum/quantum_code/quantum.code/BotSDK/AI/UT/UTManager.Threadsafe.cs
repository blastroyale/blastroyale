using System;

namespace Quantum
{
	public unsafe static partial class UTManager
	{
		public class ThreadSafe
		{
			// ========== PUBLIC METHODS ==================================================================================

			/// <summary>
			/// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
			/// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global
			/// </summary>
			/// <param name="frame"></param>
			/// <param name="reasoner"></param>
			/// <param name="entity"></param>
			public static void Update(FrameThreadSafe frame, UtilityReasoner* reasoner, EntityRef entity = default)
			{
				if (entity != default)
				{
					OnUpdate?.Invoke(entity);
				}

				if (reasoner == default && entity != default)
				{
					reasoner = &frame.GetPointer<UTAgent>(entity)->UtilityReasoner;
				}

				reasoner->Update(frame, reasoner, entity);
			}
		}
	}
}
