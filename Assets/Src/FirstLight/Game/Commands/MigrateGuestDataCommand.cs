using System;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;
using UnityEngine;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Migrates relevant data from guest account, into an account with already set up login data (email, password, etc)
	/// Used for guests logging into their existing (hub) accounts for the first time
	/// </summary>
	public struct MigrateGuestDataCommand : IGameCommand
	{
		public MigrationData GuestMigrationData;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;
		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			// Mark all flags that were completed in guest data, but not player data
			foreach (TutorialSection section in Enum.GetValues(typeof(TutorialSection)))
			{
				if (ctx.Logic.PlayerLogic().MigratedGuestAccount) return;
				
				if (GuestMigrationData.TutorialSections.HasFlag(section) && !ctx.Logic.PlayerLogic().HasTutorialSection(section))
				{
					ctx.Logic.PlayerLogic().MarkTutorialSectionCompleted(section);
					Debug.LogError("migrate tutorial section completed: " + section);
				}
			}
			
			ctx.Logic.PlayerLogic().MarkGuestAccountMigrated();
		}
	}
}