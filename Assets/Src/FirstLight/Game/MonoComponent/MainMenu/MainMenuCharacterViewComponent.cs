using System.Collections.Generic;
using System.Threading.Tasks;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Configs;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <inheritdoc cref="CharacterEquipmentMonoComponent"/>
	public class MainMenuCharacterViewComponent : CharacterEquipmentMonoComponent, IDragHandler, IPointerClickHandler
	{
		[SerializeField] private MainMenuCharacterAnimationConfigs _mainMenuCharacterAnimations;
		
		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async Task Init(List<EquipmentInfo> items)
		{
			var list = new List<Task>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.Equipment.IsWeapon() ? EquipWeapon(item.Equipment.GameId) : EquipItem(item.Equipment.GameId));
			}

			await Task.WhenAll(list);
		}
		
		/// <summary>
		/// Equip this character with the equipment data given in the <paramref name="info"/>
		/// </summary>
		public async Task Init(List<Equipment> items)
		{
			var list = new List<Task>();
			
			foreach (var item in items)
			{
				// TODO mihak: Make this into a single Equip method call
				list.Add(item.IsWeapon() ? EquipWeapon(item.GameId) : EquipItem(item.GameId));
			}

			await Task.WhenAll(list);
		}
		
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.dragging)
			{
				return;
			}

			PlayAnimation();
		}

		public void PlayAnimation()
		{
			// Animator.SetTrigger(_triggerNamesClicked[Random.Range(0, _triggerNamesClicked.Length)]);
			var config = _mainMenuCharacterAnimations.Configs[0].AnimationNames;
			Animator.SetTrigger(config[Random.Range(0, config.Length)]);
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			transform.parent.Rotate(0, -eventData.delta.x, 0, Space.Self);
		}
	}
}