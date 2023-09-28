using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.MonoComponent;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Services
{
	public interface ICollectionService
	{
		Task<GameObject> LoadCollectionItem3DModel(GameId id, bool menuModel = false, bool instantiate = true);
		Task<Sprite> LoadCollectionItemSprite(GameId id, bool instantiate = true);
	}

	public class CollectionService : ICollectionService
	{
		private IAssetResolverService _assetResolver;
		private IConfigsProvider _configsProvider;
		private CharacterSkinsConfig SkinContainer => _configsProvider.GetConfig<CharacterSkinsConfig>();

		public CollectionService(IAssetResolverService assetResolver, IConfigsProvider configsProvider)
		{
			_assetResolver = assetResolver;
			_configsProvider = configsProvider;
		}


		public async Task<Sprite> LoadCollectionItemSprite(GameId id, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == id);
			if (skin.GameId == id)
			{
				return await _assetResolver.LoadAssetByReference<Sprite>(skin.Sprite, true, instantiate);
			}

			return await _assetResolver.RequestAsset<GameId, Sprite>(id, true, instantiate);
		}

		public async Task<GameObject> LoadCollectionItem3DModel(GameId id, bool menuModel = false, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == id);
			if (skin.GameId == id)
			{
				return await CreateCharacterSkin(id, menuModel, instantiate);
			}

			return await _assetResolver.RequestAsset<GameId, GameObject>(id, true, instantiate);
		}


		private async Task UpdateAnimator(GameObject obj, CharacterSkinMonoComponent skinComponent, bool menu)
		{
			// Copy default animators values
			var defaultValues = menu ? SkinContainer.MenuDefaultAnimation : SkinContainer.InGameDefaultAnimation;
			var animator = obj.GetComponent<Animator>();
			animator.runtimeAnimatorController = menu switch
			{
				true when skinComponent.MenuController != null    => skinComponent.MenuController,
				false when skinComponent.InGameController != null => skinComponent.InGameController,
				_                                                 => defaultValues.Controller
			};

			animator.applyRootMotion = defaultValues.ApplyRootMotion;
			animator.updateMode = defaultValues.UpdateMode;
			animator.cullingMode = defaultValues.CullingMode;
		}

		public async Task<GameObject> CreateCharacterSkin(GameId skinId, bool menu = false, bool instantiate = true)
		{
		
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == skinId);
			if (skin.GameId != skinId)
			{
				return null;
			}

			var obj = await _assetResolver.LoadAssetByReference<GameObject>(skin.Prefab, true, instantiate);
			if (!instantiate) return obj;
			var skinComponent = obj.GetComponent<CharacterSkinMonoComponent>();
			// Check animators
			await UpdateAnimator(obj, skinComponent, menu);

			return obj;
		}
	}
}