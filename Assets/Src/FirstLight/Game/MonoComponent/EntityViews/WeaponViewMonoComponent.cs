using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using JetBrains.Annotations;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;


namespace FirstLight.Game.MonoComponent.EntityViews
{

	/// <summary>
	/// This Mono component handles particle system playback when player aims.
	/// </summary>
	public class WeaponViewMonoComponent : EntityViewBase
	{
		[SerializeField, Required] private ParticleSystem _particleSystem;
		[SerializeField, Required] private int _shells;
		private IGameServices _services;
		
		protected override void OnAwake()
		{
			_particleSystem.Stop();
			_services = MainInstaller.ResolveServices();
			QuantumEvent.Subscribe<EventOnPlayerAttack>(this, OnEventOnPlayerAttack);
			QuantumEvent.Subscribe<EventOnPlayerStopAttack>(this, OnEventOnPlayerStopAttack);
			QuantumEvent.Subscribe<EventOnGameEnded>(this, OnEventOnGameEnded);
		}

		private void OnEventOnPlayerAttack(EventOnPlayerAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
			}
			
			if (callback.Game.Frames.Predicted.IsCulled(EntityRef))
			{
				return;
			}

			_particleSystem.Stop();
			_particleSystem.time = 0;
			_particleSystem.Play();

			var t = transform;
			for (var x = 0; x < _shells; x++)
			{
				var currentEuler = t.rotation.eulerAngles;
				var rot = Quaternion.Euler(currentEuler.x, currentEuler.y+65, currentEuler.z);
				_services.VfxService.Spawn(VfxId.Shell).transform.SetPositionAndRotation(t.position, rot);
			}
			_services.AudioFxService.PlayClip3D(AudioId.Shells, t.position);
		}

		private void OnEventOnPlayerStopAttack(EventOnPlayerStopAttack callback)
		{
			if (EntityRef != callback.PlayerEntity)
			{
				return;
			}
			_particleSystem.Stop();
		}

		private void OnEventOnGameEnded(EventOnGameEnded callback)
		{
			_particleSystem.Stop();
		}
	}
}