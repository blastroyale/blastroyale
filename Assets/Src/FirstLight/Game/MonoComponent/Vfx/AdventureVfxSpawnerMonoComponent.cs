using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc />
	/// <remarks>
	/// Spawner to use during Quantum's simulation
	/// </remarks>
	public class AdventureVfxSpawnerMonoComponent : VfxSpawnerBase<VfxId>
	{
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		/// <inheritdoc />
		public override void Spawn()
		{
			var spawned = _services.VfxService.Spawn(_vfx);

			spawned.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
		}
	}
}