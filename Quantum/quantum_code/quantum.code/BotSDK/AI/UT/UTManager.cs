using System;

namespace Quantum
{
	public unsafe static partial class UTManager
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public static Action<EntityRef, string> SetupDebugger;

		public static Action<EntityRef, long> ConsiderationChosen;
		public static Action<EntityRef> OnUpdate;
	}
}
