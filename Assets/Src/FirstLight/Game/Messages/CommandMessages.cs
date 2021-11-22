using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Messages
{
	public struct PlayUiVfxCommandMessage : IMessage
	{
		public GameId Id;
		public Vector3 OriginWorldPosition;
		public uint Quantity;
	}
}