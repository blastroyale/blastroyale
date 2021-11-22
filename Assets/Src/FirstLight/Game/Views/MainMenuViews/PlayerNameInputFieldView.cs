using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Player name input field. Let the user input his name, will appear above the player in the game.
	/// </summary>
	public class PlayerNameInputFieldView : MonoBehaviour
	{
		[SerializeField] private Button _textButton;
		[SerializeField] private TextMeshProUGUI _textField;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void OnValidate()
		{
			_textField = _textField == null ? GetComponent<TextMeshProUGUI>() : _textField;
		}

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_textButton.onClick.AddListener(OnSetPlayerButtonClicked);
			_gameDataProvider.PlayerDataProvider.NicknameId.InvokeObserve(OnPlayerNameChanged);
		}

		private void OnDestroy()
		{
			_gameDataProvider?.PlayerDataProvider?.NicknameId?.StopObserving(OnPlayerNameChanged);
		}

		private void OnPlayerNameChanged(string previousValue, string newValue)
		{
			_textField.text = _gameDataProvider.PlayerDataProvider.Nickname;
		}

		private void OnSetPlayerButtonClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnNameSet
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.NameHeroTitle, 
			                                                    _gameDataProvider.PlayerDataProvider.Nickname, 
			                                                    confirmButton, true);
		}

		private void OnNameSet(string newName)
		{
			_textField.text = newName;
			
			_services.CommandService.ExecuteCommand(new UpdatePlayerNicknameCommand { Nickname = newName });
		}
	}
}