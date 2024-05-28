using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using Quantum.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	[RequireComponent(typeof(EntityView))]
	public unsafe class KnockedOutPlayerViewMonoComponent : MonoBehaviour
	{
		private const float ANIMATION_DURATION = 0.333f;

		[SerializeField, Required] private GameObject _indicatorsRoot;
		[SerializeField, Required] private Image _reviveProgressIndicator;
		[SerializeField, Required] private Image _rangeIndicator;

		private IGameServices _services;
		private EntityView _view;
		private TweenerCore<Vector3, Vector3, VectorOptions> _tweener;
		private bool _circleActive;
		private Quaternion _vfxInitialRotation;
		private Vfx<VfxId> _revivingFX;

		private EntityRef _entityRef => _view.EntityRef;
		private CharacterSkinMonoComponent _skin => GetComponentInChildren<CharacterSkinMonoComponent>(); // TODO: BAD!
		private MatchCharacterViewMonoComponent _matchCharacterView => GetComponentInChildren<MatchCharacterViewMonoComponent>();

		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
			_view = GetComponent<EntityView>();
			_view.OnEntityInstantiated.AddListener(OnEntityInstantiated);
			_vfxInitialRotation = _indicatorsRoot.transform.rotation;
			QuantumEvent.Subscribe<EventOnPlayerRevived>(this, OnPlayerRevived);
			QuantumEvent.Subscribe<EventOnPlayerKnockedOut>(this, OnPlayerKnockedOut);
			QuantumEvent.Subscribe<EventOnPlayerStartReviving>(this, OnPlayerStartReviving);
			QuantumEvent.Subscribe<EventOnPlayerStopReviving>(this, OnPlayerStopReviving);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			_indicatorsRoot.SetActive(false);
		}

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Predicted;
			if (!f.Unsafe.TryGetPointer<KnockedOut>(_entityRef, out var knockedOut))
			{
				return;
			}

			// Only team mates can see revive circle
			if (!_services.TeamService.IsSameTeamAsSpectator(_entityRef))
			{
				return;
			}

			var reviving = f.ResolveHashSet(knockedOut->PlayersReviving).Count > 0;
			if (!reviving && knockedOut->BackAtZero < f.Time)
			{
				ToggleOffCircle();
				return;
			}

			// Hack so the revive circle doesn't rotate with the player
			_indicatorsRoot.transform.rotation = _vfxInitialRotation;
			SetProgress(ReviveSystem.CalculateRevivePercentage(f, knockedOut).AsFloat);
		}


		private void SetProgress(float fp)
		{
			_reviveProgressIndicator.fillAmount = fp;
			_rangeIndicator.fillAmount = 1 - fp;
		}


		private void KnockoutPlayer()
		{
			_view.GetComponentInChildren<MatchCharacterViewMonoComponent>()?.HideAllEquipment();
			_skin.TriggerKnockOut();
			EnableRevivingFX(false);
		}

		private void StartRevivingPlayer(Frame f, EntityRef entityRef)
		{
			if (!f.Unsafe.TryGetPointer<KnockedOut>(entityRef, out var knockedOut) ||
				!f.Unsafe.TryGetPointer<PhysicsCollider3D>(knockedOut->ColliderEntity, out var knockedOutCollider))
			{
				return;
			}

			var range = (knockedOutCollider->Shape.Sphere.Radius.AsFloat * 0.9f) * 2;
			SetProgress(0);
			_circleActive = true;
			_indicatorsRoot.SetActive(true);
			IncreaseCircleSize(range);
		}

		private void RevivePlayer()
		{
			_indicatorsRoot.SetActive(false);
			_skin.TriggerRestore();
			_services.VfxService.Spawn(VfxId.Revived).transform.position = transform.position + Vector3.up;
			_view.GetComponentInChildren<MatchCharacterViewMonoComponent>()?.ShowAllEquipment();
			EnableRevivingFX(false);
		}


		private void ToggleOffCircle()
		{
			if (!_circleActive)
			{
				return;
			}

			_circleActive = false;
			_tweener?.Kill();
			_tweener = null;
			_tweener = _indicatorsRoot.transform.DOScale(new Vector3(0, 0, 0), ANIMATION_DURATION).SetAutoKill().SetEase(Ease.OutBack);
		}

		private void IncreaseCircleSize(float range)
		{
			_tweener?.Kill();
			_tweener = null;
			_indicatorsRoot.transform.localScale.Set(0, 0, 0);
			_tweener = _indicatorsRoot.transform.DOScale(new Vector3(range, 0, range), ANIMATION_DURATION).SetAutoKill().SetEase(Ease.OutBack);
		}

		private void OnEntityInstantiated(QuantumGame game)
		{
			var f = game.Frames.Verified;
			if (ReviveSystem.IsKnockedOut(f, _entityRef))
			{
				KnockoutPlayer();
			}
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut callback)
		{
			if (callback.Entity != _entityRef) return;
			KnockoutPlayer();
			
			// Only team mates can hear knocked down sound
			if (!_services.TeamService.IsSameTeamAsSpectator(callback.Entity))
			{
				return;
			}
			// This is a general notification about the event, that's why it's not in 3D space
			_services.AudioFxService.PlayClip2D(AudioId.TeammateKnockedDown, GameConstants.Audio.MIXER_GROUP_SFX_2D_ID);
		}

		private void OnPlayerStartReviving(EventOnPlayerStartReviving callback)
		{
			if (callback.Entity != _entityRef) return;

			EnableRevivingFX(true);

			// Only team mates can see revive circle
			if (!_services.TeamService.IsSameTeamAsSpectator(callback.Entity))
			{
				return;
			}

			var f = callback.Game.Frames.Verified;
			StartRevivingPlayer(f, callback.Entity);
		}

		private void OnPlayerStopReviving(EventOnPlayerStopReviving callback)
		{
			if (callback.Entity != _entityRef || _revivingFX == null) return;

			EnableRevivingFX(false);
		}

		private void EnableRevivingFX(bool enable)
		{
			if (enable)
			{
				if (_revivingFX != null) return;
				_revivingFX = _services.VfxService.Spawn(VfxId.Reviving);
				_revivingFX.transform.SetParent(transform, false);
			}
			else
			{
				if (_revivingFX == null) return;
				_services.VfxService.Despawn(_revivingFX);
				_revivingFX = null;
			}
		}

		private void OnPlayerRevived(EventOnPlayerRevived callback)
		{
			if (callback.Entity != _entityRef) return;
			RevivePlayer();
		}
	}
}