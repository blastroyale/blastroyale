using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="PlayerCharacter"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class PlayerCharacterMonoComponent : HealthEntityBase
	{
		[SerializeField, Required] private Transform _emojiAnchor;
		[SerializeField] private GameObject _shadowBlob;

		private PlayerCharacterViewMonoComponent _playerView;

		/// <summary>
		/// The <see cref="Transform"/> anchor values to attach the avatar emoji
		/// </summary>
		public Transform EmojiAnchor => _emojiAnchor;

		/// <summary>
		/// The associated view of this monocomponent
		/// </summary>
		public PlayerCharacterViewMonoComponent PlayerView => _playerView;

		protected override void OnAwake()
		{
			_shadowBlob.SetActive(false);

			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLanded);
		}

		private void OnPlayerSkydiveLanded(EventOnPlayerSkydiveLand callback)
		{
			if (callback.Entity != EntityView.EntityRef)
				return;

			_playerView.GetComponent<MatchCharacterViewMonoComponent>().ShowAllEquipment();
			_shadowBlob.SetActive(true);
		}

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			if (HasRenderedView()) return;

			var frame = game.Frames.Verified;

			InstantiateAvatar(game, frame.Get<PlayerCharacter>(EntityView.EntityRef).Player);
		}

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			var f = game?.Frames?.Verified;

			if (f == null || _playerView == null) return;

			var playerData = f.GetSingleton<GameContainer>().PlayersData[_playerView.PlayerRef];
			var marker = playerData.PlayerDeathMarker;

			SpawnDeathMarker(marker);
		}

		private async void SpawnDeathMarker(GameId marker)
		{
			var position = transform.position;
			var obj = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(marker);

			obj.transform.position = position;
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}

			// Disabled VXF on player spawn
			//var position = GetComponentData<Transform3D>(callback.Game).Position.ToUnityVector3();
			//var aliveVfx = Services.VfxService.Spawn(VfxId.SpawnPlayer);

			//aliveVfx.transform.position = position;
		}

		private async void InstantiateAvatar(QuantumGame quantumGame, PlayerRef player)
		{
			var frame = quantumGame.Frames.Verified;
			var stats = frame.Get<Stats>(EntityView.EntityRef);
			var loadout = PlayerLoadout.GetLoadout(frame, EntityView.EntityRef);
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(loadout.Skin, true, true, OnLoaded);

			if (this.IsDestroyed())
			{
				return;
			}

			_playerView = instance.GetComponent<PlayerCharacterViewMonoComponent>();
			var matchCharacterViewMonoComponent = instance.GetComponent<MatchCharacterViewMonoComponent>();
			await matchCharacterViewMonoComponent.Init(EntityView, loadout, frame);

			if (this.IsDestroyed())
			{
				return;
			}

			if (frame.Has<BotCharacter>(EntityView.EntityRef))
			{
				var bot = _playerView.gameObject.AddComponent<BotCharacterViewMonoComponent>();
				bot.SetEntityView(quantumGame, _playerView.EntityView);
			}

			if (stats.CurrentStatusModifierType != StatusModifierType.None)
			{
				var time = stats.CurrentStatusModifierEndTime - frame.Time;

				_playerView.SetStatusModifierEffect(stats.CurrentStatusModifierType, time.AsFloat);
			}
		}

		protected override string GetName(QuantumGame game)
		{
			var pc = GetComponentData<PlayerCharacter>(game);
			return (pc.RealPlayer ? "[Player]" : "[Bot]")
				+ " - " + Extensions.GetPlayerName(GetFrame(game), EntityView.EntityRef, pc)
				+ " - " + EntityView.EntityRef;
		}

		protected override string GetGroup(QuantumGame game)
		{
			return "Players";
		}
	}
}