using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This View shows any Emoji broadcast from players during the game.
	/// </summary>
	public class EmojiContainerView : MonoBehaviour
	{
		[SerializeField] private EmojiView _emojiViewRef;
		
		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private IObjectPool<EmojiPoolObject> _pool;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_pool = new ObjectPool<EmojiPoolObject>(Constants.PLAYER_COUNT, Instantiator);
			
			_emojiViewRef.gameObject.SetActive(false);
			QuantumEvent.Subscribe<EventOnPlayerEmojiSent>(this, OnPlayerEmojiSent);
		}

		private async void OnPlayerEmojiSent(EventOnPlayerEmojiSent callback)
		{
			if (!_entityViewUpdaterService.TryGetView(callback.Entity, out var entityView))
			{
				// In case that the emoji is trying to be shown when a late joiner opens the game
				return;
			}
			
			var emoji = _pool.Spawn();
			var anchor = entityView.GetComponent<PlayerCharacterMonoComponent>().EmojiAnchor;
			var sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(callback.Emoji);

			emoji.EmojiView.SetInfo(sprite);
			emoji.OverlayWorldView.Follow(anchor);
		}
		
		private EmojiPoolObject Instantiator()
		{
			var instance = Instantiate(_emojiViewRef, transform, true);
			var instanceTransform = instance.transform;

			var poolObject = new EmojiPoolObject
			{
				EmojiView = instance,
				OverlayWorldView = instance.GetComponent<OverlayWorldView>()
			};
			
			instanceTransform.localScale = Vector3.one;
			instanceTransform.localPosition = Vector3.zero;
			
			instance.gameObject.SetActive(false);
			poolObject.EmojiView.Init(Despawn);
			
			return poolObject;

			void Despawn()
			{
				_pool?.Despawn(poolObject);
			}
		}
		
		private struct EmojiPoolObject : IPoolEntitySpawn, IPoolEntityDespawn
		{
			public EmojiView EmojiView;
			public OverlayWorldView OverlayWorldView;
			
			/// <inheritdoc />
			public void OnSpawn()
			{
				EmojiView.gameObject.SetActive(true);
			}
			
			/// <inheritdoc />
			public void OnDespawn()
			{
				EmojiView.gameObject.SetActive(false);
			}
		}
	}
}