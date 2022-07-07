using System.Numerics;
using Circuit;
using DG.Tweening;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the AirDrop.
	/// </summary>
	public class AirDropMonoComponent : EntityBase
	{
		[SerializeField, Required] private GameObject _parachute;
		[SerializeField, Required] private GameObject _airplaneShadow;
		[SerializeField, Required] private ParticleSystem _landingPS;
		[SerializeField, Required] private Animation _landingAnim;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			QuantumEvent.Subscribe<EventOnAirDropStarted>(this, OnAirDropStarted);
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
		}

		private void OnAirDropStarted(EventOnAirDropStarted callback)
		{
			if (callback.Entity == EntityView.EntityRef)
			{
				Services.AssetResolverService.RequestAsset<GameId, GameObject>(callback.AirDrop.Chest, true, true,
				                                                               OnLoaded);
				_parachute.SetActive(true);
				
				var airDrop = callback.AirDrop;

				var airdropPosition = airDrop.Position.ToUnityVector3();
				var airplaneDirection = airDrop.Direction.XOY.ToUnityVector3();

				var distance = 150f;

				Vector3 startingPosition = airdropPosition - airplaneDirection * distance + Vector3.up * 10f;
				Vector3 targetPosition = airdropPosition + airplaneDirection * distance + Vector3.up * 10f;


				_airplaneShadow.transform.position = startingPosition;
				_airplaneShadow.transform.DOMove(targetPosition, 5f);
				
			}
		}
		

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			if (callback.Entity == EntityView.EntityRef)
			{
				_landingPS.Play();
				_landingAnim.Play();
			}
		}
	}
}