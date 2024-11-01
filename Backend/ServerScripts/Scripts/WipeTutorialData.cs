using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using PlayFab.ServerModels;

namespace Scripts.Scripts
{
	/// <summary>
	/// Script to replace Hammers in all players inventories to a random weapon
	/// Hammer in player inventory should be an invalid state of the data.  
	/// </summary>
	public class WipeTutorialData : PlayfabScript
	{
		public override Environment GetEnvironment()
		{
			return Environment.DEV;
		}

		public override void Execute(ScriptParameters args)
		{
			RunAsync().Wait();
		}

		public async Task RunAsync()
		{
			var tasks = new List<Task>();
			
			foreach (var player in await GetAllPlayers())
			{
				tasks.Add(WipeTutorialSections(player));
			}

			await Task.WhenAll(tasks.ToArray());

			Console.WriteLine($"Wiped {tasks.Count} tutorial sections total!");
		}
		
		private async Task WipeTutorialSections(PlayerProfile profile)
		{
			var state = await ReadUserState(profile.PlayerId);
			if (state == null)
			{
				return;
			}
			
			state.UpdateModel(new TutorialData());
			
			await SetUserState(profile.PlayerId, state);
		}
	}
}
