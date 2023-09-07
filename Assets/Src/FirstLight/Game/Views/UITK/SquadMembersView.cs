using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles displaying the squad members on the screen, if there are any.
	/// </summary>
	public class SquadMembersView : UIView
	{
		private const int MAX_SQUAD_MEMBERS = 2;

		private IMatchServices _matchServices;
		private IGameServices _services;

		private readonly Dictionary<EntityRef, SquadMemberElement> _squadMembers = new();

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.ResolveServices();
			element.Clear(); // Clears development-time child elements.
		}

		public override void SubscribeToEvents()
		{
			QuantumEvent.SubscribeManual<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnPlayerGearChanged>(this, OnPlayerGearChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerWeaponChanged>(this, OnPlayerWeaponChanged);
			QuantumEvent.SubscribeManual<EventOnHealthChanged>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChanged>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnPlayerLevelUp>(this, OnPlayerLevelUp);
			QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
			QuantumEvent.SubscribeManual<EventOnTeamAssigned>(this, OnTeamAssigned);
		}

		private void OnTeamAssigned(EventOnTeamAssigned e)
		{
			if (!_squadMembers.TryGetValue(e.Entity, out var squadMemberElement)) return;
			squadMemberElement.SetTeamColor(_matchServices.TeamService.GetTeamMemberColor(e.Entity));
		}

		public override void UnsubscribeFromEvents()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			RecheckSquadMembers(callback.Game.Frames.Verified);
		}

		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.SetDead();
			_squadMembers.Remove(callback.Entity);
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

		private void OnHealthChanged(EventOnHealthChanged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateHealth((float) callback.CurrentHealth / callback.MaxHealth);
		}

		private void OnShieldChanged(EventOnShieldChanged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateShield((float) callback.CurrentShield / callback.CurrentShieldCapacity);
		}

		private void OnPlayerLevelUp(EventOnPlayerLevelUp callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateLevel(callback.CurrentLevel);
		}

		private void OnEntityDamaged(EventOnEntityDamaged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.PingDamage();
		}

		private void RecheckSquadMembers(Frame f)
		{
			var spectatedPlayer = _matchServices.SpectateService.SpectatedPlayer.Value;

			// Not ideal but easy to implement and I don't have time to figure out how to remove missing members and also that would probably require another list or something that would need an allocation but this is fiiiine it's not like we're making a game that's gonna run on an Arduino. Or... are we?
			_squadMembers.Clear();

			var index = 0;
			foreach (var (e, pc) in f.GetComponentIterator<PlayerCharacter>())
			{
				if (_squadMembers.Count >= MAX_SQUAD_MEMBERS) break;

				if (pc.TeamId == spectatedPlayer.Team && spectatedPlayer.Entity != e)
				{
					SquadMemberElement squadMember;
					if (Element.childCount <= index)
					{
						Element.Add(squadMember = new SquadMemberElement());
					}
					else
					{
						squadMember = (SquadMemberElement) Element[index];
					}

					_squadMembers.Add(e, squadMember);
					
					var teamColor = _matchServices.TeamService.GetTeamMemberColor(e);
					if (teamColor.HasValue)
					{
						squadMember.SetTeamColor(teamColor.Value);
					}

					var isBot = f.Has<BotCharacter>(e);
					var playerName = Extensions.GetPlayerName(f, e, pc);

					squadMember.SetPlayer(pc.Player, playerName, pc.GetEnergyLevel(f),
						isBot ? null : f.GetPlayerData(pc.Player).AvatarUrl);
					if (f.TryGet<Stats>(e, out var stats))
					{
						squadMember.UpdateHealth(StatUtils.GetHealthPercentage(stats).AsFloat);
						squadMember.UpdateShield(StatUtils.GetShieldPercentage(stats).AsFloat);
					}

					index++;
				}
			}

			while (Element.childCount > index)
			{
				Element.RemoveAt(index);
			}
		}
	}
}