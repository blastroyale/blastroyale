using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
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

		private EntityRef _entityRef => _view.EntityRef;
		private AnimatorWrapper _animatorWrapper => GetComponentInChildren<PlayerCharacterViewMonoComponent>().AnimatorWrapper;
		private MatchCharacterViewMonoComponent _matchCharacterView => GetComponentInChildren<MatchCharacterViewMonoComponent>();


		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
			_view = GetComponent<EntityView>();
			_view.OnEntityInstantiated.AddListener(OnEntityInstantiated);
			_vfxInitialRotation = _indicatorsRoot.transform.rotation;
			QuantumEvent.Subscribe<EventOnPlayerRevived>(this, OnPlayerRevived);
			QuantumEvent.Subscribe<EventOnPlayerRevived>(this, OnPlayerRevived);
			QuantumEvent.Subscribe<EventOnPlayerKnockedOut>(this, OnPlayerKnockedOut);
			QuantumEvent.Subscribe<EventOnPlayerStartReviving>(this, OnPlayerStartReviving);
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
			_animatorWrapper.SetTrigger(AvatarViewBase.Triggers.KnockedOut);
		}

		private void StartRevivingPlayer(Frame f, EntityRef entityRef)
		{
			if (!f.Unsafe.TryGetPointer<KnockedOut>(entityRef, out var knockedOut) || !f.Unsafe.TryGetPointer<PhysicsCollider3D>(knockedOut->ColliderEntity, out var knockedOutCollider))
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
			_animatorWrapper.SetTrigger(AvatarViewBase.Triggers.Revived);
			_services.VfxService.Spawn(VfxId.Revived).transform.position = transform.position + Vector3.up;
			_view.GetComponentInChildren<MatchCharacterViewMonoComponent>()?.ShowAllEquipment();
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
		}

		private void OnPlayerStartReviving(EventOnPlayerStartReviving callback)
		{
			if (callback.Entity != _entityRef) return;
			// Only team mates can see revive circle
			if (!_services.TeamService.IsSameTeamAsSpectator(callback.Entity))
			{
				return;
			}

			var f = callback.Game.Frames.Verified;
			StartRevivingPlayer(f, callback.Entity);
		}

		private void OnPlayerRevived(EventOnPlayerRevived callback)
		{
			if (callback.Entity != _entityRef) return;
			RevivePlayer();
		}
	}
}