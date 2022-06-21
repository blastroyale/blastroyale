using Photon.Deterministic;
using Photon.Deterministic.Server.Interface;
using Photon.Hive.Plugin;

namespace Quantum
{
	/// <summary>
	/// Class represents a quantum plugin. It wraps around a server.
	/// This instance is created by PluginFactory everytime a match starts.
	/// </summary>
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