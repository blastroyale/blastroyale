using System;

namespace Quantum
{
	public unsafe static partial class UTManager
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public static Action<EntityRef, string> SetupDebugger;

		public static Action<EntityRef, long> ConsiderationChosen;
		public static Action<EntityRef> OnUpdate;

		// ========== PUBLIC METHODS ==================================================================================

		/// <summary>
		/// Initializes the Utility Reasoner, allocating all frame data needed.
		/// If no UTRoot asset is passed by parameter, it will try to initialize with one already set on the Component, if any.
		/// </summary>
		public static void Init(Frame frame, UtilityReasoner* reasoner, AssetRefUTRoot utRootRef = default, EntityRef entity = default)
		{
			reasoner->Initialize(frame, utRootRef, entity);
		}

		public static void Free(Frame frame, UtilityReasoner* reasoner)
		{
			reasoner->Free(frame);
		}

		/// <summary>
		/// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
		/// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="reasoner"></param>
		/// <param name="entity"></param>
		public static void Update(Frame frame, UtilityReasoner* reasoner, EntityRef entity = default)
		{
			ThreadSafe.Update((FrameThreadSafe)frame, reasoner, entity);
		}
	}
}
