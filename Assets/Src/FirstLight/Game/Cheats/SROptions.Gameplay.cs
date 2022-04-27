using System.ComponentModel;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
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

	// TODO: Find a way to have this code without extra parameters in quantum component and without copy/paste the logic from quantum
	private void ShowCurrentRaycastShots(float dt)
	{
		var runner = QuantumRunner.Default;
		var f = runner == null ? null : runner.Game?.Frames?.PredictedPrevious;
		
		if (f == null)
		{
			Debug.LogWarning("Simulation is not running yet");
			return;
		}

		var raycasts = f.Filter<RaycastShots>();

		while(raycasts.Next(out var entityRef, out var shot))
		{
			var speed = shot.Speed;
			var deltaTime = runner.Game.Frames.Predicted.Time - shot.StartTime;
			var previousTime = shot.PreviousTime - shot.StartTime;

			var angleCount = shot.NumberOfShots + 1;
			var angleStep = shot.AttackAngle / (FP)angleCount;
			var angle = -(int) shot.AttackAngle / FP._2;

			if (shot.IsInstantShot || deltaTime > shot.Range / speed)
			{
				speed = FP._1;
				deltaTime = shot.Range / speed;
			}
			
			for (var i = 0; i < angleCount - 1; i++)
			{

				angle += angleStep;

				var direction = FPVector2.Rotate(shot.Direction, angle * FP.Deg2Rad).XOY * speed;
				var previousPosition = shot.SpawnPosition + direction * previousTime;
				var currentPosition = shot.SpawnPosition + direction * deltaTime;
				
				Debug.DrawLine(previousPosition.ToUnityVector3(), currentPosition.ToUnityVector3(), Color.magenta, dt);
				
			}
		}
	}
#endif
}