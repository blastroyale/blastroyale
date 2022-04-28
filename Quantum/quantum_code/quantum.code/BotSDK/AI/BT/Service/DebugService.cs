using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class DebugService : BTService
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Message;

		// ========== BTService INTERFACE =============================================================================

		protected unsafe override void OnUpdate(BTParams btParams)
		{
			Log.Info($"[BT SERVICE] { Message } | Frame: {btParams.Frame.Number}");
		}
	}
}
