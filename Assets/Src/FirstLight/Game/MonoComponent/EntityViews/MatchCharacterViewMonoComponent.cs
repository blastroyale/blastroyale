using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Utils;
using Quantum;

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
		public async Task Init(EntityView entityView, PlayerLoadout loadout, bool isSkydiving)
		{
			_footsteps = gameObject.AddComponent<FootprinterMonoComponent>();
			_footsteps.Init(entityView, loadout);

			var weaponTask = EquipWeapon(loadout.Weapon.GameId);
			var list = new List<Task> {weaponTask};

			foreach (var item in loadout.Equipment)
			{
				if (!item.IsValid())
				{
					continue;
				}

				list.Add(EquipItem(item.GameId));
			}

			if (isSkydiving)
			{
				list.Add(InstantiateItem(loadout.Glider, GameIdGroup.Glider));
			}

			await Task.WhenAll(list);

			var runner = QuantumRunner.Default;

			if (this.IsDestroyed() || runner == null)
			{
				return;
			}

			var weapons = weaponTask.Result;

			for (var i = 0; i < weapons.Count; i++)
			{
				if (weapons[i] == null) continue;
				
				var components = weapons[i].GetComponents<EntityViewBase>();

				foreach (var entityViewBase in components)
				{
					entityViewBase.SetEntityView(runner.Game, entityView);
				}
			}
			
		
		}
	}
}