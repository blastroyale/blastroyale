using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views.UITK;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This is responsible for displaying the screen during spectate mode,
	/// that follows your killer around.
	/// </summary>
	public class SpectateScreenPresenter : UIPresenterData<SpectateScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnLeaveClicked;
		}

		private const string USS_HIDE_CONTROLS = "hide-controls";
		private const string USS_ADD_FRIEND_BUTTON = "add-friend-button";
		private const string USS_ADD_FRIEND_BUTTON_BLOCKED = "add-friend-button--blocked";
		private const string USS_ADD_FRIEND_BUTTON_PENDING = "add-friend-button--pending";

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private IMatchServices _matchServices;
		private Action _friendPopAnimationCancel;

		[Q("Header")] private ScreenHeaderElement _header;
		[Q("PlayerName")] private Label _playerName;
		[Q("AddFriendLabel")] private Label _addFriendLabel;
		[Q("DefeatedYou")] private VisualElement _defeatedYou;
		[Q("ShowHide")] private VisualElement _showHide;
		[Q("ArrowLeft")] private ImageButton _arrowLeft;
		[Q("ArrowRight")] private ImageButton _arrowRight;
		[Q("AddFriend")] private ImageButton _addFriend;
		[Q("CurrentPlayerFriendIcon")] private VisualElement _currentPlayerFriendIcon;
		[Q("LeaveButton")] private LocalizedButton _leaveButton;
		[QView("StatusBars")] private StatusBarsView _statusBarsView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.ResolveData();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		protected override void QueryElements()
		{
			_statusBarsView.ForceOverheadUI();
			_statusBarsView.InitAll();

			_header.SetButtonsVisibility(false);

			_leaveButton.clicked += Data.OnLeaveClicked;
			_arrowLeft.clicked += OnPreviousPlayerClicked;
			_arrowRight.clicked += OnNextPlayerClicked;

			_addFriend.clicked += UniTask.Action(AddFriend);
			_showHide.RegisterCallback<ClickEvent, VisualElement>((_, r) =>
				r.ToggleInClassList(USS_HIDE_CONTROLS), Root);

			Root.SetupClicks(_services);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			// TODO: Use proper localization
			var gameModeID = _services.RoomService.CurrentRoom.Properties.SimulationMatchConfig.Value.GameModeID;
			_header.SetSubtitle(gameModeID.ToUpper());

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_matchServices.SpectateService.SpectatedPlayer.StopObservingAll(this);
			return base.OnScreenClose();
		}

		private async UniTaskVoid AddFriend()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning(considerStalling: false)) return;
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			var playerRef = _matchServices.SpectateService.SpectatedPlayer.Value.Player;
			// BOT LETS FAKE IT WHY NOT
			if (f.GetPlayerData(playerRef) == null)
			{
				QuantumPlayerMatchData data;
				unsafe
				{
					var playersData = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData;
					data = new QuantumPlayerMatchData(f, playersData[playerRef]);
				}

				await _services.GameSocialService.FakeInviteBot(data.GetPlayerName());
				UpdateFriendButton(RelationshipType.FriendRequest);
				return;
			}

			var unityId = f.GetPlayerData(playerRef).UnityId;
			var relation = FriendsService.Instance.GetFriendByID(unityId);
			if (relation != null) return;
			await FriendsService.Instance.AddFriendAsync(unityId).AsUniTask();

			UpdateFriendButton(RelationshipType.FriendRequest);
			_services.NotificationService.QueueNotification("Friend request sent");
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer _, SpectatedPlayer current)
		{
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			FixedArray<PlayerMatchData> playersData;
			unsafe
			{
				playersData = f.Unsafe.GetPointerSingleton<GameContainer>()->PlayersData;
			}

			if (!current.Player.IsValid)
			{
				FLog.Warn($"Invalid player entity {current.Entity} being spectated");
				return;
			}

			if (!f.TryGet<PlayerCharacter>(current.Entity, out var _))
			{
				return;
			}

			var data = new QuantumPlayerMatchData(f, playersData[current.Player]);
			var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, (int) data.LeaderboardRank);

			_playerName.text = data.GetPlayerName();
			_playerName.style.color = nameColor;
			_defeatedYou.SetDisplay(current.Player == _matchServices.MatchEndDataService.LocalPlayerKiller);

			var runtimePlayer = f.GetPlayerData(current.Player);
			if (runtimePlayer == null)
			{
				if (_services.GameSocialService.IsBotInvited(data.GetPlayerName()))
				{
					UpdateFriendButton(RelationshipType.FriendRequest);
				}
				else
				{
					UpdateFriendButton(null);
				}

				return;
			}

			var relationship = FriendsService.Instance.GetRelationShipById(runtimePlayer.UnityId);
			UpdateFriendButton(relationship);
		}

		private void UpdateFriendButton(RelationshipType type)
		{
			_addFriend.enabled = false;
			_friendPopAnimationCancel?.Invoke();

			_addFriend.RemoveModifiers();
			switch (type)
			{
				case RelationshipType.Friend:
					_addFriend.SetDisplay(false);
					break;
				case RelationshipType.Block:
					_addFriendLabel.text = "BLOCKED";
					_addFriend.AddToClassList(USS_ADD_FRIEND_BUTTON_BLOCKED);
					break;
				case RelationshipType.FriendRequest:
					_addFriendLabel.text = "REQUEST SENT";
					_addFriend.AddToClassList(USS_ADD_FRIEND_BUTTON_PENDING);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateFriendButton(Relationship relationship)
		{
			var canUseFriendSystem = _dataProvider.PlayerDataProvider.HasUnlocked(UnlockSystem.Friends);
			_addFriend.SetDisplay(canUseFriendSystem);
			if (!canUseFriendSystem)
			{
				return;
			}
			
			if (relationship == null)
			{
				_addFriend.enabled = true;
				_addFriendLabel.text = "ADD FRIEND";
				_addFriend.RemoveModifiers();
				_friendPopAnimationCancel?.Invoke();
				_friendPopAnimationCancel = _addFriend.AnimatePingRepeating(1.2f, duration: 300, delay: 3000);
				return;
			}

			UpdateFriendButton(relationship.Type);
		}

		private void OnNextPlayerClicked()
		{
			_matchServices.SpectateService.SwipeRight();
		}

		private void OnPreviousPlayerClicked()
		{
			_matchServices.SpectateService.SwipeLeft();
		}
	}
}