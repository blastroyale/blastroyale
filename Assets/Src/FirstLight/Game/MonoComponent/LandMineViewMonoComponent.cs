using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
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
		[Required] [SerializeField] private Transform _explosion;
		
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
				var x = callback.Radius.AsFloat*2;
				_circle.gameObject.SetActive(true);
				_circle.localScale = new Vector3(x, x, x);
				_animator.SetTrigger(_moveTrigger);
			}
		}

		private async UniTaskVoid DeleteVfx()
		{
			await UniTask.WaitForSeconds(4);
			if (_explosion != null)
			{
				Destroy(_explosion.gameObject);
			}
		}

		private void OnMineExploded(EventLandMineExploded callback)
		{
			if (callback.Entity == _view.EntityRef)
			{
				_explosion.transform.SetParent(null, true);
				_explosion.gameObject.SetActive(true);
				_visualTransform.gameObject.SetActive(false);
				DeleteVfx().Forget();
			}
		}
	}
}