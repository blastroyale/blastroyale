using System.ComponentModel;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Playables;

public partial class SROptions
{
	[Category("Gameplay")]
	public void KillLocalPlayer()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatLocalPlayerKillCommand());
	}

	[Category("Gameplay")]
	public void WinLocalPlayer()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatCompleteKillCountCommand {IsLocalWinner = true});
	}

	[Category("Gameplay")]
	public void WinOtherPlayer()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatCompleteKillCountCommand());
	}

	[Category("Gameplay")]
	public void RefillAmmoAndSpecials()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatRefillAmmoAndSpecials());
	}

	[Category("Gameplay")]
	public void MakeLocalPlayerSuperTough()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatMakeLocalPlayerSuperToughCommand());
	}

	[Category("Gameplay")]
	public void KillAllExceptOne()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatKillAllExceptCommand()
		{
			Amount = 1
		});
	}

	[Category("Gameplay")]
	public void KillAllExceptTwo()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatKillAllExceptCommand()
		{
			Amount = 2
		});
	}

	[Category("Gameplay")]
	public void SkipTutorialSection()
	{
		Object.FindObjectOfType<PlayableDirector>().playableGraph.PlayTimeline();
	}

	[Category("Gameplay")]
	public void SpawnAirDropHere()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatSpawnAirDropCommand {OnPlayerPosition = true});
	}

	[Category("Gameplay")]
	public void SpawnAirDropRandom()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatSpawnAirDropCommand {OnPlayerPosition = false});
	}

	[Category("Gameplay")]
	public void SpawnSpecials()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		game.SendCommand(new CheatSpawnAllSpecialsCommand());
	}

	[Category("Gameplay")]
	public void SendTeamPingSelf()
	{
		QuantumRunner.Default.Game.SendCommand(new TeamPositionPingCommand()
		{
			Position = MainInstaller.Resolve<IMatchServices>().SpectateService.SpectatedPlayer.Value.Transform.position.ToFPVector2(),
			Type = TeamPingType.General
		});
	}

	[Category("Gameplay")]
	public void CrashUnity()
	{
		Utils.ForceCrash(ForcedCrashCategory.FatalError);
	}
}