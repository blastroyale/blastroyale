using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class SquadMembersView : IUIView
	{
		private VisualElement _root;

		private IMatchServices _matchServices;

		private readonly Dictionary<EntityRef, SquadMemberElement> _squadMembers = new();

		public void Attached(VisualElement root)
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_root = root;

			_root.Clear(); // Clears development-time child elements.
		}

		public void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerGearChanged>(this, OnPlayerGearChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerWeaponChanged>(this, OnPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
		}

		public void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			RecheckSquadMembers(callback.Game.Frames.Verified);
		}

		private void OnPlayerWeaponChanged(EventOnPlayerWeaponChanged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateEquipment(callback.Weapon);
		}

		private void OnPlayerGearChanged(EventOnPlayerGearChanged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateEquipment(callback.Gear);
		}
		
		private void OnEntityDamaged(EventOnEntityDamaged callback)
		{

			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;
			
			// TODO;
		}

		private void RecheckSquadMembers(Frame f)
		{
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;

			_squadMembers
				.Clear(); // Not ideal but easy to implement and I don't have time to figure out how to remove missing members and also that would probably require another list or something that would need an allocation but this is fiiiine it's not like we're making a game that's gonna run on an Arduino. Or... are we?

			var index = 0;
			foreach (var (e, pc) in f.GetComponentIterator<PlayerCharacter>())
			{
				if (pc.TeamId == spectatedPlayer.Team && spectatedPlayer.Entity != e)
				{
					SquadMemberElement squadMember;
					if (_root.childCount <= index)
					{
						_root.Add(squadMember = new SquadMemberElement());
					}
					else
					{
						squadMember = (SquadMemberElement) _root[index];
					}

					_squadMembers.Add(e, squadMember);

					squadMember.SetPlayer(e);
					index++;
				}
			}

			while (_root.childCount > index)
			{
				_root.RemoveAt(index);
			}
		}
	}
}