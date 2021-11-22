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
	public class AdventureCharacterViewMonoComponent : CharacterEquipmentMonoComponent
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

			if (this.IsDestroyed())
			{
				return;
			}

			var weapons = weaponTask.Result;
			
			for (var i = 0; i < weapons.Count; i++)
			{
				weapons[i].GetComponent<WeaponViewMonoComponent>().SetEntityView(entityView);
			}
		}
	}
}