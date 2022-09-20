using System;
using FirstLight.Game.Logic.RPC;

namespace ContainerApp.Cloudscript
{
	/// <summary>
	/// Objects that represent cloudscript response formats
	/// </summary>
	[Serializable]
	public class CloudscriptRequest
	{
		public PlayfabEntityProfile CallerEntityProfile { get; set; }

		public LogicRequest FunctionArgument { get; set; }
		
		public string PlayfabId => CallerEntityProfile?.Lineage?.MasterPlayerAccountId;
	}

	[Serializable]
	public class PlayfabEntityProfile
	{
		public PlayfabEntity Entity { get; set; }
		public PlayfabLineage Lineage { get; set; }
	}

	[Serializable]
	public class PlayfabEntity 
	{
		public string Id { get; set; }
	}

	[Serializable]
	public class PlayfabLineage
	{
		public string MasterPlayerAccountId { get; set; }
		
		public string TitlePlayerAccountId { get; set; }
	}
}

