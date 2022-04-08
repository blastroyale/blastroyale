using FirstLight;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;

namespace Tests.Stubs;

/// <summary>
/// Server logic test class.
/// Mainly to add testing helpers and functions to manipulate logic inside tests.
/// </summary>
public class ServerTestLogic : GameLogic
{
	public ServerTestLogic(IConfigsProvider cfg, IDataProvider data) : base(
		new MessageBrokerService(),
		new ServerTestTime(), 
		data, 
		new AnalyticsService(), 
		cfg, 
		new TestServerAudio()
		)
	{
	}
}