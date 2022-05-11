using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;
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
		public async Task Init(Equipment weapon, List<Equipment> gear)
		{
			var list = new List<Task>();
			
			if (weapon.IsValid)
			{
				list.Add(EquipWeapon(weapon.GameId));
			}

			foreach (var item in gear)
			{
				list.Add(EquipItem(item.GameId));
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