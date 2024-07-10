using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Text;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Photon.Deterministic;
using Quantum;
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

		public bool Selected
		{
			set => _button.EnableInClassList(USS_SELECTED, value);
		}

		private bool Disabled
		{
			get => !_button.enabledSelf;
			set { _button.SetEnabled(!value); }
		}

		private IPartyService _partyService;
		private LocalPrefsService _localPrefs;
		private IRemoteTextureService _remoteTexture;

		private AngledContainerElement _button;
		private Label _gameModeLabel;
		private Label _teamSizeLabel;
		private Label _timeLeftLabel;
		private VisualElement _teamSizeIcon;
		private Label _gameModeDescriptionLabel;
		private Label _disabledLabel;
		private VisualElement _char;
		private VisualElement _newEventEffectsHolder;
		private VisualElement _rewardContainer;
		private ImageButton _infoButton;
		private IVisualElementScheduledItem _scheduled;

		protected override void Attached()
		{
			var services = MainInstaller.ResolveServices();
			_partyService = services.PartyService;
			_localPrefs = services.LocalPrefsService;
			_remoteTexture = services.RemoteTextureService;
			_char = Element.Q<VisualElement>("Char").Required();
			_button = Element.Q<AngledContainerElement>().Required();

			var dataPanel = Element.Q<VisualElement>("TextContainer");
			_gameModeLabel = dataPanel.Q<Label>("Title").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_teamSizeIcon = dataPanel.Q<VisualElement>("TeamSizeIcon").Required();
			_teamSizeLabel = dataPanel.Q<Label>("TeamSizeLabel").Required();
			_timeLeftLabel = Element.Q<Label>("StatusLabel").Required();
			_infoButton = Element.Q<ImageButton>("InfoButton").Required();
			_rewardContainer = Element.Q<VisualElement>("RewardsContainer").Required();
			_button.clicked += () => Clicked?.Invoke(this);
			_infoButton.clicked += () =>
			{
				var entry = GameModeInfo.Entry;
				// Temp should use same popups as custom games
				var details = new StringBuilder();
				details.Append($"<align=\"center\"><size=+2><allcaps>{entry.Visual.TitleTranslationKey.GetText()}</allcaps></size><br>");
				details.Append($"<size=-2>{entry.Visual.DescriptionTranslationKey.GetText()}</size><br>");
				details.Append("<br></align>");
				details.Append("<align=\"left\">Reward Multipliers:<br>");
				foreach (var mp in entry.MatchConfig.RewardModifiers)
				{
					details.Append($"<indent=1em>{mp.Multiplier}x {mp.Id.GetLocalization()}{(mp.CollectedInsideGame ? " collected inside the game" : "")}<br></indent>");
				}

				details.Append("<br>Drop Chances:<br>");
				foreach (var mp in entry.MatchConfig.MetaItemDropOverwrites)
				{
					var currencyView = new CurrencyItemViewModel(ItemFactory.Currency(mp.Id, 1));
					details.Append($"<indent=1em>{currencyView.GetRichTextIcon()} on {mp.Place} with {(mp.DropRate * FP._100).ToString("0.##")}%<br></indent>");
				}


				details.Append("<br>Mutators:<br>");
				foreach (var mp in entry.MatchConfig.Mutators)
				{
					details.Append($"<indent=1em>{mp}<br></indent>");
				}

				details.Append("</align>");


				MainInstaller.ResolveServices().GenericDialogService.OpenSimpleMessage("Event Details Placeholder", details.ToString());
			};
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

		/// <summary>
		/// Sets the data needed to fill the button's visuals
		/// </summary>
		/// <param name="gameModeInfo">Game mode data to fill the button's visuals</param>
		public void SetData(GameModeInfo gameModeInfo)
		{
			_scheduled?.Pause();
			GameModeInfo = gameModeInfo;

			RemoveClasses();
			_button.AddToClassList($"{USS_BASE}--{GameModeInfo.Entry.Visual.CardModifier}");
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

			var showEventAnimation = !GameModeInfo.IsFixed && GameModeInfo.Duration.Contains(DateTime.UtcNow) && _localPrefs.LastSeenEvent.Value != GameModeInfo.GetKey();
			if (showEventAnimation)
			{
				_localPrefs.LastSeenEvent.Value = GameModeInfo.GetKey();
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

			_scheduled = Element.schedule.Execute(() =>
			{
				var timeLeft = gameModeInfo.Duration.GetEndsAtDateTime() - DateTime.UtcNow;
				if (comingSoon)
				{
					timeLeft = gameModeInfo.Duration.GetStartsAtDateTime() - DateTime.UtcNow;
				}

				if (timeLeft.TotalSeconds < 0)
				{
					if (comingSoon)
					{
						SetData(gameModeInfo);
						return;
					}

					Disabled = true;
					_timeLeftLabel.text = "EVENT FINISHED";
					return;
				}

				var prefix = comingSoon ? "NEXT EVENT IN\n" : "ENDS IN ";
				_timeLeftLabel.text = $"{prefix}{timeLeft.Display(showSeconds: true, showDays: false).ToLowerInvariant()}";
			}).Every(1000);
		}

		private void CheckCustomImage()
		{
			var url = GameModeInfo.Entry.Visual.OverwriteImageURL;
			if (string.IsNullOrWhiteSpace(GameModeInfo.Entry.Visual.OverwriteImageURL))
			{
				return;
			}

			var request = _remoteTexture.RequestTexture(url, cancellationToken: Presenter.GetCancellationTokenOnClose());
			_button.AddToClassList(USS_EVENT_CUSTOM_IMAGE);
			_button.ListenOnce<GeometryChangedEvent>(() =>
			{
				SetTexture(request).Forget();
			});
		}

		private async UniTaskVoid SetTexture(UniTask<Texture2D> task)
		{
			// Didn't load before geometry change, so add a loading effect
			if (task.Status == UniTaskStatus.Pending)
			{
				_char.AddToClassList("anim-fade");
			}
			var tex = await task;
			_char.style.backgroundImage = new StyleBackground(tex);
			_char.style.opacity = 1;
		}

		private void UpdateRewards()
		{
			_rewardContainer.Clear();
			if (GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites == null) return;
			foreach (var gameId in GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites.Select(a => a.Id).Distinct())
			{
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
			_teamSizeIcon.AddToClassList(gameModeInfo.Entry.Visual.IconSpriteClass);
			_teamSizeLabel.text = gameModeInfo.Entry.TeamSize + "";
		}

		public void UpdateDisabledStatus()
		{
			if (GameModeInfo.Entry.MatchConfig.MatchType == MatchType.Custom && _partyService.HasParty.Value)
			{
				Disabled = true;
				return;
			}

			if (GameModeInfo.Entry.TeamSize < _partyService.GetCurrentGroupSize())
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
			_gameModeLabel.text = GameModeInfo.Entry.Visual.TitleTranslationKey.GetText();
			_gameModeDescriptionLabel.text = GameModeInfo.Entry.Visual.DescriptionTranslationKey.GetText();
		}
	}
}
