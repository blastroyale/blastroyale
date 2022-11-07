namespace Quantum
{
	[System.Serializable]
	public unsafe class DebugAction : AIAction
	{
		public string Message;

		public override void Update(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			Log.Info(Message);
		}
	}
}
