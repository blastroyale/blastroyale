using System;
using System.Collections.Generic;

namespace FirstLight.Game.Domains.HomeScreen
{
	public enum HomeScreenForceBehaviourType : byte
	{
		None,
		Store,
		Matchmaking,
		PaidEvent,
	}

	/// <summary>
	/// Hack service to force behaviours when opening home screen
	/// this is due to the huge amount of complexity on the state machine, this is just a way of isolating weird code changing it here
	/// </summary>
	public interface IHomeScreenService
	{
		public event Action<List<string>> CustomPlayButtonValidations;
		public HomeScreenForceBehaviourType ForceBehaviour { get; }
		public object ForceBehaviourData { get; }
		public void SetForceBehaviour(HomeScreenForceBehaviourType type, object data = null);
		public List<string> ValidatePlayButton();
	}

	public class HomeScreenService : IHomeScreenService
	{
		public event Action<List<string>> CustomPlayButtonValidations;
		public HomeScreenForceBehaviourType ForceBehaviour { get; set; }
		public object ForceBehaviourData { get; set; }

		public void SetForceBehaviour(HomeScreenForceBehaviourType type, object data = null)
		{
			ForceBehaviour = type;
			ForceBehaviourData = data;
		}

		public List<string> ValidatePlayButton()
		{
			var errors = new List<string>();
			CustomPlayButtonValidations?.Invoke(errors);
			return errors;
		}
	}
}