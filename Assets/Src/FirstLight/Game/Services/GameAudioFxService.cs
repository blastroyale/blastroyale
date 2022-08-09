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

		protected string _mixerMasterGroupId;
		protected string _mixerSfx2dGroupId;
		protected string _mixerSfx3dGroupId;
		protected string _mixerMusicGroupId;
		protected string _mixerVoiceGroupId;
		protected string _mixerAmbientGroupId;
		
		public GameAudioFxService(IAssetResolverService assetResolver) : base(GameConstants.Audio.SPATIAL_3D_THRESHOLD, GameConstants.Audio.SOUND_QUEUE_BREAK_MS)
		{
			_assetResolver = assetResolver;
		}

		public override async Task LoadAudioMixers(IEnumerable audioMixers)
		{
			var mixerConfigs = audioMixers as IReadOnlyDictionary<int, AudioMixerConfig>;
			var mainMixerConfig = mixerConfigs.Values.ToList().First();
			
			var mixerObject = await mainMixerConfig.AudioMixer.LoadAssetAsync<AudioMixer>().Task;

			_audioMixer = mixerObject;
			
			_mixerMasterGroupId = mainMixerConfig.MixerMasterKey;
			_mixerSfx2dGroupId = mainMixerConfig.MixerSfx2dKey;
			_mixerSfx3dGroupId = mainMixerConfig.MixerSfx3dKey;
			_mixerMusicGroupId = mainMixerConfig.MixerMusicKey;
			_mixerVoiceGroupId = mainMixerConfig.MixerVoiceKey;
			_mixerAmbientGroupId = mainMixerConfig.MixerAmbientKey;
			
			_mixerGroups.Add(_mixerMasterGroupId, _audioMixer.FindMatchingGroups(_mixerMasterGroupId).First());
			_mixerGroups.Add(_mixerSfx2dGroupId, _audioMixer.FindMatchingGroups(_mixerSfx2dGroupId).First());
			_mixerGroups.Add(_mixerSfx3dGroupId, _audioMixer.FindMatchingGroups(_mixerSfx3dGroupId).First());
			_mixerGroups.Add(_mixerMusicGroupId, _audioMixer.FindMatchingGroups(_mixerMusicGroupId).First());
			_mixerGroups.Add(_mixerVoiceGroupId, _audioMixer.FindMatchingGroups(_mixerVoiceGroupId).First());
			_mixerGroups.Add(_mixerAmbientGroupId, _audioMixer.FindMatchingGroups(_mixerAmbientGroupId).First());
			
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
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND, clipData);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.MixerGroup = GetAudioMixerGroup(_mixerSfx3dGroupId);
			updatedInitData.Mute = IsSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip3D(id, worldPosition, sourceInitData);
		}

		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip2D(AudioId id, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.MixerGroup = GetAudioMixerGroup(_mixerSfx2dGroupId);
			updatedInitData.Mute = IsSfxMuted;
			sourceInitData = updatedInitData;

			return base.PlayClip2D(id, sourceInitData);
		}

		/// <inheritdoc />
		public override void PlayMusic(AudioId id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                               bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.MixerGroup = GetAudioMixerGroup(_mixerMusicGroupId);
			updatedInitData.StartTime = continueFromCurrentTime ? GetCurrentMusicPlaybackTime() : 0;
			updatedInitData.Mute = IsBgmMuted;
			updatedInitData.Loop = true;
			sourceInitData = updatedInitData;

			base.PlayMusic(id, fadeInDuration, fadeOutDuration, continueFromCurrentTime, sourceInitData);
		}

		/// <inheritdoc />
		public override void PlayClipQueued2D(AudioId id, string mixerGroupId, AudioSourceInitData? sourceInitData = null)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return;
			}

			sourceInitData ??= GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData);

			var updatedInitData = sourceInitData.Value;
			updatedInitData.MixerGroup = GetAudioMixerGroup(mixerGroupId);
			updatedInitData.Mute = IsSfxMuted;
			sourceInitData = updatedInitData;

			base.PlayClipQueued2D(id, mixerGroupId, sourceInitData);
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