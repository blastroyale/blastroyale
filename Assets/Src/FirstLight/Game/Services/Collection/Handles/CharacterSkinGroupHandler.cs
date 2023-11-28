using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
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

		public bool CanHandle(ItemData item)
		{
			return SkinContainer.Skins.Any(s => s.GameId == item.Id);
		}

		public async Task<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			return await _assetResolver.LoadAssetByReference<Sprite>(skin.Sprite, true, instantiate);
		}


		public async Task<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(skin.Prefab, true, instantiate);
			if (!instantiate) return obj;
			var skinComponent = obj.GetComponent<CharacterSkinMonoComponent>();
			if (skinComponent != null) UpdateAnimator(obj, skinComponent, menuModel);
			var level = menuModel ? 0 : 2;

			foreach (var component in obj.GetComponents<Renderer>())
			{
				var text = component.material.mainTexture as Texture2D;
				if (text != null)
					text.ClearRequestedMipmapLevel();
					text.requestedMipmapLevel = level;
			}

			return obj;
		}

		private void UpdateAnimator(GameObject obj, CharacterSkinMonoComponent skinComponent, bool menu)
		{
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