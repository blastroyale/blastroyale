using FirstLight.Game.Domains.Flags.View;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	public class DeathFlagMonoComponent : EntityBase
	{
		private IMatchServices _matchServices;
		private DeathFlagView _view;

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			if (_view != null)
			{
				_matchServices.FlagService.Despawn(_view);
			}

			_view = null;
		}

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_matchServices ??= MainInstaller.Resolve<IMatchServices>();
			var deathFlagId = GetComponentData<DeathFlag>(game).ID;
			var flag = _matchServices.FlagService.Spawn(deathFlagId);
			OnLoaded(deathFlagId, flag.gameObject, true);
			flag.transform.localEulerAngles = new Vector3(0, 180, 0);
			flag.TriggerFlag();
		}
	}
}