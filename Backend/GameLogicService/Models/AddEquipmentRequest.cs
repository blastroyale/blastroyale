using System;
using System.Collections.Generic;
using Quantum;

namespace GameLogicService.Models
{
	[Serializable]
	public class AddEquipmentRequest
	{
		public string PlayerId;
		public Equipment Equipment;
	}
}