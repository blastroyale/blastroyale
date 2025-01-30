using System;
using Quantum;

namespace GameLogicService.Models
{
	[Serializable]
	public class AddEquipmentRequest
	{
		public string PlayerId;
		public Equipment Equipment;
		public string TokenId;
	}
}