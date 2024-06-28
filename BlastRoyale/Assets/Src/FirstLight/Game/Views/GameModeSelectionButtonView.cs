using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

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
			get => _selected;
			set
			{
				_selected = value;

				if (_selected)
				{
					_button.AddToClassList(GameModeButtonSelectedModifier);
				}
				else
				{
					_button.RemoveFromClassList(GameModeButtonSelectedModifier);
				}
			}
		}

		public bool Disabled
		{
			get => _disabled;
			set
			{
				_disabled = value;
				_button.SetEnabled(!_disabled);
				_disabledContainer.SetDisplay(_disabled);
				if (!_disabled) return;
				_disabledLabel.text = IsCustomGame() ? ScriptLocalization.UITGameModeSelection.custom_blocked_for_party : ScriptLocalization.UITGameModeSelection.too_many_players;
			}
		}

		private ImageButton _button;
		private Label _gameModeLabel;
		private Label _teamSizeLabel;
		private Label _timeLeftLabel;
		private VisualElement _teamSizeIcon;
		private Label _gameModeDescriptionLabel;
		private VisualElement _disabledContainer;
		private Label _disabledLabel;
		private bool _selected;
		private bool _disabled;

		protected override void Attached()
		{
			_button = Element.Q<ImageButton>().Required();

			var dataPanel = Element.Q<VisualElement>("TextContainer");
			_gameModeLabel = dataPanel.Q<Label>("Title").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_teamSizeIcon = dataPanel.Q<VisualElement>("TeamSizeIcon").Required();
			_teamSizeLabel = dataPanel.Q<Label>("TeamSizeLabel").Required();
			_disabledContainer = Element.Q<VisualElement>("Disabled").Required();
			_disabledLabel = Element.Q<Label>("DisabledLabel").Required();
			_timeLeftLabel = Element.Q<Label>("TimeLeftLabel").Required();

			_button.clicked += () => Clicked?.Invoke(this);
		}

		/// <summary>
		/// Sets the data needed to fill the button's visuals
		/// </summary>
		/// <param name="orderNumber">Order of the button on the list</param>
		/// <param name="gameModeInfo">Game mode data to fill the button's visuals</param>
		public void SetData(string buttonName, string visibleClass, GameModeInfo gameModeInfo)
		{
			_button.name = buttonName;
			_button.AddToClassList(visibleClass);

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
			_button.AddToClassList($"{GameModeButtonBase}--{GameModeInfo.Entry.GameModeCardModifier}");
			UpdateTeamSize(gameModeInfo);
			UpdateTitleAndDescription();
			if (gameModeInfo.IsFixed)
			{
				return;
			}

			Element.schedule.Execute(() =>
			{
				var timeLeft = gameModeInfo.EndTime - DateTime.UtcNow;
				if ( timeLeft.TotalSeconds < 0)
				{
					_timeLeftLabel.text="ENDED";
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
			if (IsCustomGame())
			{
				_teamSizeIcon.RemoveSpriteClasses();
				_teamSizeLabel.SetDisplay(false);
				_teamSizeIcon.SetDisplay(false);
				return;
			}

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

		private void UpdateMutators()
		{
			var mutators = GameModeInfo.Entry.MatchConfig.Mutators;
			if (mutators.Length == 0)
			{
				_mutatorsPanel.SetDisplay(false);
				return;
			}

			_mutatorsPanel.SetDisplay(true);

			for (var mutatorIndex = 0; mutatorIndex < _mutatorLines.Count; mutatorIndex++)
			{
				if (mutatorIndex <= mutators.Length - 1)
				{
					_mutatorLines[mutatorIndex].SetDisplay(true);
					SetMutatorLine(_mutatorLines[mutatorIndex], mutators[mutatorIndex]);
				}
				else
				{
					_mutatorLines[mutatorIndex].SetDisplay(false);
				}
			}
		}

		private void SetMutatorLine(VisualElement mutatorLine, string mutator)
		{
			mutatorLine.ClearClassList();
			mutatorLine.AddToClassList(GameModeButtonMutatorLine);
			mutatorLine.AddToClassList(mutator.ToLowerInvariant() + "-mutator");
			var mutatorTitle = mutatorLine.Q<Label>("Title").Required();
			mutatorTitle.text = mutator.ToUpperInvariant();
		}
	}
}