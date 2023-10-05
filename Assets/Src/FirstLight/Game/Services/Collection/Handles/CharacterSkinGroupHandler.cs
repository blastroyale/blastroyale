using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class CharacterSkinGroupHandler : ICollectionGroupHandler
	{
		private IConfigsProvider _configsProvider;
		private IAssetResolverService _assetResolver;
		private CharacterSkinsConfig SkinContainer => _configsProvider.GetConfig<CharacterSkinsConfig>();

		public CharacterSkinGroupHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
		}

		public bool CanHandle(GameId id)
		{
			return SkinContainer.Skins.Any(s => s.GameId == id);
		}

		public async Task<Sprite> LoadCollectionItemSprite(GameId id, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == id);
			return await _assetResolver.LoadAssetByReference<Sprite>(skin.Sprite, true, instantiate);
		}


		public async Task<GameObject> LoadCollectionItem3DModel(GameId id, bool menuModel = false, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == id);
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(skin.Prefab, true, instantiate);
			if (!instantiate) return obj;
			var skinComponent = obj.GetComponent<CharacterSkinMonoComponent>();
			// Check animators
			UpdateAnimator(obj, skinComponent, menuModel);

			return obj;
		}

		private void UpdateAnimator(GameObject obj, CharacterSkinMonoComponent skinComponent, bool menu)
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
	}
}