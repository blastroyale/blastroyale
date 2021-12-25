using System;

namespace Quantum
{
	/// <summary>
	/// Using this system is optional. It is only used to aim the Debugger on the Unity side.
	/// It is also safe to copy logic from this system into your own systems, if it better suits your architecture.
	/// </summary>
	public class BotSDKDebuggerSystem : SystemMainThread
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		// Used for DEBUGGING purposes only
		public static Action<Frame> OnVerifiedFrame;
		public static Action<EntityRef, string> SetEntityDebugLabel;

		// ========== PUBLIC METHODS ==================================================================================

		/// <summary>
		/// Use this to add an entity to the Debugger Window on Unity.
		/// You can provide a custom label of your preference if you want to identify your bots in a custom way.
		/// Use the separator '/' on the custom label if you want to create an Hierarchy on the Debugger Window.
		/// </summary>
		public static void AddToDebugger(EntityRef entity, string customLabel = default)
		{
			if (SetEntityDebugLabel != null)
			{
				SetEntityDebugLabel(entity, customLabel);
			}
		}

		// ========== SystemMainThread INTERFACE ======================================================================

		public override void Update(Frame frame)
		{
			if (frame.IsVerified)
			{
				OnVerifiedFrame?.Invoke(frame);
			}
		}
	}
}
