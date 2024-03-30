using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.Services
{
	public class UIVFXService
	{
		private readonly IAssetResolverService _assetResolver;
		private readonly IGameServices _services;

		private UIVFXScreenPresenter _presenter;

		public UIVFXService(IGameServices gameServices, IAssetResolverService assetResolverService)
		{
			_assetResolver = assetResolverService;
			_services = gameServices;
		}

		public async UniTask Init()
		{
			_presenter = await _services.UIService.OpenScreen<UIVFXScreenPresenter>();
		}

		public async void PlayVfx(GameId id, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition,
								  UnityAction onCompleteCallback)
		{
			var sprite = await _assetResolver.RequestAsset<GameId, Sprite>(id);

			_presenter.PlayAnimation(sprite, delay, originWorldPosition, targetWorldPosition, onCompleteCallback);
		}

		public void PlayFloatingText(string text)
		{
			_presenter.Validate<UIVFXScreenPresenter>()?.PlayFloatingText(text);
		}

		public void PlayFloatingTextAtPosition(string text, Vector3 position)
		{
			_presenter.PlayFloatingText(text, position);
		}
	}
}