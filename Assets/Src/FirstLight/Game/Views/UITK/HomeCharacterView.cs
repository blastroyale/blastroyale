using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	[Serializable]
	public class MenuPartyMember
	{
		public GameObject SlotRoot;
		public BaseCharacterMonoComponent Character;
		public Transform NameAnchor;
		public float LabelScale = 1;

		[NonSerialized] public HomePartyNameView NameView;
		[NonSerialized] public string PlayerId;
		[NonSerialized] public GameId LoadedSkin;

		public bool IsUsed => PlayerId != null;
	}

	[Serializable]
	public class HomeCharacterView : UIView
	{
		[SerializeField] private MenuPartyMember[] _teamMatePositions;
		[SerializeField] private MenuPartyMember _localPlayer;
		[SerializeField] private VisualTreeAsset _playerNameTemplate;

		private IGameServices _services;
		private IPartyService _partyService;
		private readonly SemaphoreSlim _lock = new (1, 1);


		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();
			_partyService = _services.PartyService;
		}

		public override void OnScreenOpen(bool reload)
		{
			_partyService.Members.Observe(OnPartyUpdate);
			_partyService.HasParty.Observe(OnHasPartyChanged);
			_partyService.LobbyProperties.Observe(OnLobbyPropertiesChanged);
			_partyService.LocalReadyStatus.Observe(OnLocalStatusChanged);


			CleanAll();
			UpdateMembers().Forget();
		}


		public override void OnScreenClose()
		{
			_partyService.LocalReadyStatus.StopObserving(OnLocalStatusChanged);
			_partyService.HasParty.StopObserving(OnHasPartyChanged);
			_partyService.Members.StopObserving(OnPartyUpdate);
			_partyService.LobbyProperties.StopObserving(OnLobbyPropertiesChanged);
		}

		private void OnLobbyPropertiesChanged(string arg1, string arg2, string arg3, ObservableUpdateType arg4)
		{
			UpdateMembers().Forget();
		}

		private void OnLocalStatusChanged(bool arg1, bool arg2)
		{
			UpdateMembers().Forget();
		}

		private void OnHasPartyChanged(bool _, bool newValue)
		{
			if (!newValue)
			{
				_localPlayer.NameView?.Disable();
				CleanAll();
				return;
			}

			UpdateSlot(_localPlayer, _partyService.GetLocalMember()).Forget();
		}

		private void OnPartyUpdate(int _, PartyMember oldMenuPartyMember, PartyMember newMenuPartyMember, ObservableUpdateType updateType)
		{
			UpdateMembers().Forget();
		}

		public async UniTaskVoid UpdateMembers()
		{
			await _lock.WaitAsync();
			try
			{
				var partyMembers = _partyService.Members.ToList();
				if (!_partyService.HasParty.Value)
				{
					CleanAll();
				}

				// Remove old members
				foreach (var teamMatePosition in _teamMatePositions)
				{
					if (!teamMatePosition.IsUsed) continue;
					if (partyMembers.All(pm => pm.PlayfabID != teamMatePosition.PlayerId))
					{
						Clean(teamMatePosition);
					}
				}

				foreach (var member in partyMembers)
				{
					if (member.Local)
					{
						await UpdateSlot(_localPlayer, member);
						continue;
					}

					var currentSlot = _teamMatePositions.FirstOrDefault(slot => slot.PlayerId == member.PlayfabID);
					if (currentSlot == null)
					{
						var emptySlot = _teamMatePositions.FirstOrDefault(slot => !slot.IsUsed);
						if (emptySlot == null)
						{
							// No space available for this player
							continue;
						}

						currentSlot = emptySlot;
					}

					await UpdateSlot(currentSlot, member);
				}
			}
			finally
			{
				_lock.Release();
			}
		}


		public async UniTask UpdateSlot(MenuPartyMember slot, PartyMember member)
		{
			CreateLabelFor(slot);
			slot.PlayerId = member.PlayfabID;
			// Update ui
			slot.NameView.Enable(member.DisplayName, member.Leader, _partyService.IsReady(member));
			slot.NameView.UpdatePosition();
			// Local player is always on screen so we don't have to load their skin
			if (member.Local)
			{
				return;
			}

			slot.NameView.OnClicked = () => OpenPlayerOptions(slot, member);

			var playerSkin = GameId.MaleAssassin;
			if (Enum.TryParse<GameId>(member.CharacterSkin, out var value))
			{
				playerSkin = value;
			}

			var meleeSkin = GameId.Hammer;
			if (Enum.TryParse<GameId>(member.CharacterSkin, out var melee))
			{
				meleeSkin = melee;
			}

			// Already loaded same skin
			if (slot.Character.gameObject.activeSelf && slot.LoadedSkin == playerSkin) return;
			slot.SlotRoot.SetActive(true);
			slot.LoadedSkin = playerSkin;
			await slot.Character.UpdateSkin(ItemFactory.Collection(playerSkin));
			await slot.Character.UpdateMeleeSkin(ItemFactory.Collection(meleeSkin));
			slot.Character.CharacterViewComponent.Clicked += _ => OpenPlayerOptions(slot, member);
		}

		public void CreateLabelFor(MenuPartyMember slot)
		{
			if (slot.NameView != null) return;
			var playerNameElement = _playerNameTemplate.Instantiate();
			Element.Add(playerNameElement);
			slot.NameView = new HomePartyNameView(slot.NameAnchor.position, slot.LabelScale);
			playerNameElement.AttachExistingView(Presenter, slot.NameView);
		}

		private void CleanAll()
		{
			foreach (var teamMatePosition in _teamMatePositions)
			{
				Clean(teamMatePosition);
			}
		}

		public void Clean(MenuPartyMember slot)
		{
			slot.PlayerId = null;
			slot.NameView?.Disable();
			slot.SlotRoot.SetActive(false);
		}

		private void OpenPlayerOptions(MenuPartyMember slot, PartyMember partyMember = null)
		{
			var member = partyMember ?? _partyService.Members.FirstOrDefault(m => m.PlayfabID == slot.PlayerId);
			if (member == null)
			{
				return;
			}

			var isLocalPlayerLeader = _partyService.GetLocalMember()?.Leader ?? false;
			var playerContextButtons = new List<PlayerContextButton>();
			playerContextButtons.Add(new PlayerContextButton
			{
				Text = "Open profile",
				OnClick = () =>
				{
					var data = new PlayerStatisticsPopupPresenter.StateData
					{
						PlayerId = member.ProfileMasterId,
						OnCloseClicked = () => _services.UIService.CloseScreen<PlayerStatisticsPopupPresenter>().Forget()
					};
					_services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(data).Forget();
				}
			});

			if (isLocalPlayerLeader)
			{
				playerContextButtons.AddRange(new[]
				{
					new PlayerContextButton
					{
						Text = "Promote to leader",
						OnClick = () => PromotePartyMember(slot.PlayerId).Forget()
					},
					new ()
					{
						ContextStyle = PlayerButtonContextStyle.Red,
						Text = LocalizationManager.GetTranslation(ScriptTerms.UITSquads.kick),
						OnClick = () => KickPartyMember(slot.PlayerId).Forget()
					},
				});
			}

			var displayName = member.DisplayName;
			if (int.TryParse(member.Trophies, out var trophies))
			{
				displayName += $"\n{trophies} <sprite name=\"TrophyIcon\">";
			}

			TooltipUtils.OpenPlayerContextOptions(slot.NameView.PlayerNameLabel, Element, displayName, playerContextButtons);
		}

		private async UniTaskVoid KickPartyMember(string playfabId)
		{
			try
			{
				await _partyService.Kick(playfabId);
			}
			catch (PartyException pe)
			{
				MainInstaller.ResolveServices().GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, pe.Error.GetTranslation(),
					true,
					new GenericDialogButton()).Forget();
				FLog.Warn("Error on kicking squad member", pe);
			}
		}

		private async UniTaskVoid PromotePartyMember(string playfabId)
		{
			try
			{
				await _partyService.Promote(playfabId);
			}
			catch (PartyException pe)
			{
				MainInstaller.ResolveServices().GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, pe.Error.GetTranslation(),
					true,
					new GenericDialogButton()).Forget();
				FLog.Warn("Error on promoting squad member", pe);
			}
		}
	}
}