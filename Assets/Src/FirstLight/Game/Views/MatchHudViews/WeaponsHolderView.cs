using System.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MatchHudViews;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles logic for Weapon slots UI
	/// </summary>
	public class WeaponsHolderView : MonoBehaviour
	{
		[SerializeField] private WeaponSlotView[] _slots;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponAdded>(this, OnEventOnLocalPlayerWeaponAdded);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnLocalPlayerWeaponChanged);

			foreach (var slot in _slots)
			{
				slot.Init();
			}
			_slots[Constants.WEAPON_INDEX_DEFAULT].SetEquipment(new Equipment(GameId.Hammer));
			_slots[Constants.WEAPON_INDEX_PRIMARY].SetEquipment(Equipment.None);
			_slots[Constants.WEAPON_INDEX_SECONDARY].SetEquipment(Equipment.None);

			UpdateVisibleSlots();
			
			SetSelectedSlot(Constants.WEAPON_INDEX_DEFAULT);
		}

		private void UpdateVisibleSlots()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var gameModeConfig = frame.Context.GameModeConfig;

			if (gameModeConfig.SingleSlotMode)
			{
				for (var slotIndex = 0; slotIndex < _slots.Length ; slotIndex++)
				{
					_slots[slotIndex].gameObject.SetActive(slotIndex is Constants.WEAPON_INDEX_DEFAULT or Constants.WEAPON_INDEX_PRIMARY);
				}
			}
		}

		private void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync || _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			var game = msg.Game;
			var f = game.Frames.Predicted;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			if (!localPlayer.Entity.IsAlive(f))
			{
				return;
			}

			var playerCharacter = f.Get<PlayerCharacter>(localPlayer.Entity);

			_slots[Constants.WEAPON_INDEX_DEFAULT]
				.SetEquipment(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_DEFAULT].Weapon);
			_slots[Constants.WEAPON_INDEX_PRIMARY]
				.SetEquipment(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_PRIMARY].Weapon);
			_slots[Constants.WEAPON_INDEX_SECONDARY]
				.SetEquipment(playerCharacter.WeaponSlots[Constants.WEAPON_INDEX_SECONDARY].Weapon);
			SetSelectedSlot(playerCharacter.CurrentWeaponSlot);
		}

		private void OnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetSelectedSlot(callback.Slot);
		}

		private void OnEventOnLocalPlayerWeaponAdded(EventOnLocalPlayerWeaponAdded callback)
		{
			_slots[callback.WeaponSlotNumber].SetEquipment(callback.Weapon);
		}

		private void SetSelectedSlot(int slotIndex)
		{
			for (var i = 0; i < _slots.Length; i++)
			{
				_slots[i].SetSelected(i == slotIndex);
			}
		}

		[Button, FoldoutGroup("Debug")]
		private void DebugSetRarity(EquipmentRarity rarity)
		{
			foreach (var ws in _slots)
			{
				ws.SetEquipment(new Equipment(GameId.Hammer, rarity: rarity));
			}
		}

		[InfoBox("Use -1 to deselect all of them.")]
		[Button, FoldoutGroup("Debug")]
		private void DebugSetSelected(int slot)
		{
			SetSelectedSlot(slot);
		}

		[Button, FoldoutGroup("Debug")]
		private void DebugReset()
		{
			foreach (var ws in _slots)
			{
				ws.SetEquipment(Equipment.None);
			}

			SetSelectedSlot(-1);
		}
	}
}