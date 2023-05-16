using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class EquipmentDisplayView : IUIView
	{
		private const string USS_GEAR_ACQUIRED = "equipment-display__gear-item--acquired";

		private IMatchServices _matchServices;

		private VisualElement _armor;
		private VisualElement _amulet;
		private VisualElement _helmet;
		private VisualElement _shield;
		private Label _count;

		private int _currentGear;

		public void Attached(VisualElement element)
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_armor = element.Q<VisualElement>("Armor");
			_amulet = element.Q<VisualElement>("Amulet");
			_helmet = element.Q<VisualElement>("Helmet");
			_shield = element.Q<VisualElement>("Shield");
			_count = element.Q<Label>("Count");
		}

		public void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerGearChanged>(this, OnPlayerGearChanged);
		}

		public void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerGearChanged(EventOnPlayerGearChanged callback)
		{
			if (callback.Entity != _matchServices.SpectateService.SpectatedPlayer.Value.Entity) return;

			switch (callback.Gear.GetEquipmentGroup())
			{
				case GameIdGroup.Helmet when !_helmet.ClassListContains(USS_GEAR_ACQUIRED):
					_helmet.AddToClassList(USS_GEAR_ACQUIRED);
					_helmet.AnimatePing();
					_currentGear++;
					break;
				case GameIdGroup.Amulet when !_amulet.ClassListContains(USS_GEAR_ACQUIRED):
					_amulet.AddToClassList(USS_GEAR_ACQUIRED);
					_amulet.AnimatePing();
					_currentGear++;
					break;
				case GameIdGroup.Armor when !_armor.ClassListContains(USS_GEAR_ACQUIRED):
					_armor.AddToClassList(USS_GEAR_ACQUIRED);
					_armor.AnimatePing();
					_currentGear++;
					break;
				case GameIdGroup.Shield when !_shield.ClassListContains(USS_GEAR_ACQUIRED):
					_shield.AddToClassList(USS_GEAR_ACQUIRED);
					_shield.AnimatePing();
					_currentGear++;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			_count.text = $"{_currentGear}/4";
		}
	}
}