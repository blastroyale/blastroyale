
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// Interface to read any liveops player-specific data that is stored to the player
	/// regarding his liveops state such as which actions he has already triggered.
	/// </summary>
	public interface ILiveopsDataProvider
	{
		/// <summary>
		/// Checks if a given action was already triggered
		/// </summary>
		bool HasTriggeredSegmentationAction(int actionIdentifier);

	}
	
	/// <summary>
	/// Logic to handle liveops data
	/// </summary>
	public interface ILiveopsLogic : ILiveopsDataProvider
	{
		/// <summary>
		/// Adds a triggered action to the triggered action list.
		/// </summary>
		void MarkTriggeredSegmentationAction(int actionIdentifier);
	}

	/// <inheritdoc cref="IPlayerLogic"/>
	public class LiveopsLogic : AbstractBaseLogic<LiveopsData>, ILiveopsLogic, IGameLogicInitializer
	{
		private IObservableList<int> _triggeredActions;

		/// <inheritdoc />
		public IObservableListReader<int> TriggeredActions => _triggeredActions;

		public void Init()
		{
			_triggeredActions = new ObservableList<int>(Data.TriggeredActions);
		}

		public void ReInit()
		{
			{
				var listeners = _triggeredActions.GetObservers();
				_triggeredActions = new ObservableList<int>(Data.TriggeredActions);
				_triggeredActions.AddObservers(listeners);
			}
			
			_triggeredActions.InvokeUpdate();
		}

		public LiveopsLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public bool HasTriggeredSegmentationAction(int actionIdentifier)
		{
			return Data.TriggeredActions.Contains(actionIdentifier);
		}


		public void MarkTriggeredSegmentationAction(int actionIdentifier)
		{
			_triggeredActions.Add(actionIdentifier);
		}
	}
}