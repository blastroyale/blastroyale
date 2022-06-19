using Photon.Deterministic;
using Photon.Deterministic.Server.Interface;
using Photon.Hive.Plugin;

namespace Quantum
{
	public class CustomQuantumPlugin : DeterministicPlugin
	{
		protected CustomQuantumServer _server;

		public CustomQuantumPlugin(IServer server) : base(server)
		{
			Assert.Check(server is CustomQuantumServer);
			_server = (CustomQuantumServer)server;
		}

		public override void OnCloseGame(ICloseGameCallInfo info)
		{
			_server.Dispose();
			base.OnCloseGame(info);
		}
	}
}