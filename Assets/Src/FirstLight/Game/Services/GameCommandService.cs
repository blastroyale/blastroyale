using System;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.NativeUi;
using FirstLight.Services;
using PlayFab;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <inheritdoc cref="ICommandService{TGameLogic}"/>
	public interface IGameCommandService
	{
		/// <inheritdoc cref="ICommandService{TGameLogic}.ExecuteCommand{TCommand}"/>
		void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand;
	}
	
	/// <inheritdoc />
	public class GameCommandService : IGameCommandService
	{
		private readonly IDataProvider _dataProvider;
		private readonly IGameLogic _gameLogic;
		
		public GameCommandService(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			_gameLogic = gameLogic;
			_dataProvider = dataProvider;
		}
		
		/// <inheritdoc cref="CommandService{TGameLogic}.ExecuteCommand{TCommand}" />
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand
		{
			try
			{
				command.Execute(_gameLogic, _dataProvider);
			}
			catch (Exception e)
			{
				var title = "Game Exception";
				var button = new AlertButton
				{
					Callback = Application.Quit,
					Style = AlertButtonStyle.Negative,
					Text = "Quit Game"
				};
			
				if (e is LogicException)
				{
					title = "Logic Exception";
				}
				else if (e is PlayFabException)
				{
					title = "PlayFab Exception";
				}
				
				NativeUiService.ShowAlertPopUp(false, title, e.Message, button);
				throw;
			}
		}

		/// <summary>
		/// Generic PlayFab error that is being called on PlayFab responses.
		/// Will throw an <see cref="PlayFabException"/> to be shown to the player.
		/// </summary>
		public static void OnPlayFabError(PlayFabError error)
		{
			throw new PlayFabException(PlayFabExceptionCode.AuthContextRequired, error.ErrorMessage);
		}
	}
}