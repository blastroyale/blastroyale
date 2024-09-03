using System;
using System.Collections.Generic;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class LocalMigrationData
	{
		public static string SYNC_NAME = "sync-name";

		public List<string> RanMigrations = new ();
	}
}