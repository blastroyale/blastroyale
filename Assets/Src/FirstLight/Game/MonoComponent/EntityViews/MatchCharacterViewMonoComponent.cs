using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc />
	public class MatchCharacterViewMonoComponent : CharacterEquipmentMonoComponent
	{
		private FootprinterMonoComponent _footsteps;

		public bool PrintFootsteps
		{
			set
			{
				if (_footsteps == null) return;
				_footsteps.SpawnFootprints = value;
			}
		}


		/// <summary>
		/// Initializes the Adventure character view with the given player data
		/// </summary>
		public async UniTask Init(EntityView entityView, PlayerLoadout loadout, Frame frame)
		{
			Cosmetics = loadout.Cosmetics;
			_footsteps = gameObject.AddComponent<FootprinterMonoComponent>();
			_footsteps.Init(entityView, loadout);

			// TODO: Very ugly hack
			var meleeWeapon = await InstantiateMelee();

			var isSkydiving = frame.Get<AIBlackboardComponent>(entityView.EntityRef).GetBoolean(frame, Constants.IsSkydiving);

			if (isSkydiving)
			{
				var glider = _services.CollectionService.GetCosmeticForGroup(Cosmetics, GameIdGroup.Glider);
				await InstantiateGlider(glider);
			}


			var runner = QuantumRunner.Default;

			if (this.IsDestroyed() || runner == null)
			{
				return;
			}
			
			// TODO: Ugly
			var components = meleeWeapon.GetComponents<EntityViewBase>();

			foreach (var entityViewBase in components)
			{
				entityViewBase.SetEntityView(runner.Game, entityView);
			}

			if (isSkydiving)
			{
				HideAllEquipment();
			}
		}
	}
}