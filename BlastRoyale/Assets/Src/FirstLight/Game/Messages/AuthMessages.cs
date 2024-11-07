using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct SuccessAuthentication : IMessage
	{
	}

	public struct DataReinitializedMessage : IMessage
	{
	}

	public struct DisplayNameChangedMessage : IMessage
	{
		public string NewPlayfabDisplayName;
	}

	public struct CoreLoopInitialized : IMessage
	{
		public bool ConnectedToMatch;
	}
}