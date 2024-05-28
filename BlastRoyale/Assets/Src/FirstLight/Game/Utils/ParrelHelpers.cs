using System;
using PlayFab;

namespace FirstLight.Game.Utils
{
	public static class ParrelHelpers
	{

		public static String DeviceID()
		{
			var id = PlayFabSettings.DeviceUniqueIdentifier;
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				id += "_clone_" + ParrelSync.ClonesManager.GetArgument();
			}
#endif
			return id;
		}
	}
}