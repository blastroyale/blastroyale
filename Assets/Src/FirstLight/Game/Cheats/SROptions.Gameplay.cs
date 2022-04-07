using System.ComponentModel;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
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
	
	
#if UNITY_EDITOR
	private bool _isRaycastGizmoShowing;

	[Category("Gameplay")]
	public bool ShowRaycastGizmo
	{
		get => _isRaycastGizmoShowing;
		set
		{
			_isRaycastGizmoShowing = value;

			if (_isRaycastGizmoShowing)
			{
				MainInstaller.Resolve<IGameServices>().TickService.SubscribeOnUpdate(ShowCurrentRaycastShots);
			}
			else
			{
				MainInstaller.Resolve<IGameServices>().TickService.Unsubscribe(ShowCurrentRaycastShots);
			}
		}
	}

	private void ShowCurrentRaycastShots(float deltaTime)
	{
		var runner = QuantumRunner.Default;
		var f = runner == null ? null : runner.Game?.Frames?.Verified;
		
		if (f == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		var raycasts = f.Filter<RaycastShot>();

		while(raycasts.Next(out var entityRef, out var raycastShot))
		{
			if (raycastShot.PreviousToLastBulletPosition.ToUnityVector3() != Vector3.zero)
			{
				Debug.DrawLine(raycastShot.PreviousToLastBulletPosition.ToUnityVector3(), 
				               raycastShot.LastBulletPosition.ToUnityVector3(), Color.magenta, deltaTime);
			}
		}
	}
#endif
}