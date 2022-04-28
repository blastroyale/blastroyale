using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Used to display how many contenders are left within the Battle Royale.
	/// </summary>
	public class ContendersLeftHolderView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _contendersLeftText;
		[SerializeField, Required] private Animation _animation;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private int _playersLeft;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_contendersLeftText.text = _services.NetworkService.QuantumClient.CurrentRoom.MaxPlayers.ToString();
			
			QuantumEvent.Subscribe<EventOnPlayerDead>(this, OnEventOnPlayerDead);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		private void OnEventOnPlayerDead(EventOnPlayerDead callback)
		{
			var container = callback.Game.Frames.Verified.GetSingleton<GameContainer>();
			
			_contendersLeftText.text = (container.TargetProgress - container.CurrentProgress + 1).ToString();
			
			_animation.Rewind();
			_animation.Play();
		}
	}
}
