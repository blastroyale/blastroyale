using System;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This class manages the visual components of the GameModeSelectionButton elements in the GameModeSelectionScreen
	/// </summary>
	public class GameModeSelectionButtonView : UIView
	{
		private const string GameModeButtonBase = "game-mode-card";
		private const string GameModeButtonSelectedModifier = GameModeButtonBase + "--selected";

		public GameModeInfo GameModeInfo { get; private set; }
		public event Action<GameModeSelectionButtonView> Clicked;

		public bool Selected
		{
			set => _button.EnableInClassList(GameModeButtonSelectedModifier, value);
		}

		public bool Disabled
		{
			get => !_button.enabledSelf;
			set => _button.SetEnabled(!value);
		}

		private ImageButton _button;
		private Label _gameModeLabel;
		private Label _teamSizeLabel;
		private Label _timeLeftLabel;
		private VisualElement _teamSizeIcon;
		private Label _gameModeDescriptionLabel;
		private Label _disabledLabel;

		protected override void Attached()
		{
			_button = Element.Q<ImageButton>().Required();

			var dataPanel = Element.Q<VisualElement>("TextContainer");
			_gameModeLabel = dataPanel.Q<Label>("Title").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_teamSizeIcon = dataPanel.Q<VisualElement>("TeamSizeIcon").Required();
			_teamSizeLabel = dataPanel.Q<Label>("TeamSizeLabel").Required();
			_timeLeftLabel = Element.Q<Label>("TimeLeftLabel").Required();

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
			GameModeInfo = gameModeInfo;

			RemoveClasses();
			_button.AddToClassList($"{GameModeButtonBase}--{GameModeInfo.Entry.Visual.CardModifier}");
			UpdateTeamSize(gameModeInfo);
			UpdateTitleAndDescription();
			if (gameModeInfo.IsFixed)
			{
				return;
			}

			Element.schedule.Execute(() =>
			{
				var timeLeft = gameModeInfo.EndTime - DateTime.UtcNow;
				if (timeLeft.TotalSeconds < 0)
				{
					_timeLeftLabel.text = "ENDED";
					return;
				}

				_timeLeftLabel.text = $"ENDS IN {timeLeft.Hours}h {timeLeft.Minutes}m {timeLeft.Seconds}s";
			}).Every(1000);
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