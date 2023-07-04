using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.SDK.Services;
using Quantum;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Messages
{
	public struct RequestStartFirstGameTutorialMessage : IMessage
	{
	}

	public struct RequestStartMetaMatchTutorialMessage : IMessage
	{
	}

	public struct CompletedTutorialSectionMessage : IMessage
	{
		public TutorialSection Section;
	}

	public enum TutorialFirstMatchStates
	{
		EnterKillFinalBot,
		EnterKill2Bots,
		
	}

	public struct AdvancedFirstMatchMessage : IMessage
	{
		public TutorialFirstMatchStates State;
	}
}