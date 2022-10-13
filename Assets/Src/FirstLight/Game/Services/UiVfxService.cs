using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.Events;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service allows to play multiple VFX in the UI screen
	/// </summary>
	public interface IUiVfxService
	{
		/// <inheritdoc cref="UiVfxPresenter.PlayAnimation(UnityEngine.Sprite,UnityEngine.Vector3,UnityEngine.Vector3,UnityEngine.Events.UnityAction)"/>
		void PlayVfx(GameId id, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition, UnityAction onCompleteCallback);

		/// <summary>
		/// Plays the Floating Text animation with the given <paramref name="text"/>.
		/// </summary>
		void PlayFloatingText(string text);

		/// <summary>
		/// Plays the Floating Text animation with the given <paramref name="text"/> at the given <paramref name="position"/>.
		/// </summary>
		void PlayFloatingTextAtPosition(string text, Vector3 position);
	}
	
	/// <inheritdoc />
	/// <remarks>
	/// Used only on internal creation data and should not be exposed to the views
	/// </remarks>
	public interface IUiVfxInternalService : IUiVfxService
	{
		/// <summary>
		/// Initializes the <see cref="IUiVfxService"/> with all it's necessary data
		/// </summary>
		void Init(IUiService service);
	}
	
	/// <inheritdoc />
	public class UiVfxService : IUiVfxInternalService
	{
		private readonly IAssetResolverService _assetResolver;
		
		private UiVfxPresenter _presenter;
		
		public UiVfxService(IAssetResolverService assetResolverService)
		{
			_assetResolver = assetResolverService;
		}

		/// <inheritdoc />
		public void Init(IUiService service)
		{
			_presenter = service.GetUi<UiVfxPresenter>();
		}

		/// <inheritdoc />
		public async void PlayVfx(GameId id, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition, 
		                    UnityAction onCompleteCallback)
		{
			var sprite = await _assetResolver.RequestAsset<GameId, Sprite>(id);

			_presenter.Validate<UiVfxPresenter>()?.PlayAnimation(sprite, delay, originWorldPosition, targetWorldPosition, 
			                                                     onCompleteCallback);
		}

		/// <inheritdoc />
		public void PlayFloatingText(string text)
		{
			_presenter.Validate<UiVfxPresenter>()?.PlayFloatingText(text);
		}
		
		/// <inheritdoc />
		public void PlayFloatingTextAtPosition(string text, Vector3 position)
		{
			_presenter.Validate<UiVfxPresenter>()?.PlayFloatingText(text, position);
		}
	}
}