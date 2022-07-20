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
			base(GameConstants.Audio.SFX_2D_DEFFAULT_VOLUME, 
			     GameConstants.Audio.SFX_3D_DEFAULT_VOLUME, 
				GameConstants.Audio.BGM_DEFAULT_VOLUME)
		{
			_assetResolver = assetResolver;
		}

		/// <inheritdoc />
		public override bool TryGetClip(AudioId id, out AudioClip clip)
		{
			var task = _assetResolver.RequestAsset<AudioId, AudioClip>(id);
			
			clip = task.Result;

			return task.IsCompleted;
		}

		/// <inheritdoc cref="AudioFxService{T}.PlayClip3D"/>
		public new async void PlayClip3D(AudioId id, AudioObject initProps, Vector3 worldPosition, float delay = 0f)
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
				base.PlayClip3D(id, initProps, worldPosition, delay);
			}
		}

		/// <inheritdoc cref="AudioFxService{T}.PlayClip2D"/>
		public new async void PlayClip2D(AudioId id, AudioObject initProps, float delay = 0f)
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
				base.PlayClip2D(id, initProps, delay);
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
	}
}