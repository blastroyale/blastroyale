using System;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	public class WinnersScreenPresenter : UiToolkitPresenterData<WinnersScreenPresenter.StateData>
	{
		[SerializeField] private MenuCharacterMonoComponent _character1;
		[SerializeField] private MenuCharacterMonoComponent _character2;
		[SerializeField] private MenuCharacterMonoComponent _character3;
		[SerializeField] private CinemachineVirtualCamera _camera;
		
		public struct StateData
		{
			public Action ContinueClicked;
		}
		
		private Button _nextButton;
		private Label _playerName1;
		private Label _playerName2;
		private Label _playerName3;
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

		protected override void QueryElements(VisualElement root)
		{
			_nextButton = root.Q<Button>("NextButton").Required();
			_nextButton.clicked += Data.ContinueClicked;
			
			_playerName1 = root.Q<Label>("PlayerName1").Required();
			_playerName2 = root.Q<Label>("PlayerName2").Required();
			_playerName3 = root.Q<Label>("PlayerName3").Required();
		}

		private void SetupCamera()
		{
			_camera.gameObject.SetActive(true);
		}

		private async void UpdateCharacters()
		{
			var playerData = _matchServices.MatchDataService.QuantumPlayerMatchData;
			playerData.SortByPlayerRank(false);

			await Task.WhenAll(_character1.UpdateSkin(playerData[0].Data.PlayerSkin, _matchServices.MatchDataService.PlayerMatchData[playerData[0].Data.Player].Gear.ToList()),
								_character2.UpdateSkin(playerData[1].Data.PlayerSkin, _matchServices.MatchDataService.PlayerMatchData[playerData[1].Data.Player].Gear.ToList()),
								_character3.UpdateSkin(playerData[3].Data.PlayerSkin, _matchServices.MatchDataService.PlayerMatchData[playerData[2].Data.Player].Gear.ToList()));
			
			_character1.AnimateVictory();

			_playerName1.text = playerData[0].GetPlayerName();
			_playerName2.text = playerData[1].GetPlayerName();
			_playerName3.text = playerData[2].GetPlayerName();
		}
	}
}