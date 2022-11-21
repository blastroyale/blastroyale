using System;

namespace GameLogicApp.Cloudscript.Models
{
	[Serializable]
	public class CurrencyUpdateRequest
	{
		public string PlayerId { get; set; }
		public int CurrencyId { get; set; }
		public int Delta { get; set; }
	}
}