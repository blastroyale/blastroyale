using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
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
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += OnLocalLobbyJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnLocalLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.LobbyDeleted += OnLobbyDeleted;
			_services.FLLobbyService.CurrentPartyCallbacks.LobbyChanged += OnLobbyChanged;

			Element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			CleanAll();
			UpdateMembers().Forget();
		}

		public override void OnScreenClose()
		{
			_services.MatchmakingService.IsMatchmaking.StopObserving(OnMatchmaking);
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined -= OnLocalLobbyJoined;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnLocalLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.LobbyChanged -= OnLobbyChanged;
			_services.FLLobbyService.CurrentPartyCallbacks.LobbyDeleted -= OnLobbyDeleted;
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnLobbyDeleted()
		{
			UpdateMembers().Forget();
		}

		private void OnLobbyChanged(ILobbyChanges msg)
		{
			UpdateMembers().Forget();
		}

		private void OnLocalLobbyChanged(ILobbyChanges msg)
		{
			UpdateMembers().Forget();
		}

		private void OnLocalLobbyJoined(Lobby lobby)
		{
			UpdateMembers().Forget();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			foreach (var menuPartyMember in AllMembers.Where(m => m.IsUsed))
			{
				menuPartyMember.NameView.UpdatePosition();
			}
		}

		private void OnMatchmaking(bool _, bool _2)
		{
			UpdateMembers().Forget();
		}

		public async UniTaskVoid UpdateMembers()
		{
			await _lock.WaitAsync();
			try
			{
				var partyLobby = _services.FLLobbyService.CurrentPartyLobby;

				if (partyLobby == null)
				{
					CleanAll();
					return;
				}

				var partyMembers = partyLobby.Players;

				// Remove old members
				foreach (var teamMatePosition in _teamMatePositions)
				{
					if (!teamMatePosition.IsUsed) continue;
					if (partyMembers.All(pm => pm.Id != teamMatePosition.PlayerId))
					{
						Clean(teamMatePosition);
					}
				}

				foreach (var member in partyMembers)
				{
					if (member.Id == AuthenticationService.Instance.PlayerId)
					{
						await UpdateSlot(_localPlayer, member);
						continue;
					}

					var currentSlot = _teamMatePositions.FirstOrDefault(slot => slot.PlayerId == member.Id);
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

		public async UniTask UpdateSlot(MenuPartyMember slot, Player member)
		{
			slot.PlayerId = member.Id;
			// Update ui
			slot.NameView.Enable(member.GetPlayerName(), member.Id == _services.FLLobbyService.CurrentPartyLobby.HostId,
				member.IsReady() || _services.MatchmakingService.IsMatchmaking.Value);
			// Local player is always on screen so we don't have to load their skin
			if (member.Id == AuthenticationService.Instance.PlayerId)
			{
				if ((_services.FLLobbyService.CurrentPartyLobby?.Players?.Count ?? 1) > 1)
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

			var playerSkin = member.GetPlayerCharacterSkin();
			var meleeSkin = member.GetPlayerMeleeSkin();

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

		private void CleanAll()
		{
			foreach (var teamMatePosition in _teamMatePositions)
			{
				Clean(teamMatePosition);
			}

			_localPlayer.NameView?.Disable();
			_localPlayer.ApplyInitialPosition();
		}

		public void Clean(MenuPartyMember slot)
		{
			slot.PlayerId = null;
			slot.NameView?.Disable();
			slot.SlotRoot.SetActive(false);
		}

		private void OpenPlayerOptions(MenuPartyMember slot, Player partyMember)
		{
			_services.GameSocialService.OpenPlayerOptions(slot.NameView.PlayerNameLabel, Element, slot.PlayerId, partyMember.GetPlayerName(), new PlayerContextSettings()
			{
				ShowTeamOptions = true,
				Position = TooltipPosition.Top
			});
		}

		private async UniTaskVoid KickPartyMember(string playerID)
		{
			await _services.FLLobbyService.KickPlayerFromParty(playerID);
		}

		private async UniTaskVoid PromotePartyMember(string playerID)
		{
			await _services.FLLobbyService.UpdatePartyHost(playerID);
		}
	}
}