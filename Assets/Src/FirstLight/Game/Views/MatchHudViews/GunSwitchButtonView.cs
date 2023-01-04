using FirstLight.Game.Utils;
using Quantum;
using Quantum.Commands;
using Sirenix.OdinInspector;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays a button to switch between guns.
	/// </summary>
	public class GunSwitchButtonView : MonoBehaviour
	{
		[SerializeField, Required, TabGroup("Refs")]
		private Button _button;

		private void Awake()
		{
			_button.onClick.AddListener(OnGunSwitchClicked);
		}
		
		private void OnGunSwitchClicked()
		{
			var data = QuantumRunner.Default.Game.GetLocalPlayerData(false, out var f);

			// Check if there is a point in switching or not. Avoid extra commands to save network message traffic $$$
			if (!f.TryGet<PlayerCharacter>(data.Entity, out var pc))
			{
				return;
			}
			
			var slotIndexToSwitch = -1;

			switch (pc.CurrentWeaponSlot)
			{
				case 0:
					if (pc.WeaponSlots[1].Weapon.IsValid())
						slotIndexToSwitch = 1;
					else if (pc.WeaponSlots[2].Weapon.IsValid())
						slotIndexToSwitch = 2;
					break;

				case 1:
					if (pc.WeaponSlots[2].Weapon.IsValid())
						slotIndexToSwitch = 2;
					else
						slotIndexToSwitch = 0;
					break;

				case 2:
					if (pc.WeaponSlots[1].Weapon.IsValid())
						slotIndexToSwitch = 1;
					else
						slotIndexToSwitch = 0;
					break;
			}

			if (slotIndexToSwitch == -1)
			{
				return;
			}

			QuantumRunner.Default.Game.SendCommand(new WeaponSlotSwitchCommand { WeaponSlotIndex = slotIndexToSwitch });
		}
	}
}