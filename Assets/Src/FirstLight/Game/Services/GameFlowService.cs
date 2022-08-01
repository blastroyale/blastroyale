using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service that provides game flow tools
	/// </summary>
	public interface IGameFlowService
	{
		/// <summary>
		/// Reason why we quit
		/// </summary>
		public string QuitReason { get; }
		
		/// <summary>
		/// Method used when we want to leave the app, so we can record the reason
		/// </summary>
		/// <param name="reason">Reason why we quit the app</param>
		public void QuitGame(string reason);
	}
	
	/// <inheritdoc />
	public class GameFlowService : IGameFlowService
	{
		/// <inheritdoc />
		public string QuitReason { get; set; }
		
		/// <inheritdoc />
		public void QuitGame(string reason)
		{
			QuitReason = reason;
			Application.Quit();
		}
	}
}