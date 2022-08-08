using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

namespace FirstLight.Game.Services
{
	/// <inheritdoc cref="AudioFxService{T}"/>
	public class GameAudioFxService : AudioFxService<AudioId>, IAudioFxService<AudioId>
	{
		private readonly IAssetResolverService _assetResolver;

		public GameAudioFxService(IAssetResolverService assetResolver) : base()
		{
			_assetResolver = assetResolver;
		}

		public override async Task LoadAudioMixers(IEnumerable audioMixers)
		{
			var mixerConfigs = audioMixers as IReadOnlyDictionary<int, AudioMixerConfig>;
			var mainMixerConfig = mixerConfigs.Values.ToList().First();
			
			var mixerObject = await mainMixerConfig.AudioMixer.LoadAssetAsync<AudioMixer>().Task;

			_audioMixer = mixerObject;
			_mixerMasterGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerMasterKey).First();
			_mixer2dGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerSfx2dKey).First();
			_mixer3dGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerSfx3dKey).First();
			_mixerBgmGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerBgmKey).First();
			_mixerAncrGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerAncrKey).First();
			_mixerAmbGroup = _audioMixer.FindMatchingGroups(mainMixerConfig.MixerAmbKey).First();

			foreach (var snapshotKey in mainMixerConfig.SnapshotKeys)
			{
				_mixerSnapshots.Add(snapshotKey, _audioMixer.FindSnapshot(snapshotKey));
			}
		}

		/// <inheritdoc />
		public override async Task LoadAudioClips(IEnumerable clips)
		{
			var clipConfigs = clips as IReadOnlyDictionary<AudioId, AudioClipConfig>;
			var tasks = new List<Task>();

			foreach (var configKvp in clipConfigs)
			{
				var clipLoadTasks = new List<Task<AudioClip>>();

				foreach (var assetReference in configKvp.Value.AudioClips)
				{
					clipLoadTasks.Add(assetReference.LoadAssetAsync().Task);
				}

				tasks.Add(LoadAudioClipsForId(configKvp.Key, clipLoadTasks, configKvp.Value));
			}

			await Task.WhenAll(tasks);
		}

		private async Task LoadAudioClipsForId(AudioId id, List<Task<AudioClip>> clipTasks, AudioClipConfig clipConfig)
		{
			await Task.WhenAll(clipTasks);

			var clipPlaybackData = new AudioClipPlaybackData()
			{
				AudioClips = clipTasks.ConvertAll(task => task.Result),
				MinVol = clipConfig.BaseVolume - clipConfig.VolumeRandDeviation,
				MaxVol = clipConfig.BaseVolume + clipConfig.VolumeRandDeviation,
				MinPitch = clipConfig.BasePitch - clipConfig.PitchRandDeviation,
				MaxPitch = clipConfig.BasePitch + clipConfig.PitchRandDeviation
			};

			AddAudioClips(id, clipPlaybackData);
		}

		/// <inheritdoc />
		public override void UnloadAudioClips(IEnumerable clips)
		{
			var convertedClips = clips as IReadOnlyDictionary<AudioId, AudioClipConfig>;

			foreach (var configKvp in convertedClips)
			{
				foreach (var clipAssetRef in configKvp.Value.AudioClips)
				{
					if (clipAssetRef.IsValid())
					{
						clipAssetRef.ReleaseAsset();
					}
				}

				RemoveAudioClip(configKvp.Key);
			}
		}

		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition,
		                                                    AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None)
			{
				return null;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND, _audioClips[id]);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = IsSfxMuted;
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

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, _audioClips[id]);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.Mute = IsSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip2D(id, sourceInitData);
		}

		/// <inheritdoc />
		public override void PlayMusic(AudioId id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                               bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None)
			{
				return;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, _audioClips[id]);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.StartTime = continueFromCurrentTime ? GetCurrentMusicPlaybackTime() : 0;
			updatedInitData.Mute = IsBgmMuted;
			updatedInitData.Loop = true;
			sourceInitData = updatedInitData;

			base.PlayMusic(id, fadeInDuration, fadeOutDuration, continueFromCurrentTime, sourceInitData);
		}

		/// <inheritdoc />
		public override AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData)
		{
			return new AudioSourceInitData()
			{
				Clip = playbackData.PlaybackClip,
				SpatialBlend = spatialBlend,
				Pitch = playbackData.PlaybackPitch,
				Volume = playbackData.PlaybackVolume,
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