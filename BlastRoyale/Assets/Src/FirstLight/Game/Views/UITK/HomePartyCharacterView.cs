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
	public class LocalPlayerMember : MenuPartyMember
	{
		public Transform PartyPosition;
		[NonSerialized] private Vector3 _initialPosition;
		[NonSerialized] private Quaternion _initialRotation;
		[NonSerialized] private bool _isUsingPartyPosition;

		public void SaveInitialPosition()
		{
			_initialPosition = SlotRoot.transform.position;
			_initialRotation = SlotRoot.transform.rotation;
		}

		public void ApplyInitialPosition()
		{
			if (!_isUsingPartyPosition) return;
			_isUsingPartyPosition = false;
			SlotRoot.transform.SetPositionAndRotation(_initialPosition, _initialRotation);
		}

		public void ApplyPartyPosition()
		{
			if (_isUsingPartyPosition) return;
			_isUsingPartyPosition = true;
			SlotRoot.transform.SetPositionAndRotation(PartyPosition.position, PartyPosition.rotation);
		}
	}

	[Serializable]
	public class HomePartyCharacterView : UIView
	{
		[SerializeField] private MenuPartyMember[] _teamMatePositions;
		[SerializeField] private LocalPlayerMember _localPlayer;
		[SerializeField] private VisualTreeAsset _playerNameTemplate;

		private IGameServices _services;
		private IPartyService _partyService;
		private readonly SemaphoreSlim _lock = new (1, 1);

		private List<MenuPartyMember> AllMembers
		{
			get
			{
				var list = _teamMatePositions.ToList();
				list.Add(_localPlayer);
				return list;
			}
		}

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();
			_partyService = _services.PartyService;

			_localPlayer.SaveInitialPosition();
			// Create labels before so they are ready for OnScreenOpen, because we can't add new views inside there
			foreach (var teamMatePosition in AllMembers)
			{
				CreateLabelFor(teamMatePosition);
			}
		}

		public override void OnScreenOpen(bool reload)
		{
			_services.MatchmakingService.IsMatchmaking.Observe(OnMatchmaking);
			_partyService.Members.Observe(OnPartyUpdate);
			_partyService.HasParty.Observe(OnHasPartyChanged);
			_partyService.LobbyProperties.Observe(OnLobbyPropertiesChanged);
			_partyService.LocalReadyStatus.Observe(OnLocalStatusChanged);
			Element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			CleanAllRemote();
			UpdateMembers().Forget();
		}

		public override void OnScreenClose()
		{
			_services.MatchmakingService.IsMatchmaking.StopObserving(OnMatchmaking);
			_partyService.LocalReadyStatus.StopObserving(OnLocalStatusChanged);
			_partyService.HasParty.StopObserving(OnHasPartyChanged);
			_partyService.Members.StopObserving(OnPartyUpdate);
			_partyService.LobbyProperties.StopObserving(OnLobbyPropertiesChanged);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			foreach (var menuPartyMember in AllMembers.Where(m => m.IsUsed))
			{
				menuPartyMember.NameView.UpdatePosition();
			}
		}

		private void OnLobbyPropertiesChanged(string arg1, string arg2, string arg3, ObservableUpdateType arg4)
		{
			UpdateMembers().Forget();
		}

		private void OnMatchmaking(bool _, bool _2)
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
				_localPlayer.ApplyInitialPosition();
				CleanAllRemote();
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
				//var partyMembers = GetFakeMembers();
				if (!_partyService.HasParty.Value)
				{
					CleanAllRemote();
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
			slot.PlayerId = member.PlayfabID;
			// Update ui
			slot.NameView.Enable(member.DisplayName, member.Leader, _partyService.IsReady(member) || _services.MatchmakingService.IsMatchmaking.Value);
			// Local player is always on screen so we don't have to load their skin
			if (member.Local)
			{
				if (_partyService.Members.Count > 1)
				{
					((LocalPlayerMember) slot).ApplyPartyPosition();
				}
				else
				{
					((LocalPlayerMember) slot).ApplyInitialPosition();
				}

				slot.NameView.UpdatePosition();
				return;
			}

			slot.NameView.UpdatePosition();
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
			await slot.Character.UpdateSkin(ItemFactory.Collection(playerSkin), false);
			await slot.Character.UpdateMeleeSkin(ItemFactory.Collection(meleeSkin));
			slot.Character.CharacterViewComponent.Clicked += _ => OpenPlayerOptions(slot, member);
		}

		public void CreateLabelFor(MenuPartyMember slot)
		{
			if (slot.NameView != null) return;
			var playerNameElement = _playerNameTemplate.Instantiate();
			Element.Add(playerNameElement);
			slot.NameView = new HomePartyNameView(slot.NameAnchor, slot.LabelScale);
			playerNameElement.AttachExistingView(Presenter, slot.NameView);
			slot.NameView.Disable();
		}

		private void CleanAllRemote()
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
						PlayfabID = member.ProfileMasterId
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

#if UNITY_EDITOR
		private List<PartyMember> GetFakeMembers()
		{
			return new List<PartyMember>()
			{
				new ()
				{
					Leader = true,
					PlayfabID = PlayFab.PlayFabSettings.staticPlayer.EntityId,
					RawProperties = new Dictionary<string, string>()
					{
						{PartyMember.PROFILE_MASTER_ID, PlayFab.PlayFabSettings.staticPlayer.PlayFabId},
						{PartyMember.TROPHIES_PROPERTY, "4242"},
						{PartyMember.CHARACTER_SKIN_PROPERTY, GameId.PlayerSkinEGirl.ToString()},
						{PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, "Local Player"},
						{PartyMember.READY_MEMBER_PROPERTY, "false"}
					}
				},
				new ()
				{
					Leader = false,
					PlayfabID = "FE254F35877468D3",
					RawProperties = new Dictionary<string, string>()
					{
						{PartyMember.PROFILE_MASTER_ID, "6BED68CD7667A34E"},
						{PartyMember.TROPHIES_PROPERTY, "4242"},
						{PartyMember.CHARACTER_SKIN_PROPERTY, GameId.PlayerSkinGearedApe.ToString()},
						{PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, "Member1"},
						{PartyMember.READY_MEMBER_PROPERTY, "false"}
					}
				},
				new ()
				{
					Leader = false,
					PlayfabID = "7C47BE0F485E1157",
					RawProperties = new Dictionary<string, string>()
					{
						{PartyMember.PROFILE_MASTER_ID, "643C1C2F2926C934"},
						{PartyMember.TROPHIES_PROPERTY, "423"},
						{PartyMember.CHARACTER_SKIN_PROPERTY, GameId.PlayerSkinNinja.ToString()},
						{PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, "Member2"},
						{PartyMember.READY_MEMBER_PROPERTY, "false"}
					}
				},
				new ()
				{
					Leader = false,
					PlayfabID = "313AC847A0DC6817",
					RawProperties = new Dictionary<string, string>()
					{
						{PartyMember.PROFILE_MASTER_ID, "313AC847A0DC6817"},
						{PartyMember.TROPHIES_PROPERTY, "4242"},
						{PartyMember.CHARACTER_SKIN_PROPERTY, GameId.PlayerSkinLeprechaun.ToString()},
						{PartyMember.DISPLAY_NAME_MEMBER_PROPERTY, "Member3"},
						{PartyMember.READY_MEMBER_PROPERTY, "false"}
					}
				}
			};
		}
#endif
	}
}