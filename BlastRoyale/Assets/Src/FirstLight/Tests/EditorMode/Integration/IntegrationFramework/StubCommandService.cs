using System;
using System.Security.Cryptography;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// If copied to server tests can be re-implement to run commands on server as well and compare final data results
	/// </summary>
	public class StubCommandService : IGameCommandService
	{

		private IGameLogic _logic;
		private IDataProvider _data;
		private IGameServices _services;
		
		public StubCommandService(IGameLogic logic, IDataProvider data, IGameServices services)
		{
			_logic = logic;
			_data = data;
			_services = services;
		}
		
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : IGameCommand
		{
			var serialized = ModelSerializer.Serialize(command).Value;
			var deserialized = ModelSerializer.Deserialize<TCommand>(serialized, command.GetType());
			var ctx = new CommandExecutionContext(new LogicContainer().Build(_logic), new ServiceContainer().Build(_services),
				_data);
			deserialized.Execute(ctx);
		}

		public T ExecuteCommandWithResult<T>(IGameCommandWithResult<T> command)
		{
			throw new NotImplementedException();
		}
	}

}