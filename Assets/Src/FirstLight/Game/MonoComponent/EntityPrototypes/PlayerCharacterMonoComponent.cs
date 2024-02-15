
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Quantum.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using Extensions = FirstLight.Game.Utils.Extensions;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="PlayerCharacter"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class PlayerCharacterMonoComponent : HealthEntityBase
	{
		[SerializeField, Required] private Transform _emojiAnchor;
		[SerializeField] private GameObject _shadowBlob;
		[SerializeField] private SpriteRenderer _circleIndicator;
		private PlayerCharacterViewMonoComponent _playerView;
		private IGameServices _services;
		private IMatchServices _matchServices;

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
			_services = MainInstaller.ResolveServices();
			_matchServices = MainInstaller.ResolveMatchServices();
			_shadowBlob.SetActive(false);
			_circleIndicator.gameObject.SetActive(false);
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectateChange);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLanded);
			QuantumEvent.Subscribe<EventOnTeamAssigned>(this, OnTeamAssigned);
		}

		public bool IsBot => QuantumRunner.Default.PredictedFrame().Has<BotCharacter>(EntityView.EntityRef);

		public void SwitchShadowVisibility(bool visibility)
		{
			_shadowBlob.SetActive(visibility);
		}

		private void OnSpectateChange(SpectatedPlayer oldP, SpectatedPlayer newP)
		{
			if (oldP.Team == newP.Team) return;
			if (_circleIndicator.IsDestroyed() || this.IsDestroyed()) return;
			_circleIndicator.gameObject.SetActive(ShouldDisplayColorTag());
		}

		private void OnTeamAssigned(EventOnTeamAssigned e)
		{
			if (PlayerView == null || e.Entity != PlayerView.EntityRef) return;
			var color = _services.TeamService.GetTeamMemberColor(e.Entity);
			if (!color.HasValue) return;
			_circleIndicator.color = color.Value;
			_circleIndicator.gameObject.SetActive(ShouldDisplayColorTag());
		}

		private void OnPlayerSkydiveLanded(EventOnPlayerSkydiveLand callback)
		{
			if (callback.Entity != EntityView.EntityRef) return;
			if (_playerView != null)
			{
				_playerView.GetComponent<MatchCharacterViewMonoComponent>()?.ShowAllEquipment();
			}
			_shadowBlob.SetActive(true);
			_circleIndicator.gameObject.SetActive(ShouldDisplayColorTag());
		}

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			if (HasRenderedView()) return;
			var frame = game.Frames.Verified;
			_ = InstantiateAvatar(game, frame.Get<PlayerCharacter>(EntityView.EntityRef).Player);
		}

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			var f = game?.Frames?.Verified;

			if (f == null || _playerView == null) return;

			var cosmeticIds = PlayerLoadout.GetCosmetics(f, _playerView.PlayerRef);
			var marker = _services.CollectionService.GetCosmeticForGroup(cosmeticIds, GameIdGroup.DeathMarker);
			
			_ = SpawnDeathMarker(marker);
		}

		private async UniTaskVoid SpawnDeathMarker(ItemData marker)
		{ 
			var position = transform.position;
			var obj = await Services.CollectionService.LoadCollectionItem3DModel(marker);
			if (!QuantumRunner.Default.IsDefinedAndRunning())
			{
				Destroy(obj);
				return;
			}

			obj.transform.position = position;
		}


		public bool ShouldDisplayColorTag()
		{
			if (PlayerView == null || this.IsDestroyed() || PlayerView.IsEntityDestroyed())
			{
				return false;
			}
			if (TeamSystem.GetTeamMembers(QuantumRunner.Default.PredictedFrame(), PlayerView.EntityRef).Count < 1)
			{
				return false;
			}
			return !PlayerView.IsSkydiving && _services.TeamService.IsSameTeamAsSpectator(EntityView.EntityRef);
		}

		private async UniTask<GameObject> LoadCharacterSkin(GameId[] playerSkins)
		{
			var skin = Services.CollectionService.GetCosmeticForGroup(playerSkins, GameIdGroup.PlayerSkin);
			var obj = await Services.CollectionService.LoadCollectionItem3DModel(skin);

			// Add renderer containers
			var container = obj.AddComponent<RenderersContainerMonoComponent>();
			container.UpdateRenderers();
			// TODO REMOVE THIS SHIT SOMEDAY
			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH)
			{
				AddLegacyCollider(obj);
			}
			obj.AddComponent<RenderersContainerProxyMonoComponent>();
			obj.AddComponent<MatchCharacterViewMonoComponent>();
			obj.AddComponent<PlayerCharacterViewMonoComponent>();
			OnLoaded(skin.Id, obj, true);
			return obj;
		}
		
		private void AddLegacyCollider(GameObject obj)
		{
			// Legacy collider for old visibility volumes
			var newCollider = obj.AddComponent<CapsuleCollider>();
			newCollider.center = new Vector3(0, 0.75f, 0);
			newCollider.radius = 0.2f;
			newCollider.height = 0.75f;
			newCollider.direction = 1; // Y axis
			newCollider.isTrigger = true;
		}
		
		private async UniTaskVoid InstantiateAvatar(QuantumGame quantumGame, PlayerRef player)
		{
			var frame = quantumGame.Frames.Verified;
			var stats = frame.Get<Stats>(EntityView.EntityRef);
			var loadout = PlayerLoadout.GetLoadout(frame, EntityView.EntityRef);
			var skinInstance = await LoadCharacterSkin(loadout.Cosmetics);

			if (this.IsDestroyed())
			{
				return;
			}

			_playerView = skinInstance.GetComponent<PlayerCharacterViewMonoComponent>();
			var matchCharacterViewMonoComponent = skinInstance.GetComponent<MatchCharacterViewMonoComponent>();
			await matchCharacterViewMonoComponent.Init(EntityView, loadout, frame);

			if (this.IsDestroyed())
			{
				return;
			}

			if (IsBot)
			{
				var bot = _playerView.gameObject.AddComponent<BotCharacterViewMonoComponent>();
				bot.SetEntityView(quantumGame, _playerView.EntityView);
			}

			if (stats.CurrentStatusModifierType != StatusModifierType.None)
			{
				var time = stats.CurrentStatusModifierEndTime - frame.Time;

				_playerView.SetStatusModifierEffect(stats.CurrentStatusModifierType, time.AsFloat);
			}

			var colorTag = _services.TeamService.GetTeamMemberColor(EntityView.EntityRef);
			if (colorTag.HasValue) _circleIndicator.color = colorTag.Value;
			_circleIndicator.gameObject.SetActive(ShouldDisplayColorTag());

			_services.MessageBrokerService.Publish(new PlayerCharacterInstantiated()
			{
				Character = this
			});
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