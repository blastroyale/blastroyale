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
		
		public GameAudioFxService(IAssetResolverService assetResolver) : 
			base(GameConstants.Audio.SFX_2D_DEFFAULT_VOLUME_MULTIPLIER, 
			     GameConstants.Audio.SFX_3D_DEFAULT_VOLUME_MULTIPLIER, 
				GameConstants.Audio.BGM_DEFAULT_VOLUME)
		{
			_assetResolver = assetResolver;
		}
		
		public override bool TryGetClip(AudioId id, out AudioClip clip)
		{
			var task = _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			
			clip = task.Result;

			return task.IsCompleted;
		}

		public new async void PlayClip3D(AudioId id, Vector3 worldPosition, AudioInitProps initProps = null)
		{
			if (id == AudioId.None)
			{
				return;
			}
			
			if (initProps == null)
			{
				initProps = GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			}
			
			initProps.Mute = Is3dSfxMuted;
			
			var startTime = DateTime.Now;
			var clip = await _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			var loadingTime = (float) (DateTime.Now - startTime).TotalSeconds;

			if (!Application.isPlaying)
			{
				return;
			}

			if (loadingTime < clip.length)
			{
				base.PlayClip3D(id, worldPosition, initProps);
			}
		}
		
		public new async void PlayClip2D(AudioId id, AudioInitProps initProps = null)
		{
			if (id == AudioId.None)
			{
				return;
			}
			
			if (initProps == null)
			{
				initProps = GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			}
			
			initProps.Mute = Is2dSfxMuted;
			
			var startTime = DateTime.Now;
			var clip = await _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			var loadingTime = (float) (DateTime.Now - startTime).TotalSeconds;

			if (!Application.isPlaying)
			{
				return;
			}

			if (loadingTime < clip.length)
			{
				base.PlayClip2D(id, initProps);
			}
		}

		/// <inheritdoc />
		public new async void PlayMusic(AudioId id, AudioInitProps initProps = null)
		{
			if (id == AudioId.None)
			{
				return;
			}

			if (initProps == null)
			{
				initProps = GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			}
			
			initProps.Loop = true;
			initProps.Mute = IsBgmMuted;
			
			await _assetResolver.RequestAsset<AudioId, AudioClip>(id);

			if (!Application.isPlaying)
			{
				return;
			}
			
			base.PlayMusic(id, initProps);
		}
		
		public new AudioInitProps GetDefaultAudioInitProps(float spatialBlend)
		{
			return new AudioInitProps()
			{
				SpatialBlend = spatialBlend,
				Pitch = GameConstants.Audio.SFX_DEFAULT_PITCH,
				Volume = GameConstants.Audio.SFX_DEFAULT_VOLUME,
				Loop = false,
				Mute = false,
				StartTime = 0,
				RolloffMode = AudioRolloffMode.Linear,
				MinDistance = 0f,
				MaxDistance = 75f
			};
		}
	}
}