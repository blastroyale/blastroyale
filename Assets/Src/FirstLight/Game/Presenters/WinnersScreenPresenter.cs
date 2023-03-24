using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using DG.Tweening;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Presenter for the winners screen, which shows who are the top 3 winners of the game
	/// </summary>
	public class WinnersScreenPresenter : UiToolkitPresenterData<WinnersScreenPresenter.StateData>
	{
		[SerializeField] private BaseCharacterMonoComponent _character1;
		[SerializeField] private BaseCharacterMonoComponent _character2;
		[SerializeField] private BaseCharacterMonoComponent _character3;
		[SerializeField] private CinemachineVirtualCamera _camera;

		public struct StateData
		{
			public Action ContinueClicked;
		}

		private Button _nextButton;
		private Label _playerName1;
		private Label _playerName2;
		private Label _playerName3;
		private VisualElement _playerBadge1;
		private VisualElement _playerBadge2;
		private VisualElement _playerBadge3;
		private IMatchServices _matchServices;

		protected override void OnInitialized()
		{
			base.OnInitialized();

			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupCamera();
			UpdateCharacters();
		}

		protected override async Task OnClosed()
		{
			StartMovingCharacterOut(_character1.gameObject);
			StartMovingCharacterOut(_character2.gameObject);
			StartMovingCharacterOut(_character3.gameObject);

			await base.OnClosed();
		}

		private void StartMovingCharacterOut(GameObject character)
		{
			var targetPosition = character.transform.position;
			targetPosition.x -= 60f;
			character.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.Linear);
		}

		protected override void QueryElements(VisualElement root)
		{
			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;

			_playerName1 = root.Q<Label>("PlayerName1").Required();
			_playerName2 = root.Q<Label>("PlayerName2").Required();
			_playerName3 = root.Q<Label>("PlayerName3").Required();

			_playerBadge1 = root.Q("Player1").Q("Badge").Required();
			_playerBadge2 = root.Q("Player2").Q("Badge").Required();
			_playerBadge3 = root.Q("Player3").Q("Badge").Required();
		}

		private void SetupCamera()
		{
			_camera.gameObject.SetActive(true);
		}

		private async void UpdateCharacters()
		{
			var playerData = _matchServices.MatchEndDataService.QuantumPlayerMatchData;
			playerData.SortByPlayerRank(false);

			var playerDataCount = Math.Min(playerData.Count, 3);
			var playerNames = new[] {_playerName1, _playerName2, _playerName3};
			var playerBadges = new[] {_playerBadge1, _playerBadge2, _playerBadge3};
			var characters = new[] {_character1, _character2, _character3};

			for (var i = 0; i < characters.Length; i++)
			{
				if (i < playerDataCount)
				{
					var player = playerData[i];

					characters[i].gameObject.SetActive(true);
					playerNames[i].visible = true;
					playerNames[i].text = player.GetPlayerName();

					playerBadges[i].RemoveModifiers();
					playerBadges[i].AddToClassList($"player__badge--position-{player.PlayerRank}");

					continue;
				}

				characters[i].gameObject.SetActive(false);
				playerNames[i].visible = false;
			}

			var tasks = new List<Task>();

			for (var i = 0; i < playerDataCount; i++)
			{
				var player = playerData[i].Data.Player;
				if (!player.IsValid || !_matchServices.MatchEndDataService.PlayerMatchData.ContainsKey(player))
				{
					continue;
				}
				tasks.Add(characters[i].UpdateSkin(playerData[i].Data.PlayerSkin,
					_matchServices.MatchEndDataService.PlayerMatchData[player].Gear.ToList()));
			}

			await Task.WhenAll(tasks);

			_character1.AnimateVictory();
		}
	}
}