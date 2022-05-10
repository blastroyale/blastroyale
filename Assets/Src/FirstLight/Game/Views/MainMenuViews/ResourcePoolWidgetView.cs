using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	public class ResourcePoolWidgetView : MonoBehaviour
	{
		[SerializeField] private GameId _poolToObserve = GameId.CS;

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private ResourcePoolConfig _resourcePool;
		
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
			var configs = _services.ConfigsProvider.GetConfigsList<ResourcePoolConfig>();
		}
	}
}
