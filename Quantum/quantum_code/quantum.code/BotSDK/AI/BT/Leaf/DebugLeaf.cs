using System;

namespace Quantum
{
	[Serializable]
	public unsafe partial class DebugLeaf : BTLeaf
	{
		// ========== PUBLIC MEMBERS ==================================================================================

		public string Message;

		// ========== BTNode INTERFACE ================================================================================

		/// <summary>
		/// When Update is called, we just write a message on the console.
		/// This Leaf never fails, nor takes more than one frame to finish,
		/// so we always return Success.
		/// </summary>
		protected override BTStatus OnUpdate(BTParams btParams)
		{
			Log.Info(Message + " | Frame: " + btParams.Frame.Number);

			return BTStatus.Success;
		}
	}
}
