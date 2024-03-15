using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using Quantum;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	public class DeathFlagMonoComponent : EntityBase
	{
		
		protected override void OnEntityInstantiated(QuantumGame game)
		{
			SpawnDeathMarker(game).Forget();
		}
		
		
		private async UniTaskVoid SpawnDeathMarker(QuantumGame game)
		{
			var deathFlagId = GetComponentData<DeathFlag>(game).ID;

			var services = MainInstaller.ResolveServices();
			var marker = services.CollectionService.GetCosmeticForGroup(new []{deathFlagId}, GameIdGroup.DeathMarker);
			
			var obj = await services.CollectionService.LoadCollectionItem3DModel(marker);
			
			OnLoaded(deathFlagId, obj, true);
		}
	}
}