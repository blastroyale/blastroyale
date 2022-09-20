using System;
using System.ComponentModel;
using System.Linq;
using FirstLight.Game.Ids;
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
			                 Mutators.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());
	}
}