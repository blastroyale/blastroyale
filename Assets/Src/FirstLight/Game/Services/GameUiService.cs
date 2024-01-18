using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using FirstLight.UiService;

namespace FirstLight.Game.Services
{
	/// <inheritdoc />
	/// <remarks>
	/// Game custom implementation of the <see cref="IUiService"/>
	/// </remarks>
	public interface IGameUiService : IUiService
	{
		/// <summary>
		/// Loads asynchronously a set of <see cref="UiPresenter"/> defined by the given <paramref name="uiSetId"/>.
		/// It will update the <see cref="LoadingScreenPresenter"/> value based on the current loading status to a max
		/// value defined by the given <paramref name="loadingCap"/>
		/// </summary>
		UniTask LoadGameUiSet(UiSetId uiSetId, float loadingCap);
	}

	/// <inheritdoc cref="IGameUiService"/>
	public interface IGameUiServiceInit : IGameUiService, IUiServiceInit
	{
	}
	
	/// <inheritdoc cref="IGameUiService"/>
	public class GameUiService : UiService.UiService, IGameUiServiceInit
	{
		public const int MinDefaultLayer = -3;
		public const int MaxDefaultLayer = 10;
		
		public GameUiService(IUiAssetLoader assetLoader) : base(assetLoader)
		{
			AddLayers(MinDefaultLayer, MaxDefaultLayer);
		}
		
		/// <inheritdoc />
		public async UniTask LoadGameUiSet(UiSetId uiSetId, float loadingCap)
		{
			await LoadUiSetAsync((int) uiSetId);
		}
	}
}