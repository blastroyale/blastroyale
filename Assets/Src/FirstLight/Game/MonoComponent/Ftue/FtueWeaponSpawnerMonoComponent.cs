using System;
using FirstLight.Game.Utils;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component spawns a <see cref="Weapon"/> on the it's current position
	/// </summary>
	public class FtueWeaponSpawnerMonoComponent : MonoBehaviour
	{
		[SerializeField] private PlayableDirector _timeline;
		[SerializeField] private GameId _weapon;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnPlayerWeaponCollected);
		}

		private void Start()
		{
			QuantumRunner.Default.Game.SendCommand(new WeaponSpawnCommand
			{
				Position = transform.position.ToFPVector3(), 
				Weapon = _weapon
			});
		}

		private void OnPlayerWeaponCollected(EventOnLocalPlayerWeaponChanged callback)
		{
			if (callback.WeaponGameId == _weapon)
			{
				_timeline.playableGraph.PlayTimeline();
			}
		}
	}
}
