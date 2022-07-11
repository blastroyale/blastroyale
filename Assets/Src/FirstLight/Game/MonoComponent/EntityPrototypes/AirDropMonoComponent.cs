using DG.Tweening;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the AirDrop.
	/// </summary>
	public class AirDropMonoComponent : EntityBase
	{
		private const int AIRPLANE_DISTANCE = 150;
		
		[SerializeField, Required] private Transform _itemRoot;
		[SerializeField, Required] private GameObject _parachute;
		[SerializeField, Required] private Transform _airplane;
		[SerializeField, Required] private ParticleSystem _landingPS;
		[SerializeField, Required] private Animation _landingAnim;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			QuantumEvent.Subscribe<EventOnAirDropStarted>(this, OnAirDropStarted);
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropLanded>(this, OnAirDropLanded);
		}

		private void OnAirDropStarted(EventOnAirDropStarted callback)
		{
			if (callback.Entity != EntityView.EntityRef) return;

			Services.AssetResolverService.RequestAsset<GameId, GameObject>(callback.AirDrop.Chest, true, true,
			                                                               OnChestLoaded);

			var airDrop = callback.AirDrop;

			var airdropPosition = airDrop.Position.ToUnityVector3();
			var airplaneDirection = airDrop.Direction.XOY.ToUnityVector3();

			var startingPosition = airdropPosition - airplaneDirection * AIRPLANE_DISTANCE + Vector3.up * 10f;
			var targetPosition = airdropPosition + airplaneDirection * AIRPLANE_DISTANCE + Vector3.up * 10f;

			_airplane.gameObject.SetActive(true);
			_airplane.rotation = Quaternion.LookRotation(airplaneDirection);
			_airplane.position = startingPosition;
			_airplane.DOMove(targetPosition, 10f);
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			if (callback.Entity != EntityView.EntityRef) return;

			_parachute.SetActive(true);
		}

		private void OnAirDropLanded(EventOnAirDropLanded callback)
		{
			if (callback.Entity != EntityView.EntityRef) return;

			_landingPS.Play();
			_landingAnim.Play();
		}

		private void OnChestLoaded(GameId id, GameObject instance, bool instantiated)
		{
			var runner = QuantumRunner.Default;

			if (this.IsDestroyed() || runner == null)
			{
				Destroy(instance);
				return;
			}

			if (instance.TryGetComponent<EntityMainViewBase>(out var mainViewBase))
			{
				mainViewBase.SetEntityView(runner.Game, EntityView);
			}

			var cacheTransform = instance.transform;
			cacheTransform.SetParent(_itemRoot);
			cacheTransform.localPosition = Vector3.zero;
			cacheTransform.localRotation = Quaternion.identity;
		}
	}
}