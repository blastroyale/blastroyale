using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the winners screen, which shows who are the top 3 winners of the game
	/// This is the second screen presented on the end game sequence flow
	/// </summary>
	public class WinnersScreenPresenter : UIPresenterData<WinnersScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action ContinueClicked;
		}

		[Serializable]
		public class CharacterList
		{
			[SerializeField] private BaseCharacterMonoComponent[] _values;

			public BaseCharacterMonoComponent[] Values => _values;
		}

		[SerializeField] private CharacterList[] _characters;
		[SerializeField, Required] private VisualTreeAsset _playerNameTemplate;

		private IMatchServices _matchServices;
		private IGameServices _gameServices;

		private LocalizedButton _nextButton;
		private VisualElement _worldPositioning;
		private int _usedCharactersIndex = 0;
		private Dictionary<int, VisualElement> _playerNames = new ();
		
		private ScreenHeaderElement _header;
		
		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_header.SetButtonsVisibility(false);
			_nextButton = Root.Q<LocalizedButton>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;
			_worldPositioning = Root.Q<VisualElement>("WorldPositioning").Required();
			_worldPositioning.RegisterCallback<GeometryChangedEvent>(ev =>
			{
				if (_usedCharactersIndex == 0) return;
				var slots = _characters[_usedCharactersIndex].Values;

				foreach (var (slot, label) in _playerNames)
				{
					label.SetPositionBasedOnWorldPosition(slots[slot].transform.position);
				}
			});
		}

		protected override async UniTask OnScreenOpen(bool reload)
		{
			SetHeaderGameModeInfo();
			
			await UpdateCharacters();

			AnimateCharacters().Forget();
		}

		private void SetHeaderGameModeInfo()
		{
			var matchConfig = _matchServices.MatchEndDataService.MatchConfig;
			
			//Check if current ended match was an Event
			if (matchConfig.MatchType == MatchType.Matchmaking)
			{
				var selectedGameMode = _gameServices.GameModeService.SelectedGameMode.Value.Entry;

				if (selectedGameMode.TimedEntry && selectedGameMode.MatchConfig.ConfigId == matchConfig.ConfigId)
				{
					_header.SetSubtitle(selectedGameMode.Visual.TitleTranslationKey.GetText());	
					return;
				}
			}
			
			var matchConfigTeamSize = _matchServices.MatchEndDataService.MatchConfig.TeamSize;
			switch (matchConfigTeamSize)
			{
				case 1:
					_header.SetSubtitle(ScriptLocalization.UITGameModeSelection.br_solo_title);
					break;
				case 2:
					_header.SetSubtitle(ScriptLocalization.UITGameModeSelection.br_duos_title);
					break;
				case 4:
					_header.SetSubtitle(ScriptLocalization.UITGameModeSelection.br_quads_title);
					break;
				default:
					return;
			}
		}

		private async UniTaskVoid AnimateCharacters()
		{
			var characters = _characters[_usedCharactersIndex].Values;
			await UniTask.Delay(300);
			if (characters == null) return;
			foreach (var t in characters)
			{
				if (t.IsDestroyed()) continue;
				t.AnimateVictory();
				await UniTask.Delay(500);
			}
		}

		private async UniTask UpdateCharacters()
		{
			// Wait 1 frame so the virtual camera activates and we can fetch the proper position of the characters on screen
			await UniTask.DelayFrame(1);
			var playersData = _matchServices.MatchEndDataService.QuantumPlayerMatchData;
			playersData.SortByPlayerRank(false);
			var firstPosition = playersData.Where(data => data.PlayerRank == 1).Take(4).ToList();

			// we don't have a setup configured for that many players
			if (_characters.Length < firstPosition.Count)
			{
				return;
			}

			_usedCharactersIndex = firstPosition.Count - 1;
			var slots = _characters[_usedCharactersIndex].Values;

			var tasks = new List<UniTask>();
			for (var i = 0; i < slots.Length; i++)
			{
				var player = firstPosition[i];
				var rankColor =
					_gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) player.LeaderboardRank);

				var playerName = _playerNameTemplate.CloneTree();
				playerName.AttachView<VisualElement, PlayerNameView>(this, out var view);
				view.SetData(player.GetPlayerName(), player.UnityId, (int) player.Data.PlayerTrophies, rankColor);
				_worldPositioning.Add(playerName);
				playerName.SetPositionBasedOnWorldPosition(slots[i].transform.position);
				_playerNames.Add(i, playerName);
				var playerData = player.Data.Player;
				if (!playerData.IsValid || !_matchServices.MatchEndDataService.PlayerMatchData.ContainsKey(playerData))
				{
					continue;
				}

				var skin = _gameServices.CollectionService.GetCosmeticForGroup(
					_matchServices.MatchEndDataService.PlayerMatchData[playerData].Cosmetics, GameIdGroup.PlayerSkin);
				tasks.Add(slots[i].UpdateSkin(skin, false));
			}

			await UniTask.WhenAll(tasks);
		}
	}
}