using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Player name input field. Let the user input his name, will appear above the player in the game.
	/// </summary>
	public class PlayerNameInputFieldPresenter : UiPresenterData<PlayerNameInputFieldPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnNameSet;
		}

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}
		
		protected override void OnOpened()
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
			if (newName.Length > GameConstants.PLAYER_NAME_MAX_LENGTH)
			{
				title = string.Format(ScriptLocalization.MainMenu.NameTooLong, GameConstants.PLAYER_NAME_MAX_LENGTH);
				_services.GenericDialogService.OpenDialog(title,false, okButton);
				return;
			}

			if (newName != _gameDataProvider.AppDataProvider.Nickname)
			{
				_services.PlayfabService.UpdateNickname(newName);
			}

			Data.OnNameSet();
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