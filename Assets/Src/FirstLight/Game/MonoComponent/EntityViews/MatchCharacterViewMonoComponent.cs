using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc />
	public class MatchCharacterViewMonoComponent : CharacterEquipmentMonoComponent
	{
		/// <summary>
		/// Initializes the Adventure character view with the given player data
		/// </summary>
		public async Task Init(Equipment weapon, Equipment[] gear, EntityView entityView)
		{
			var weaponTask = EquipWeapon(weapon.GameId);
			var list = new List<Task> { weaponTask };

			foreach (var item in gear)
			{
				if (!item.IsValid)
				{
					continue;
				}
				
				list.Add(EquipItem(item.GameId));
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
				var components = weapons[i].GetComponents<EntityViewBase>();

				foreach (var entityViewBase  in components)
				{
					entityViewBase.SetEntityView(runner.Game, entityView);	
				}
			}
		}
	}
}