using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.Cheats
{
	/// <summary>
	/// This is a helper class for AOT compilation code.
	/// IMPORTANT: Do not reference or use any of the code in this object
	/// </summary>
	public class AOTCode
	{
		public List<AppData> AppData;
		public List<IdData> IdData;
		public List<RngData> RngData;
		public List<PlayerData> PlayerData;

		/// <summary>
		/// Forces the Vibration to be added to the Android manifest
		/// </summary>
		private void TriggerDefault()
		{
			Handheld.Vibrate();
		}

		private void Commands()
		{
			var services = MainInstaller.Resolve<IGameServices>();
			
			services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
			services.CommandService.ExecuteCommand(new ConsumeIapCommand());
			services.CommandService.ExecuteCommand(new EquipItemCommand());
			services.CommandService.ExecuteCommand(new GameCompleteRewardsCommand());
			services.CommandService.ExecuteCommand(new UnequipItemCommand());
			services.CommandService.ExecuteCommand(new UpdatePlayerSkinCommand());
		}
	}
}