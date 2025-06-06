using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the GameModeSelectionButton elements in the GameModeSelectionScreen
	/// </summary>
	public class GameModeSelectionButtonView : UIView
	{
		private const string USS_BASE = "game-mode-card";
		private const string USS_SELECTED = USS_BASE + "--selected";
		private const string USS_EVENT_CUSTOM_IMAGE = USS_BASE + "--event-custom-image";
		private const string USS_COMING_SOON = USS_BASE + "--coming-soon";
		private const string USS_REWARD_ICON = USS_BASE + "__reward-icon";
		private const string USS_ANIM_ROOT = "anim-root";

		public GameModeInfo GameModeInfo { get; private set; }
		public PlayableDirector NewEventDirector;

		public event Action<GameModeSelectionButtonView> Clicked;
		public event Action<GameModeSelectionButtonView> ClickedInfo;

		public bool Selected
		{
			set => _button.EnableInClassList(USS_SELECTED, value);
		}

		private bool Disabled
		{
			get => !_button.enabledSelf;
			set { _button.SetEnabled(!value); }
		}

		private LocalPrefsService _localPrefs;
		private IRemoteTextureService _remoteTexture;
		private IFLLobbyService _lobbyService;
		private IGameModeService _gameModeService;

		private AngledContainerElement _button;
		private Label _gameModeLabel;
		private Label _eventLabel;
		private Label _teamSizeLabel;
		private Label _timeLeftLabel;
		private VisualElement _teamSizeIcon;
		private Label _gameModeDescriptionLabel;
		private Label _disabledLabel;
		private VisualElement _char;
		private VisualElement _newEventEffectsHolder;
		private VisualElement _rewardContainer;
		private ImageButton _infoButton;
		private VisualElement _cardBackground;
		private IVisualElementScheduledItem _scheduled;

		protected override void Attached()
		{
			var services = MainInstaller.ResolveServices();
			_localPrefs = services.LocalPrefsService;
			_remoteTexture = services.RemoteTextureService;
			_lobbyService = services.FLLobbyService;
			_gameModeService = services.GameModeService;
			_char = Element.Q<VisualElement>("Char").Required();
			_cardBackground = Element.Q<VisualElement>("Background").Required();
			_button = Element.Q<AngledContainerElement>().Required();

			var dataPanel = Element.Q<VisualElement>("TextContainer");
			_gameModeLabel = dataPanel.Q<Label>("Title").Required();
			_eventLabel = dataPanel.Q<Label>("EventTitle").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_teamSizeIcon = dataPanel.Q<VisualElement>("TeamSizeIcon").Required();
			_teamSizeLabel = dataPanel.Q<Label>("TeamSizeLabel").Required();
			_timeLeftLabel = Element.Q<Label>("StatusLabel").Required();
			_infoButton = Element.Q<ImageButton>("InfoButton").Required();
			_rewardContainer = Element.Q<VisualElement>("RewardsContainer").Required();
			_button.clicked += OnClicked;
			_infoButton.clicked += OnClickedInfoButton;
		}

		/// <summary>
		/// Sets the data needed to fill the button's visuals
		/// </summary>
		/// <param name="orderNumber">Order of the button on the list</param>
		/// <param name="gameModeInfo">Game mode data to fill the button's visuals</param>
		public void SetData(string buttonName, GameModeInfo gameModeInfo, params string[] extraClasses)
		{
			_button.name = buttonName;
			SetData(gameModeInfo);
			foreach (var extraClass in extraClasses)
			{
				_button.AddToClassList(extraClass);
			}
		}

		public void OnClicked()
		{
			Clicked?.Invoke(this);
		}

		private void OnClickedInfoButton()
		{
			ClickedInfo?.Invoke(this);
		}

		public void LevelLock(UnlockSystem unlockSystem)
		{
			_button.clicked -= OnClicked;
			_button.LevelLock(Presenter, Presenter.Root, unlockSystem, OnClicked);
		}

		/// <summary>
		/// Sets the data needed to fill the button's visuals
		/// </summary>
		/// <param name="gameModeInfo">Game mode data to fill the button's visuals</param>
		public void SetData(GameModeInfo gameModeInfo)
		{
			_scheduled?.Pause();
			GameModeInfo = gameModeInfo;

			RemoveClasses();
			if (gameModeInfo.Entry is FixedGameModeEntry fg)
			{
				_button.AddToClassList($"{USS_BASE}--{fg.CardModifier}");
			}
			else if (gameModeInfo.Entry is EventGameModeEntry ev)
			{
				_button.AddToClassList($"{USS_BASE}--event");
				if (string.IsNullOrEmpty(ev.ImageURL))
				{
					var teamSizeConfig = _gameModeService.GetTeamSizeFor(ev);
					_button.AddToClassList($"{USS_BASE}--event--" + teamSizeConfig.EventImageModifierByTeam);
				}
			}

			UpdateTeamSize(gameModeInfo);
			UpdateTitleAndDescription();
			UpdateDisabledStatus();
			UpdateRewards();
			CheckCustomImage();

			if (gameModeInfo.IsFixed)
			{
				return;
			}

			var now = DateTime.UtcNow;
			var comingSoon = gameModeInfo.Duration.GetStartsAtDateTime() > now;
			if (comingSoon)
			{
				_button.AddToClassList(USS_COMING_SOON);
			}

			var showEventAnimation = !GameModeInfo.IsFixed && GameModeInfo.Duration.Contains(DateTime.UtcNow) &&
				!_gameModeService.HasSeenEvent(GameModeInfo);
			if (showEventAnimation)
			{
				_gameModeService.MarkSeen(GameModeInfo);
				_button.AddToClassList(USS_ANIM_ROOT);
				Element.schedule.Execute(() =>
				{
					NewEventDirector.Play();
				});
			}
			else
			{
				_button.RemoveFromClassList(USS_ANIM_ROOT);
			}

			_scheduled = _timeLeftLabel.SetTimer(
				() => comingSoon ? gameModeInfo.Duration.GetStartsAtDateTime() : gameModeInfo.Duration.GetEndsAtDateTime(),
				comingSoon ? "NEXT EVENT IN\n" : "ENDS IN ",
				(el) =>
				{
					if (comingSoon)
					{
						SetData(gameModeInfo);
						return;
					}

					Disabled = true;
					el.text = "EVENT FINISHED";
				}
			);
		}

		private void CheckCustomImage()
		{
			if (GameModeInfo.Entry is EventGameModeEntry ev)
			{
				var hasCustomImage = !string.IsNullOrWhiteSpace(ev.ImageURL);
				var hasCustomBg = !string.IsNullOrWhiteSpace(ev.BackgroundImageURL);

				if (!hasCustomImage && !hasCustomBg) return;
				UniTask<Texture2D>? customBgRequest = null;
				UniTask<Texture2D>? customImageRequest = null;
				if (hasCustomBg)
				{
					customBgRequest =
						_remoteTexture.RequestTexture(ev.BackgroundImageURL, cancellationToken: Presenter.GetCancellationTokenOnClose());
				}

				if (hasCustomImage)
				{
					customImageRequest = _remoteTexture.RequestTexture(ev.ImageURL, cancellationToken: Presenter.GetCancellationTokenOnClose());
				}

				_button.AddToClassList(USS_EVENT_CUSTOM_IMAGE);
				_button.ListenOnce<GeometryChangedEvent>(() =>
				{
					if (customBgRequest.HasValue)
					{
						SetTexture(_cardBackground, customBgRequest.Value).Forget();
					}

					if (customImageRequest.HasValue)
					{
						SetTexture(_char, customImageRequest.Value).Forget();
					}
				});
			}
		}

		private async UniTaskVoid SetTexture(VisualElement element, UniTask<Texture2D> task)
		{
			// Didn't load before geometry change, so add a loading effect
			if (task.Status == UniTaskStatus.Pending)
			{
				element.AddToClassList("anim-fade");
			}

			var tex = await task;
			element.style.backgroundImage = new StyleBackground(tex);
			element.style.opacity = 1;
		}

		private void UpdateRewards()
		{
			_rewardContainer.Clear();
			if (GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites == null) return;
			foreach (var gameId in GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites.Select(a => a.Id).Distinct())
			{
				if (RewardLogic.TryGetRewardCurrencyGroupId(gameId, out var _))
				{
					continue;
				}

				var icon = new VisualElement();
				icon.AddToClassList(USS_REWARD_ICON);
				var viewModel = new CurrencyItemViewModel(ItemFactory.Currency(gameId, 0));
				viewModel.DrawIcon(icon);
				_rewardContainer.Add(icon);
			}
		}

		public bool IsCustomGame()
		{
			return GameModeInfo.Entry.MatchConfig.GameModeID == GameConstants.GameModeId.FAKEGAMEMODE_CUSTOMGAME;
		}

		private void UpdateTeamSize(GameModeInfo gameModeInfo)
		{
			_teamSizeIcon.RemoveSpriteClasses();
			var size = _gameModeService.GetTeamSizeFor(gameModeInfo.Entry);
			_teamSizeIcon.AddToClassList(size.IconSpriteClass);
			_teamSizeLabel.text = gameModeInfo.Entry.MatchConfig.TeamSize + "";
		}

		public void UpdateDisabledStatus()
		{
			if (GameModeInfo.Entry.MatchConfig.MatchType == MatchType.Custom && _lobbyService.HasTeamMembers())
			{
				Disabled = true;
				return;
			}

			if (_lobbyService.CurrentPartyLobby != null && GameModeInfo.Entry.MatchConfig.TeamSize < _lobbyService.CurrentPartyLobby.Players.Count)
			{
				Disabled = true;
				return;
			}

			if (!GameModeInfo.IsFixed && !GameModeInfo.Duration.Contains(DateTime.UtcNow))
			{
				Disabled = true;
				return;
			}

			Disabled = false;
		}

		private void RemoveClasses()
		{
			_button.RemoveModifiers();
		}

		private void UpdateTitleAndDescription()
		{
			_eventLabel.text = GameModeInfo.Entry.Title.GetText();
			_gameModeLabel.text = GameModeInfo.Entry.Title.GetText();
			_gameModeDescriptionLabel.text = GameModeInfo.Entry.Description.GetText();
		}
	}
}