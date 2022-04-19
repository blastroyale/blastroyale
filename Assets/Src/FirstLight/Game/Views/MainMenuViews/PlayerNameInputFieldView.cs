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
			_gameDataProvider.AppDataProvider.NicknameId.InvokeObserve(OnPlayerNameChanged);
		}

		private void OnDestroy()
		{
			_gameDataProvider?.AppDataProvider?.NicknameId?.StopObserving(OnPlayerNameChanged);
		}

		private void OnPlayerNameChanged(string previousValue, string newValue)
		{
			_textField.text = _gameDataProvider.AppDataProvider.Nickname;
		}

		private void OnSetPlayerButtonClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnNameSet
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.NameHeroTitle, 
			                                                    _gameDataProvider.AppDataProvider.Nickname, 
			                                                    confirmButton, true);
		}

		private void OnNameSet(string newName)
		{
			var title = "";
			var okButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = OnNameInvalidAcknowledged
			};

			if (newName.Length < GameConstants.PLAYER_NAME_MIN_LENGTH)
			{
				title = string.Format(ScriptLocalization.MainMenu.NameTooShort, GameConstants.PLAYER_NAME_MIN_LENGTH);
				_services.GenericDialogService.OpenDialog(title,false, okButton);
				return;
			}
			else if (newName.Length > GameConstants.PLAYER_NAME_MAX_LENGTH)
			{
				title = string.Format(ScriptLocalization.MainMenu.NameTooLong, GameConstants.PLAYER_NAME_MAX_LENGTH);
				_services.GenericDialogService.OpenDialog(title,false, okButton);
				return;
			}

			_textField.text = newName;
			_services.PlayfabService.UpdateNickname(newName);
		}

		private void OnNameInvalidAcknowledged()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnNameSet
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.NameHeroTitle, 
			                                                    _gameDataProvider.AppDataProvider.Nickname, 
			                                                    confirmButton, true);
		}
	}
}