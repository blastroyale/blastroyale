using UnityEngine;

namespace FirstLight.Game.Services
{
	public interface IGameFlowService
	{
		public string QuitReason { get; }
		public void QuitGame(string reason);
	}
	
	public class GameFlowService : IGameFlowService
	{
		public string QuitReason { get; set; }
		
		public void QuitGame(string reason)
		{
			QuitReason = reason;
			Application.Quit();
		}
	}
}