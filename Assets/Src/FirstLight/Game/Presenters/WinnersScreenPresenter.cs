using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the winners screen, which shows who are the top 3 winners of the game
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


		private IMatchServices _matchServices;
		private IGameServices _gameServices;

		private Button _nextButton;
		private VisualElement _nameContainer;
		private int _usedCharactersIndex = 0;
		private Label[] _nameLabels;

		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_nextButton = Root.Q<Button>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;
			_nameContainer = Root.Q<VisualElement>("NameContainer").Required();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			// TODO Might have to forget this. If it works delete this comment
			return UpdateCharacters();
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
			_nameLabels = new Label[slots.Length];
			for (var i = 0; i < slots.Length; i++)
			{
				var player = firstPosition[i];
				var rankColor =
					_gameServices.LeaderboardService.GetRankColor(_gameServices.LeaderboardService.Ranked, (int) player.LeaderboardRank);


				var playerNameLabel = new Label();
				playerNameLabel.AddToClassList(UIService.UIService.USS_PLAYER_LABEL);
				playerNameLabel.style.color = rankColor;
				playerNameLabel.text = player.GetPlayerName();
				_nameContainer.Add(playerNameLabel);
				_nameLabels[i] = playerNameLabel;
				playerNameLabel.SetPositionBasedOnWorldPosition(slots[i].transform.position);


				var playerData = player.Data.Player;
				if (!playerData.IsValid || !_matchServices.MatchEndDataService.PlayerMatchData.ContainsKey(playerData))
				{
					continue;
				}

				var skin = _gameServices.CollectionService.GetCosmeticForGroup(_matchServices.MatchEndDataService.PlayerMatchData[playerData].Cosmetics, GameIdGroup.PlayerSkin);
				tasks.Add(slots[i].UpdateSkin(skin));
			}

			await UniTask.WhenAll(tasks);
			await UniTask.Delay(300);
			foreach (var t in slots)
			{
				if (t.IsDestroyed()) continue;
				t.AnimateVictory();
				await UniTask.Delay(500);
			}
		}
	}
}