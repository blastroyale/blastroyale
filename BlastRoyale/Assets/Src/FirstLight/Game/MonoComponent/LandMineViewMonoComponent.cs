using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	public class LandMineViewMonoComponent : MonoBehaviour
	{
		[Required] [SerializeField] private Transform _visualTransform;
		[Required] [SerializeField] private Transform _circle;

		private EntityView _view;
		private Animator _animator;
		private static readonly int _moveTrigger = Animator.StringToHash("Move");

		private void Awake()
		{
			_view = GetComponent<EntityView>();
			_animator = GetComponentInChildren<Animator>();
			QuantumEvent.Subscribe<EventLandMineTriggered>(this, OnMineTrigerred);
			QuantumEvent.Subscribe<EventLandMineExploded>(this, OnMineExploded);
		}

		private void Start()
		{
			_circle.gameObject.SetActive(false);
		}

		private void OnMineTrigerred(EventLandMineTriggered callback)
		{
			if (callback.Entity == _view.EntityRef)
			{
				var x = callback.Radius.AsFloat * 2;
				_circle.gameObject.SetActive(true);
				_circle.localScale = new Vector3(x, x, x);
				_animator.SetTrigger(_moveTrigger);
			}
		}

		private void OnMineExploded(EventLandMineExploded callback)
		{
			if (callback.Entity != _view.EntityRef) return;
			var exp = MainInstaller.ResolveMatchServices()
				.VfxService.Spawn(VfxId.Explosion);
			exp.transform.position = _visualTransform.position;
			_visualTransform.gameObject.SetActive(false);
		}
	}
}