using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <inheritdoc cref="AudioFxService{T}"/>
	public class GameAudioFxService : AudioFxService<AudioId>, IAudioFxService<AudioId>
	{
		private readonly IAssetResolverService _assetResolver;
		private readonly IMessageBrokerService _messageBroker;
		
		public GameAudioFxService(IAssetResolverService assetResolver, IMessageBrokerService messageBrokerService) : 
			base(GameConstants.Audio.SFX_2D_DEFFAULT_VOLUME, 
				GameConstants.Audio.SFX_3D_DEFAULT_VOLUME, 
				GameConstants.Audio.BGM_DEFAULT_VOLUME)
		{
			_assetResolver = assetResolver;
			_messageBroker = messageBrokerService;

			QuantumEvent.SubscribeManual<EventOnPlayerDamaged>(this, OnPlayerDamaged);
		}

		/// <inheritdoc />
		public override bool TryGetClip(AudioId id, out AudioClip clip)
		{
			var task = _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			
			clip = task.Result;

			return task.IsCompleted;
		}

		/// <inheritdoc cref="AudioFxService{T}.PlayClip3D"/>
		public new async void PlayClip3D(AudioId id, Vector3 worldPosition, float delay = 0f)
		{
			if (id == AudioId.None)
			{
				return;
			}
			
			var startTime = DateTime.Now;
			var clip = await _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			var loadingTime = (float) (DateTime.Now - startTime).TotalSeconds;

			if (!Application.isPlaying)
			{
				return;
			}

			if (loadingTime < clip.length)
			{
				base.PlayClip3D(id, worldPosition, delay);
			}
		}

		/// <inheritdoc cref="AudioFxService{T}.PlayClip2D"/>
		public new async void PlayClip2D(AudioId id, float delay = 0f)
		{
			if (id == AudioId.None)
			{
				return;
			}
			
			var startTime = DateTime.Now;
			var clip = await _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			var loadingTime = (float) (DateTime.Now - startTime).TotalSeconds;

			if (!Application.isPlaying)
			{
				return;
			}

			if (loadingTime < clip.length)
			{
				base.PlayClip2D(id, delay);
			}
		}

		/// <inheritdoc />
		public new async void PlayMusic(AudioId id, float delay = 0f)
		{
			if (id == AudioId.None)
			{
				return;
			}
			
			await _assetResolver.RequestAsset<AudioId, AudioClip>(id);

			if (!Application.isPlaying)
			{
				return;
			}
			
			base.PlayMusic(id, delay);
		}

		private void OnPlayerDamaged(EventOnPlayerDamaged callback)
		{
			var frame = callback.Game.Frames.Verified;
			
			if (callback.Game.PlayerIsLocal(callback.Player))
			{
				if (callback.ShieldDamage > 0)
				{
					PlayClip2D(AudioId.TakeShieldDamage);
				}
				else if (callback.HealthDamage > 0)
				{
					PlayClip2D(AudioId.TakeHealthDamage);
				}
			}
			else if (frame.TryGet<PlayerCharacter>(callback.Attacker, out var attacker) &&
			         callback.Game.PlayerIsLocal(attacker.Player))
			{
				if (callback.ShieldDamage > 0)
				{
					PlayClip2D(AudioId.HitShieldDamage);
				}
				else if (callback.HealthDamage > 0)
				{
					PlayClip2D(AudioId.HitHealthDamage);
				}
			}
		}
	}
}