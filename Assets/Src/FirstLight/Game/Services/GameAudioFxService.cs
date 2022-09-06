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

		public GameAudioFxService(IAssetResolverService assetResolver) : base(GameConstants.Audio.SPATIAL_3D_THRESHOLD, GameConstants.Audio.SOUND_QUEUE_BREAK_MS)
		{
			_assetResolver = assetResolver;
		}

		public override async Task LoadAudioMixers(IEnumerable audioMixers)
		{
			var mixerConfigs = audioMixers as IReadOnlyDictionary<AudioMixerID, AudioMixerConfig>;
			var mainMixerConfig = mixerConfigs[AudioMixerID.Default];
			
			var mixerObject = await mainMixerConfig.AudioMixer.LoadAssetAsync<AudioMixer>().Task;

			_audioMixer = mixerObject;
			
			_mixerMasterGroupId = mainMixerConfig.MixerMasterKey;
			_mixerSfx2dGroupId = mainMixerConfig.MixerSfx2dKey;
			_mixerSfx3dGroupId = mainMixerConfig.MixerSfx3dKey;
			_mixerMusicGroupId = mainMixerConfig.MixerMusicKey;
			_mixerDialogueGroupId = mainMixerConfig.MixerVoiceKey;
			_mixerAmbientGroupId = mainMixerConfig.MixerAmbientKey;
			
			_mixerGroups.Add(_mixerMasterGroupId, _audioMixer.FindMatchingGroups(_mixerMasterGroupId).First());
			_mixerGroups.Add(_mixerSfx2dGroupId, _audioMixer.FindMatchingGroups(_mixerSfx2dGroupId).First());
			_mixerGroups.Add(_mixerSfx3dGroupId, _audioMixer.FindMatchingGroups(_mixerSfx3dGroupId).First());
			_mixerGroups.Add(_mixerMusicGroupId, _audioMixer.FindMatchingGroups(_mixerMusicGroupId).First());
			_mixerGroups.Add(_mixerDialogueGroupId, _audioMixer.FindMatchingGroups(_mixerDialogueGroupId).First());
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
				Loop = clipConfig.Loop,
				MinVol = clipConfig.BaseVolume - clipConfig.VolumeRandDeviation,
				MaxVol = clipConfig.BaseVolume + clipConfig.VolumeRandDeviation,
				MinPitch = clipConfig.BasePitch - clipConfig.PitchRandDeviation,
				MaxPitch = clipConfig.BasePitch + clipConfig.PitchRandDeviation,
				PitchModPerLoop = clipConfig.PitchModPerLoop
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
		public override AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition, string mixerGroupOverride = null)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			var mixerGroupId = mixerGroupOverride ?? _mixerSfx2dGroupId;
			
			AudioSourceInitData sourceInitData = GetAudioInitProps(GameConstants.Audio.SFX_3D_SPATIAL_BLEND, clipData, mixerGroupId);
			var updatedInitData = sourceInitData;
			updatedInitData.MixerGroupAndId = new Tuple<AudioMixerGroup, string>(GetAudioMixerGroup(mixerGroupId), mixerGroupId);
			sourceInitData = updatedInitData;
			
			return PlayClipInternal(worldPosition, sourceInitData);
		}

		/// <inheritdoc />
		public override AudioSourceMonoComponent PlayClip2D(AudioId id, string mixerGroupOverride = null)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			var mixerGroupId = mixerGroupOverride ?? _mixerSfx2dGroupId;
			
			AudioSourceInitData sourceInitData = GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData, mixerGroupId);
			var updatedInitData = sourceInitData;
			updatedInitData.MixerGroupAndId = new Tuple<AudioMixerGroup, string>(GetAudioMixerGroup(mixerGroupId), mixerGroupId);
			sourceInitData = updatedInitData;

			return PlayClipInternal(Vector3.zero, sourceInitData);
		}
		
		/// <inheritdoc />
		public override void PlayClipQueued2D(AudioId id)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return;
			}

			AudioSourceInitData sourceInitData = GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData, _mixerSfx2dGroupId);

			var updatedInitData = sourceInitData;
			updatedInitData.MixerGroupAndId = new Tuple<AudioMixerGroup, string>(GetAudioMixerGroup(_mixerSfx2dGroupId), _mixerSfx2dGroupId);
			sourceInitData = updatedInitData;

			PlayClipQueued2DInternal(sourceInitData);
		}

		/// <inheritdoc />
		public override void PlayMusic(AudioId id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                               bool continueFromCurrentTime = false)
		{
			if (id == AudioId.None || !TryGetClipPlaybackData(id, out var clipData))
			{
				return;
			}

			AudioSourceInitData sourceInitData = GetAudioInitProps(GameConstants.Audio.SFX_2D_SPATIAL_BLEND, clipData, _mixerMusicGroupId);

			var updatedInitData = sourceInitData;
			updatedInitData.MixerGroupAndId = new Tuple<AudioMixerGroup, string>(GetAudioMixerGroup(_mixerMusicGroupId), _mixerMusicGroupId);
			updatedInitData.StartTime = continueFromCurrentTime ? GetCurrentMusicPlaybackTime() : 0;
			updatedInitData.Loop = true;
			sourceInitData = updatedInitData;

			PlayMusicInternal(fadeInDuration, fadeOutDuration, continueFromCurrentTime, sourceInitData);
		}
		
		/// <inheritdoc />
		public override void PlaySequentialMusicTransition(AudioId transitionClip, AudioId musicClip)
		{
			var transition = PlayClip2D(transitionClip, _mixerMusicGroupId);
			transition.SoundPlayedCallback += (source) =>
			{
				PlayMusic(musicClip);
			};
		}

		/// <inheritdoc />
		public override AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData, string mixerGroupId)
		{
			return new AudioSourceInitData()
			{
				Clip = playbackData.PlaybackClip,
				SpatialBlend = spatialBlend,
				Pitch = playbackData.PlaybackPitch,
				Volume = playbackData.PlaybackVolume,
				Loop = playbackData.Loop,
				Mute = GetMuteStatusForMixerGroup(mixerGroupId),
				StartTime = 0,
				RolloffMode = AudioRolloffMode.Linear,
				MinDistance = GameConstants.Audio.SFX_3D_MIN_DISTANCE,
				MaxDistance = GameConstants.Audio.SFX_3D_MAX_DISTANCE,
				PitchModPerLoop = playbackData.PitchModPerLoop
			};
		}
	}
}