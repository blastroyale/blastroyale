using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class CurrencyPoolWidgetView : MonoBehaviour
	{
		[SerializeField] private GameId _currencyPoolToObserve;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}
		
		private void OnEnable()
		{
			UpdateView();
		}

		private void UpdateView()
		{
			// TODO - FIX
			var meme = _services.ConfigsProvider.GetConfigsList<CurrencyPoolConfig>();
			 Debug.LogError("meme");
		}
	}
}
