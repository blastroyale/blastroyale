namespace Quantum
{
	public unsafe partial class HFSMState
	{
		/// <summary>
		/// Invokes the <see cref="AIAction.PreUpdate"/> call of "Update Actions".
		/// </summary>
		internal void PreUpdateState(Frame f, EntityRef e)
		{
			Parent?.PreUpdateState(f, e);
			
			for (int i = 0; i < OnUpdate.Length; i++) 
			{
				OnUpdate[i].PreUpdate(f, e);
				
				var nextAction = OnUpdate[i].NextAction(f, e);
				
				if (nextAction > i) 
				{
					i = nextAction;
				}
			}
		}
	}
}