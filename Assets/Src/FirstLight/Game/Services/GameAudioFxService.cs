using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

		/// <inheritdoc />
		public override async Task LoadAudioClips(IEnumerable clips)
		{
			var convertedClips = clips as IReadOnlyDictionary<AudioId, AssetReference>;

			foreach (var convClip in convertedClips)
			{
				await LoadAudioClip(convClip.Key);
			}
		}

		/// <inheritdoc />
		public override void UnloadAudioClips(IEnumerable clips)
		{
			var convertedClips = clips as IReadOnlyDictionary<AudioId, AssetReference>;

			foreach (var convClip in convertedClips)
			{
				UnloadAudioClip(convClip.Key);
			}
		}

		/// <inheritdoc />
		public override async Task LoadAudioClip(AudioId id)
		{
			var clip = await _assetResolver.RequestAsset<AudioId, AudioClip>(id, false);
			AddAudioClip(id, clip);
		}
		
		/// <inheritdoc />
		public override void UnloadAudioClip(AudioId id)
		{
			RemoveAudioClip(id);
		}

		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None)
			{
				return null;
			}
			
			if (sourceInitData == null)
			{
				sourceInitData = GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);
			}

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = Is3dSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip3D(id, worldPosition, sourceInitData);
		}
		
		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip2D(AudioId id, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None)
			{
				return null;
			}
			
			if (sourceInitData == null)
			{
				sourceInitData = GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			}

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = Is2dSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip2D(id, sourceInitData);
		}
		
		/// <inheritdoc />
		public override void PlayMusic(AudioId id, float transitionDuration = 0f, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None)
			{
				return;
			}

			if (sourceInitData == null)
			{
				sourceInitData = GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			}
			
			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = IsBgmMuted;
			updatedInitData.Loop = true;
			sourceInitData = updatedInitData;
			
			if (!Application.isPlaying)
			{
				return;
			}
			
			base.PlayMusic(id, transitionDuration, sourceInitData);
		}
		
		/// <inheritdoc />
		public override AudioSourceInitData GetDefaultAudioInitProps(float spatialBlend)
		{
			return new AudioSourceInitData()
			{
				SpatialBlend = spatialBlend,
				Pitch = GameConstants.Audio.SFX_DEFAULT_PITCH,
				Volume = GameConstants.Audio.SFX_DEFAULT_VOLUME,
				Loop = false,
				Mute = false,
				StartTime = 0,
				RolloffMode = AudioRolloffMode.Linear,
				MinDistance = GameConstants.Audio.SFX_3D_MIN_DISTANCE,
				MaxDistance = GameConstants.Audio.SFX_3D_MAX_DISTANCE
			};
		}
	}
}