using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Projectile"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class ProjectileMonoComponent : EntityBase
	{
		protected override async void OnEntityInstantiated(QuantumGame game)
		{
			var projectile = game.Frames.Predicted.Get<Projectile>(EntityView.EntityRef);
			var configsProvider = MainInstaller.Resolve<IGameServices>().ConfigsProvider;
			var assetReference = configsProvider.GetConfig<ProjectileAssetConfigs>().ConfigsDictionary[projectile.SourceId];
			
			if (!assetReference.OperationHandle.IsValid())
			{
				assetReference.LoadAssetAsync<GameObject>();
			}
			
			if (!assetReference.IsDone)
			{
				await assetReference.OperationHandle.Task;
			}
				
			if (this.IsDestroyed())
			{
				return;
			}
			
			OnLoaded(projectile.SourceId, Instantiate(assetReference.Asset as GameObject), true);
		}
	}
}