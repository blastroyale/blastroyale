using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
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
			var clipConfigs = clips as IReadOnlyDictionary<AudioId, AudioClipConfig>;
			var tasks = new List<Task>();
			
			foreach (var clipConfig in clipConfigs)
			{
				var clipLoadTasks = new List<Task<AudioClip>>();
					
				foreach (var assetReference in clipConfig.Value.AudioClips)
				{
					clipLoadTasks.Add(assetReference.LoadAssetAsync().Task);
				}
				
				tasks.Add(LoadAudioClipsForId(clipConfig.Key, clipLoadTasks));
			}
			
			await Task.WhenAll(tasks);
		}

		/// <inheritdoc />
		public override async Task LoadAudioClipsForId(AudioId id, List<Task<AudioClip>> clipTasks)
		{
			await Task.WhenAll(clipTasks);
			
			AddAudioClips(id, clipTasks.ConvertAll(task => task.Result));
		}

		/// <inheritdoc />
		public override void UnloadAudioClips(IEnumerable clips)
		{
			var convertedClips = clips as IReadOnlyDictionary<AudioId, AudioClipConfig>;

			foreach (var convClip in convertedClips)
			{
				RemoveAudioClip(convClip.Key);
			}
		}

		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null, int clipIndex = 0)
		{
			if (id == AudioId.None)
			{
				return null;
			}
			
			sourceInitData ??= GetDefaultAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = Is3dSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip3D(id, worldPosition, sourceInitData, clipIndex);
		}
		
		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip2D(AudioId id, AudioSourceInitData? sourceInitData = null, int clipIndex = 0)
		{
			if (id == AudioId.None)
			{
				return null;
			}
			
			sourceInitData ??= GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = Is2dSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip2D(id, sourceInitData, clipIndex);
		}
		
		/// <inheritdoc />
		public override void PlayMusic(AudioId id, float fadeInDuration = 0f, float fadeOutDuration = 0f, AudioSourceInitData? sourceInitData = null, int clipIndex = 0)
		{
			if (id == AudioId.None)
			{
				return;
			}

			sourceInitData ??= GetDefaultAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND);
			
			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = IsBgmMuted;
			updatedInitData.Loop = true;
			sourceInitData = updatedInitData;

			base.PlayMusic(id, fadeInDuration, fadeOutDuration, sourceInitData, clipIndex);
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