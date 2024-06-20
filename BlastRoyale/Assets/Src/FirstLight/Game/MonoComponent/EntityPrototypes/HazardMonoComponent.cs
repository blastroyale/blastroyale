using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the correct Hazard model as a child of the Hazard entity view.
	/// </summary>
	public class HazardMonoComponent : EntityBase
	{
		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			if (HasRenderedView()) return;
			
			var hazard = GetComponentData<Hazard>(game);
			var radius =  hazard.Radius.AsFloat * GameConstants.Visuals.RADIUS_TO_SCALE_CONVERSION_VALUE_NON_PLAIN_INDICATORS;
			var view = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(hazard.GameId, 
				           true, true, OnLoaded);

			if (!this.IsDestroyed())
			{
				view.gameObject.GetComponent<HazardViewMonoComponent>().SetRadius(radius);
			}
		}
	}
}