#nullable enable
using FirstLight.Game.Messages;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Game.Services
{
	public interface IEnvironmentService
	{
		public Environment? Environment { get; }
	}

	public class EnvironmentService : IEnvironmentService
	{
		private IMessageBrokerService _broker;

		public EnvironmentService(IMessageBrokerService broker)
		{
			_broker = broker;
			_broker.Subscribe<EnvironmentChanged>(OnEnvChanged);
		}

		private void OnEnvChanged(EnvironmentChanged ev)
		{
			Environment = ev.NewEnvironment;
		}

		public Environment? Environment { get; private set; }
	}
}