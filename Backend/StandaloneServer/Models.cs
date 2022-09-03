using System.Threading.Tasks;
using FirstLight.Server.SDK.Services;

namespace StandaloneServer
{
	/// <summary>
	/// Playfab model for http responses
	/// </summary>
	public class PlayfabHttpResponse
	{
		public int code;
		public string status;
		public object data;
	}

	/// <summary>
	/// No mutex implementation of server mutex. Mindblowing.
	/// Reason of this is to make simpler to spin up a test server without external dependencies.
	/// </summary>
	public class NoMutex : IServerMutex
	{
		public void Dispose() {}
		public async Task Lock(string userId) {}
		public void Unlock(string userId) {}
	}
}

