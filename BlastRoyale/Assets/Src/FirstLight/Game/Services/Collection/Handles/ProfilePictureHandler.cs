using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class ProfilePictureHandler : ICollectionGroupHandler
	{
		private IConfigsProvider _configsProvider;
		private IAssetResolverService _assetResolver;
		private Dictionary<ItemData, Sprite> _cache = new ();

		private AvatarCollectableConfig Config => _configsProvider.GetConfig<AvatarCollectableConfig>();

		public ProfilePictureHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
		}

		public bool CanHandle(ItemData item)
		{
			return item.Id.IsInGroup(GameIdGroup.ProfilePicture) || Config.GameIdUrlDictionary.ContainsKey(item.Id);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken ct = default)
		{
			var avatarUrl = AvatarHelpers.GetAvatarUrl(item, Config);
			
			var services = MainInstaller.ResolveServices();
			var tex = await services.RemoteTextureService.RequestTexture(avatarUrl, cancellationToken: ct);
			return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
		}

		public UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			return default;
		}
	}
}