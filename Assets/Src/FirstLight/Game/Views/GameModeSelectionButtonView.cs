using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
		private const string GameModeButtonBase = "game-mode-button";
		private const string GameModeButtonSelectedModifier = GameModeButtonBase + "--selected";
		private const string GameModeButtonMutatorLine = GameModeButtonBase + "__mutator-line";

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
		
		private IGameServices _services;
		private Button _button;
		private Label _gameModeLabel;
		private Label _gameModeDescriptionLabel;
		private Label _gameModeTimerLabel;
		private bool _selected;
		private Coroutine _timerCoroutine;

		private VisualElement _mutatorsPanel;
		private List<VisualElement> _mutatorLines;

		private List<string> _gameModes = new (){"deathmatch", "battleroyale", "battleroyaletrios", "battleroyaleduos"};
		private List<string> _matchTypes = new (){"matchmaking", "forced", "custom"};

		public GameModeSelectionButtonView()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_button = element.Q<Button>().Required();
			
			var dataPanel = element.Q<VisualElement>("DataPanel");
			_gameModeLabel = dataPanel.Q<VisualElement>("Title").Q<Label>("Label").Required();
			_gameModeDescriptionLabel = dataPanel.Q<Label>("Description");
			_gameModeTimerLabel = dataPanel.Q<Label>("Timer");

			_mutatorsPanel = element.Q<VisualElement>("Mutators");
			_mutatorLines = _mutatorsPanel.Query<VisualElement>("MutatorLine").ToList();
			
			_button.clicked += () => Clicked?.Invoke(this);
		}

		public override void SubscribeToEvents()
		{
			UpdateTimer();
		}

		public override void UnsubscribeFromEvents()
		{
			if (_timerCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_timerCoroutine);
			}
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
			
			_button.AddToClassList($"{GameModeButtonBase}--{GameModeInfo.Entry.MatchType.ToString().ToLowerInvariant()}");
			_button.AddToClassList($"{GameModeButtonBase}--{GameModeInfo.Entry.GameModeId.ToLowerInvariant()}");

			_gameModeLabel.text = LocalizationUtils.GetTranslationForGameModeId(GameModeInfo.Entry.GameModeId);

			UpdateDescription();
			UpdateMutators();
		}

		private void RemoveClasses()
		{
			_gameModes.ForEach(mode => _button.RemoveFromClassList($"{GameModeButtonBase}--{mode}"));
			_matchTypes.ForEach(type => _button.RemoveFromClassList($"{GameModeButtonBase}--{type}"));
		}

		private void UpdateDescription()
		{
			if (GameModeInfo.Entry.GameModeId == GameConstants.GameModeId.FAKEGAMEMODE_CUSTOMGAME)
			{
				_gameModeDescriptionLabel.text = ScriptLocalization.UITGameModeSelection.custom_game_description;
				return;
			}

			var gameModeId = GameModeInfo.Entry.GameModeId;
			var descLocalisationKey = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId).DescriptionLocalisationKey;
			_gameModeDescriptionLabel.text = LocalizationManager.GetTranslation(descLocalisationKey);
		}

		private void UpdateMutators()
		{
			if (GameModeInfo.Entry.Mutators.Count == 0)
			{
				_mutatorsPanel.SetDisplay(false);
				return;
			}
			
			_mutatorsPanel.SetDisplay(true);

			for (var mutatorIndex = 0; mutatorIndex < _mutatorLines.Count; mutatorIndex++)
			{
				if (mutatorIndex <= GameModeInfo.Entry.Mutators.Count - 1)
				{
					_mutatorLines[mutatorIndex].SetDisplay(true);
					SetMutatorLine(_mutatorLines[mutatorIndex], GameModeInfo.Entry.Mutators[mutatorIndex]);
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

		private void UpdateTimer()
		{
			_gameModeTimerLabel.text = "";
			if (!GameModeInfo.IsFixed)
			{
				if (_timerCoroutine != null)
				{
					_services.CoroutineService.StopCoroutine(_timerCoroutine);
				}

				_timerCoroutine = _services.CoroutineService.StartCoroutine(UpdateTimerCoroutine());
			}
		}

		private IEnumerator UpdateTimerCoroutine()
		{
			var wait = new WaitForSeconds(1);
			while (true)
			{
				var timeLeft = GameModeInfo.EndTime - DateTime.UtcNow;
				_gameModeTimerLabel.text = timeLeft.ToString(@"hh\:mm\:ss");
				yield return wait;
			}
		}
	}
}
