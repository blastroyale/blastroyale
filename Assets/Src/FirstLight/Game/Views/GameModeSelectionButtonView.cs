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
	public class GameModeSelectionButtonView : IUIView
	{
		private const string SELECTED_CLASS = "selected";
		private const string VISIBLE_GAMEMODE_BUTTON = "visible-gamemodebutton";

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
					_button.AddToClassList(SELECTED_CLASS);
				}
				else
				{
					_button.RemoveFromClassList(SELECTED_CLASS);
				}
			}
		}
		
		private IGameServices _services;
		private VisualElement _root;
		private Button _button;
		private Label _gameModeLabel;
		private Label _gameModeDescriptionLabel;
		private Label _gameModeTimerLabel;
		private Label _modeTagTitleLabel;
		private bool _selected;
		private Coroutine _timerCoroutine;

		private VisualElement _mutatorsPanel;
		private List<VisualElement> _mutatorLines;

		private List<string> _gameModes = new (){"deathmatch", "battleroyale"};
		private List<string> _matchTypes = new (){"ranked", "casual", "custom"};
		private List<string> _mutators = new() {"speedup", "quickspecials", "healthyair"};

		public GameModeSelectionButtonView()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		public void Attached(VisualElement element)
		{
			_root = element;
			
			_button = _root.Q<Button>().Required();
			
			_gameModeLabel = _root.Q<Label>("GameModeLabel").Required();
			_gameModeDescriptionLabel = _root.Q<Label>("GameModeDescription").Required();
			_gameModeTimerLabel = _root.Q<Label>("GameModeTimer").Required();
			_modeTagTitleLabel = _root.Q<Label>("ModeTagTitle").Required();

			_mutatorsPanel = _root.Q<VisualElement>("MutatorsPanel").Required();
			_mutatorLines = _root.Query<VisualElement>("MutatorLine").ToList();
			
			_button.clicked += () => Clicked?.Invoke(this);
		}

		/// <inheritdoc />
		public void SubscribeToEvents()
		{
			UpdateTimer();
		}

		/// <inheritdoc />
		public void UnsubscribeFromEvents()
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
		public void SetData(int orderNumber, GameModeInfo gameModeInfo)
		{
			var visibleClass = VISIBLE_GAMEMODE_BUTTON + (orderNumber > 4 ? "" : orderNumber);
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
			
			_button.AddToClassList(GameModeInfo.Entry.MatchType.ToString().ToLower());
			_button.AddToClassList(GameModeInfo.Entry.GameModeId.ToLower());
			_gameModeLabel.text = GameModeInfo.Entry.GameModeId.ToUpper();
			_modeTagTitleLabel.text = GameModeInfo.Entry.MatchType == MatchType.Custom?"":GameModeInfo.Entry.MatchType.ToString().ToUpper();

			UpdateDescription();
			UpdateMutators();
		}

		private void RemoveClasses()
		{
			_gameModes.ForEach(mode => _button.RemoveFromClassList(mode));
			_matchTypes.ForEach(type => _button.RemoveFromClassList(type));
		}

		private void UpdateDescription()
		{
			if (GameModeInfo.Entry.GameModeId == "Custom Game")
			{
				_gameModeDescriptionLabel.text = ScriptLocalization.MainMenu.CustomGameDescription;
				return;
			}

			var gameModeId = GameModeInfo.Entry.GameModeId;
			var descLocalisationKey = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode()).DescriptionLocalisationKey;
			_gameModeDescriptionLabel.text = LocalizationManager.GetTranslation(descLocalisationKey);
		}

		private void UpdateMutators()
		{
			if (GameModeInfo.Entry.Mutators.Count == 0)
			{
				_mutatorsPanel.AddToClassList("hidden");
				return;
			}
			
			_mutatorsPanel.RemoveFromClassList("hidden");

			for (var mutatorIndex = 0; mutatorIndex < _mutatorLines.Count; mutatorIndex++)
			{
				if (mutatorIndex <= GameModeInfo.Entry.Mutators.Count - 1)
				{
					_mutatorLines[mutatorIndex].RemoveFromClassList("hidden");
					SetMutatorLine(_mutatorLines[mutatorIndex], GameModeInfo.Entry.Mutators[mutatorIndex]);
				}
				else
				{
					_mutatorLines[mutatorIndex].AddToClassList("hidden");
				}
			}
		}

		private void SetMutatorLine(VisualElement mutatorLine, string mutator)
		{
			mutatorLine.ClearClassList();
			mutatorLine.AddToClassList(mutator + "-mutator");
			var mutatorTitle = mutatorLine.Q<Label>("MutatorTitle").Required();
			mutatorTitle.text = mutator;
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
			while (true)
			{
				var timeLeft = GameModeInfo.EndTime - DateTime.UtcNow;

				_gameModeTimerLabel.text = timeLeft.ToString(@"hh\:mm\:ss");
				yield return new WaitForSeconds(1);
			}
		}
	}
}
