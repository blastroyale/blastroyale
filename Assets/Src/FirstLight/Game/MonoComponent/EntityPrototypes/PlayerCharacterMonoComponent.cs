using System.Collections.Generic;
using FirstLight.Game.Ids;
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
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLanded);
		}

		private void OnPlayerSkydiveLanded(EventOnPlayerSkydiveLand callback)
		{
			if (callback.Entity != EntityView.EntityRef)
				return;

			_playerView.GetComponent<MatchCharacterViewMonoComponent>().ShowAllEquipment();
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
			
			if(f == null || _playerView == null) return;
			
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
			var playerData = frame.GetSingleton<GameContainer>().PlayersData[player];
			
			GetPlayerEquipmentSet(frame, player, out var skin, out var weapon, out var gear);

			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(skin, true, true, OnLoaded);
			
			if (this.IsDestroyed())
			{
				return;
			}
			
			_playerView = instance.GetComponent<PlayerCharacterViewMonoComponent>();

			var matchCharacterViewMonoComponent = instance.GetComponent<MatchCharacterViewMonoComponent>();
			await matchCharacterViewMonoComponent.Init(EntityView, weapon, gear, playerData.Glider);

			if (this.IsDestroyed())
			{
				return;
			}

			var isSkydiving = frame.Get<AIBlackboardComponent>(EntityView.EntityRef).GetBoolean(frame, Constants.IsSkydiving);
			if (isSkydiving)
			{
				matchCharacterViewMonoComponent.HideAllEquipment();
			}
			
			if (stats.CurrentStatusModifierType != StatusModifierType.None)
			{
				var time = stats.CurrentStatusModifierEndTime - frame.Time;

				_playerView.SetStatusModifierEffect(stats.CurrentStatusModifierType, time.AsFloat);
			}
		}

		private void GetPlayerEquipmentSet(Frame f, PlayerRef player, out GameId skin,
		                                   out Equipment weapon, out Equipment[] gear)
		{
			var playerCharacter = f.Get<PlayerCharacter>(EntityView.EntityRef);
			
			// Weapon
			weapon = playerCharacter.CurrentWeapon;
			
			// Gear
			var gearList = new List<Equipment>();

			for (int i = 0; i < playerCharacter.Gear.Length; i++)
			{
				var item = playerCharacter.Gear[i];
				if (item.IsValid())
				{
					gearList.Add(item);
				}
			}

			gear = gearList.ToArray();

			// Skin
			skin = f.TryGet<BotCharacter>(EntityView.EntityRef, out var botCharacter)
				       ? botCharacter.Skin
				       : f.GetPlayerData(player).Skin;
		}
	}
}