using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using Quantum;
using Environment = FirstLight.Game.Services.Environment;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Contains all the data to be stored in game client regarding the user account & credentials
	/// </summary>
	[Serializable]
	public class AccountData
	{
		public string DeviceId;
		public string LastLoginEmail;
	}
}