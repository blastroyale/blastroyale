using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
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
		
		public StubCommandService(IGameLogic logic, IDataProvider data)
		{
			_logic = logic;
			_data = data;
		}
		
		public void ExecuteCommand<TCommand>(TCommand command) where TCommand : struct, IGameCommand
		{
			var serialized = ModelSerializer.Serialize(command).Value;
			var deserialized = ModelSerializer.Deserialize<TCommand>(serialized, command.GetType());
			deserialized.Execute(_logic, _data);
		}
	}

}