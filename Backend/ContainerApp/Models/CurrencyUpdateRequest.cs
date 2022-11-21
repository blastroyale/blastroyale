using System;

namespace ContainerApp.Cloudscript.Models
{
	[Serializable]
	public class CurrencyUpdateRequest
	{
		public string PlayerId { get; set; }
		public int CurrencyId { get; set; }
		public int Delta { get; set; }
	}
}