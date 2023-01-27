using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;

public partial class SROptions
{
	[Category("Create Game Mode")] public MatchType MatchType { get; set; }

	[Category("Create Game Mode")] public string GameModeId { get; set; }

	[Category("Create Game Mode")] public string Mutators { get; set; }

	[Category("Create Game Mode")]
	public void SetGameMode()
	{
		MainInstaller.Resolve<IGameServices>().GameModeService.SelectedGameMode.Value =
			new GameModeInfo(GameModeId, MatchType,
				string.IsNullOrEmpty(Mutators)
					? new List<string>()
					: Mutators.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());
	}

	[Category("Squads")] public int TeamId { get; set; }

	[Category("Squads")]
	public void SetTeamId()
	{
		MainInstaller.Resolve<IGameServices>().MessageBrokerService.Publish(new SetTeamIdMessage(){TeamId = TeamId});
	}
}