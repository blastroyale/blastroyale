using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	public struct HealthEntityInstantiatedMessage : IMessage
	{
		public EntityView Entity;
		public QuantumGame Game;
	}
	
	public struct HealthEntityDestroyedMessage : IMessage
	{
		public EntityView Entity;
		public QuantumGame Game;
	}
}