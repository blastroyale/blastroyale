using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.Flags.View;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class FlagGroupHandler : ICollectionGroupHandler
	{
		private IConfigsProvider _configsProvider;
		private IAssetResolverService _assetResolver;
		private FlagSkinConfig SkinContainer => _configsProvider.GetConfig<FlagSkinConfig>();

		public FlagGroupHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
		}

		public bool CanHandle(ItemData item)
		{
			return SkinContainer.Skins.Any(s => s.GameId == item.Id);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate, CancellationToken token = default)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			return await _assetResolver.LoadAssetByReference<Sprite>(skin.Sprite, true, instantiate, cancellationToken: token);
		}

		public async UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			var flag = Object.Instantiate(SkinContainer.FlagPrefab);
			var view = flag.GetComponent<DeathFlagView>();
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			var mesh = await _assetResolver.LoadAssetByReference<Mesh>(skin.Mesh, true, false);
			view.Initialise(mesh);
			return view.gameObject;
		}
	}
}