using System;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
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
		private const string USS_COMING_SOON = USS_BASE + "--coming-soon";
		private const string USS_REWARD_ICON = USS_BASE + "__reward-icon";

		public GameModeInfo GameModeInfo { get; private set; }
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

		private ImageButton _button;
		private Label _gameModeLabel;
		private Label _teamSizeLabel;
		private Label _timeLeftLabel;
		private VisualElement _teamSizeIcon;
		private Label _gameModeDescriptionLabel;
		private Label _disabledLabel;
		private VisualElement _rewardContainer;
		private IVisualElementScheduledItem _scheduled;

		protected override void Attached()
		{
			_partyService = MainInstaller.ResolveServices().PartyService;
			_button = Element.Q<ImageButton>().Required();

			var dataPanel = Element.Q<VisualElement>("TextContainer");
			_gameModeLabel = dataPanel.Q<Label>("Title").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_teamSizeIcon = dataPanel.Q<VisualElement>("TeamSizeIcon").Required();
			_teamSizeLabel = dataPanel.Q<Label>("TeamSizeLabel").Required();
			_timeLeftLabel = Element.Q<Label>("StatusLabel").Required();
			_rewardContainer = Element.Q<VisualElement>("RewardsContainer").Required();

			_button.clicked += () => Clicked?.Invoke(this);
		}

		/// <summary>
		/// Sets the data needed to fill the button's visuals
		/// </summary>
		/// <param name="orderNumber">Order of the button on the list</param>
		/// <param name="gameModeInfo">Game mode data to fill the button's visuals</param>
		public void SetData(string buttonName, GameModeInfo gameModeInfo, params string[] extraClasses)
		{
			_button.name = buttonName;
			foreach (var extraClass in extraClasses)
			{
				_button.AddToClassList(extraClass);
			}

			SetData(gameModeInfo);
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
				_timeLeftLabel.text = $"{prefix}{timeLeft.Hours}h {timeLeft.Minutes}m {timeLeft.Seconds}s";
			}).Every(1000);
		}

		private void UpdateRewards()
		{
			_rewardContainer.Clear();
			if (GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites == null) return;
			foreach (var modifier in GameModeInfo.Entry.MatchConfig.MetaItemDropOverwrites)
			{
				var icon = new VisualElement();
				icon.AddToClassList(USS_REWARD_ICON);
				var viewModel = new CurrencyItemViewModel(ItemFactory.Currency(modifier.Id, 0));
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
			_gameModeLabel.text = LocalizationManager.GetTranslation(GameModeInfo.Entry.Visual.TitleTranslationKey);
			_gameModeDescriptionLabel.text = LocalizationManager.GetTranslation(GameModeInfo.Entry.Visual.DescriptionTranslationKey);
		}
	}
}