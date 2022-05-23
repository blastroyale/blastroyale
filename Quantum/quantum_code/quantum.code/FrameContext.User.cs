namespace Quantum
{
	public unsafe partial class FrameContextUser
	{
		public int TargetAllLayerMask { get; internal set; }
		
		public EquipmentRarity MedianRarity { get; internal set; }
		public Equipment[] PlayerWeapons { get; internal set; }
	}
}