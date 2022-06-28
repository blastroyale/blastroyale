using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the AirDrop.
	/// </summary>
	public class AirDropMonoComponent : EntityBase
	{
		[SerializeField, Required] private GameObject _parachute;

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
			}
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			_parachute.SetActive(false);
		}
	}
}