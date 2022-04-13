using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View handles the Kill Tracker View in the UI:
	/// - Shows the avatar and name of a player who killed another player. 
	/// </summary>
	public class KillHolderView : MonoBehaviour
	{
		[SerializeField] private KillTrackerView _killTrackerRef;

		private IObjectPool<KillTrackerView> _killTrackerPool;
		private IGameServices _services;
		
		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_killTrackerPool = new GameObjectPool<KillTrackerView>(10, _killTrackerRef);
			
			_killTrackerRef.gameObject.SetActive(false);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var view = _killTrackerPool.Spawn();
			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var deadData = callback.PlayersMatchData[callback.PlayerDead];

			view.transform.SetSiblingIndex(0);
			view.SetInfo(killerData.GetPlayerName(), killerData.Data.PlayerSkin, 
			             deadData.GetPlayerName(), deadData.Data.PlayerSkin, 
			             deadData.Data.Player == killerData.Data.Player);
		}
	}
}
