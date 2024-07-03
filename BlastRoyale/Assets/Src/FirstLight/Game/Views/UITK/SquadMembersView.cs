using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Photon.Deterministic;
using Quantum;
using Quantum.Systems;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles displaying the squad members on the screen, if there are any.
	/// </summary>
	public class SquadMembersView : UIView
	{
		private const int MAX_SQUAD_MEMBERS = 3;

		private IMatchServices _matchServices;
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private ICollectionService _collectionService;
		
		private readonly Dictionary<EntityRef, SquadMemberElement> _squadMembers = new ();

		protected override void Attached()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.ResolveServices();
			_dataProvider = MainInstaller.ResolveData();
			_collectionService = _services.CollectionService;
			Element.Clear(); // Clears development-time child elements.
		}

		public override void OnScreenOpen(bool reload)
		{
			QuantumEvent.SubscribeManual<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnPlayerDead>(this, OnPlayerDead);
			QuantumEvent.SubscribeManual<EventOnHealthChangedPredicted>(this, OnHealthChanged);
			QuantumEvent.SubscribeManual<EventOnShieldChangedPredicted>(this, OnShieldChanged);
			QuantumEvent.SubscribeManual<EventOnEntityDamaged>(this, OnEntityDamaged);
			QuantumEvent.SubscribeManual<EventOnTeamAssigned>(this, OnTeamAssigned);
			QuantumEvent.SubscribeManual<EventOnPlayerKnockedOut>(this, OnPlayerKnockedOut);
			QuantumEvent.SubscribeManual<EventOnPlayerRevived>(this, OnPlayerRevived);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
		}

		public override void OnScreenClose()
		{
			QuantumEvent.UnsubscribeListener(this);
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage obj)
		{
			if (!obj.IsResync)
			{
				return;
			}

			RecheckSquadMembers(obj.Game.Frames.Predicted);
		}


		private void OnTeamAssigned(EventOnTeamAssigned e)
		{
			if (!_squadMembers.TryGetValue(e.Entity, out var squadMemberElement)) return;
			squadMemberElement.SetTeamColor(_services.TeamService.GetTeamMemberColor(e.Entity));
		}


		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			RecheckSquadMembers(callback.Game.Frames.Verified);
		}

		private void OnPlayerKnockedOut(EventOnPlayerKnockedOut e)
		{
			if (!_squadMembers.TryGetValue(e.Entity, out var squadMemberElement)) return;
			squadMemberElement.SetKnocked(true);
		}

		private void OnPlayerRevived(EventOnPlayerRevived e)
		{
			if (!_squadMembers.TryGetValue(e.Entity, out var squadMemberElement)) return;
			squadMemberElement.SetKnocked(false);
		}


		private void OnPlayerDead(EventOnPlayerDead callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.SetDead();
			_squadMembers.Remove(callback.Entity);
		}


		private void OnHealthChanged(EventOnHealthChangedPredicted callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateHealth(callback.PreviousValue, callback.CurrentValue, callback.CurrentMax);
		}

		private void OnShieldChanged(EventOnShieldChangedPredicted callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.UpdateShield(callback.PreviousValue, callback.CurrentValue, callback.CurrentMax);
		}

		private void OnEntityDamaged(EventOnEntityDamaged callback)
		{
			if (!_squadMembers.TryGetValue(callback.Entity, out var squadMember)) return;

			squadMember.PingDamage();
		}

		private unsafe void RecheckSquadMembers(Frame f)
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

					var teamColor = _services.TeamService.GetTeamMemberColor(e);
					if (teamColor.HasValue)
					{
						squadMember.SetTeamColor(teamColor.Value);
					}

					var isBot = f.Has<BotCharacter>(e);
					var playerName = Extensions.GetPlayerName(f, e, pc);
					var playerNameColor = isBot
						? GameConstants.PlayerName.DEFAULT_COLOR
						: _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked,
							(int) f.GetPlayerData(pc.Player).LeaderboardRank);

					var cosmetics = f.ResolveList(f.Get<CosmeticsHolder>(e).Cosmetics);
					 _collectionService.LoadCollectionItemSprite(_collectionService.GetCosmeticForGroup(cosmetics, GameIdGroup.PlayerSkin))
									   .ContinueWith(loadedAvatar => squadMember.SetPlayer(pc.Player, playerName, loadedAvatar, playerNameColor));
					
					 squadMember.SetPlayer(pc.Player, playerName, null, playerNameColor);
					
					if (f.TryGet<Stats>(e, out var stats))
					{
						var maxHealth = FPMath.RoundToInt(stats.GetStatData(StatType.Health).StatValue);
						var maxShield = FPMath.RoundToInt(stats.GetStatData(StatType.Shield).StatValue);

						squadMember.UpdateHealth(stats.CurrentHealth, stats.CurrentHealth, maxHealth);
						squadMember.UpdateShield(stats.CurrentShield, stats.CurrentShield, maxShield);
					}
					squadMember.SetKnocked(ReviveSystem.IsKnockedOut(f,e));

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