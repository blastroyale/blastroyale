using System.ComponentModel;
using FirstLight.Game.Utils;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Playables;

public partial class SROptions
{
#if DEVELOPMENT_BUILD
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
		
		game.SendCommand(new CheatCompleteKillCountCommand { IsLocalWinner = true });
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
	public void MakeLocalPlayerBigDamager()
	{
		var game = QuantumRunner.Default.Game;
		if (game == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}
		
		game.SendCommand(new CheatMakeLocalPlayerBigDamagerCommand());
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
	public void SkipTutorialStep()
	{
		Object.FindObjectOfType<PlayableDirector>().playableGraph.PlayTimeline();
	}
#endif
}