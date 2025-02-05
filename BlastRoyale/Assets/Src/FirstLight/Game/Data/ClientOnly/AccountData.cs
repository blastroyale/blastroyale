using System;

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
		public string PlayfabID;
	}
}