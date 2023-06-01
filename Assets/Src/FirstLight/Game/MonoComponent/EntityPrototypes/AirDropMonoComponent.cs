using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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

		[SerializeField, Required, Title("Refs")]
		private Transform _itemRoot;

		[SerializeField, Required] private Transform _airplane;

		[SerializeField, Required, Title("Animation")]
		private ParticleSystem _landingPS;

		[SerializeField, Required] private Animation _landingAnim;
		[SerializeField] private int _airplaneTravelDistance = 150;
		[SerializeField] private float _airplaneTravelDuration = 10f;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropLanded>(this, OnAirDropLanded);

			var airDrop = GetComponentData<AirDrop>(game);
			var airDropHeight = Services.ConfigsProvider.GetConfig<QuantumGameConfig>().AirdropHeight.AsFloat;

			_itemRoot.gameObject.SetActive(false);
			
			Services.AssetResolverService.RequestAsset<GameId, GameObject>(airDrop.Chest, true, true,
			                                                               OnChestLoaded);
			
			if (airDrop.Stage  == AirDropStage.Dropped)
			{
				return;
			}
			
			var airdropPosition = airDrop.Position.ToUnityVector3();
			var airplaneDirection = airDrop.Direction.XOY.ToUnityVector3();

			var startingPosition = airdropPosition - airplaneDirection * _airplaneTravelDistance +
			                       Vector3.up * airDropHeight;
			var targetPosition = airdropPosition + airplaneDirection * _airplaneTravelDistance +
			                     Vector3.up * airDropHeight;

			_airplane.rotation = Quaternion.LookRotation(airplaneDirection);
			_airplane.position = startingPosition;
			_airplane.DOMove(targetPosition, _airplaneTravelDuration)
			         .SetDelay(Mathf.Max(0, airDrop.Delay.AsFloat - _airplaneTravelDuration / 2f))
			         .OnStart(() => { _airplane.gameObject.SetActive(true); })
			         .OnComplete(() => { _airplane.gameObject.SetActive(false); });
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			_itemRoot.gameObject.SetActive(true);
			
			var collectable = _itemRoot.GetComponentInChildren<CollectableViewMonoComponent>();
			collectable.SetPickupCircleVisibility(false);
		}

		private void OnAirDropLanded(EventOnAirDropLanded callback)
		{
			if (callback.Entity != EntityView.EntityRef)
			{
				return;
			}
			
			_landingPS.Play();
			_landingAnim.Play();
			
			var collectable = _itemRoot.GetComponentInChildren<CollectableViewMonoComponent>();
			collectable.SetPickupCircleVisibility(true);
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
			
			var airDrop = GetComponentData<AirDrop>(QuantumRunner.Default.Game);
			
			if (airDrop.Stage  != AirDropStage.Waiting)
			{
				_itemRoot.gameObject.SetActive(true);
				
				if (airDrop.Stage == AirDropStage.Dropped)
				{
					_landingPS.Play();
					_landingAnim.Play();
				}
			}
		}
	}
}