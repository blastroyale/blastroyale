using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <inheritdoc cref="CharacterEquipmentMonoComponent"/>
	public class MainMenuCharacterViewComponent : CharacterEquipmentMonoComponent, IDragHandler, IPointerClickHandler
	{
		[SerializeField] private string[] _triggerNamesClicked;
		
		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async Task Init(EquipmentLoadOutInfo info)
		{
			var list = new List<Task>();
			
			if (info.Weapon.HasValue)
			{
				list.Add(EquipWeapon(info.Weapon.Value.GameId));
			}

			foreach (var gear in info.Gear)
			{
				list.Add(EquipItem(gear.GameId));
			}

			await Task.WhenAll(list);
		}
		
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.dragging)
			{
				return;
			}
			
			Animator.SetTrigger(_triggerNamesClicked[Random.Range(0, _triggerNamesClicked.Length)]);
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			transform.parent.Rotate(0, -eventData.delta.x, 0, Space.Self);
		}
	}
}