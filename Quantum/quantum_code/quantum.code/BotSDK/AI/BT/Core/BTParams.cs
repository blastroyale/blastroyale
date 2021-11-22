namespace Quantum {
	public unsafe struct BTParams {
		public Frame Frame;
		public BTAgent* BtAgent;
		public AIBlackboardComponent* Blackboard;
		public EntityRef Entity;
	}
}