using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This service allows to manage multiple <see cref="T"/> of the defined <typeparamref name="T"/> enum type.
	/// </summary>
	public interface IAudioFxService<T> : IDisposable where T : struct, Enum
	{
		/// <summary>
		/// Request main game's <see cref="AudioListenerMonoComponent"/>
		/// </summary>
		AudioListenerMonoComponent AudioListener { get; }

		/// <summary>
		/// Sets the BGM muting state across the game
		/// </summary>
		bool IsBgmMuted { get; set; }

		/// <summary>
		/// Sets the ambience muting state across the game
		/// </summary>
		bool IsAmbienceMuted { get; set; }
		
		/// <summary>
		/// Sets the 2D Sfx muting state across the game
		/// </summary>
		bool IsSfxMuted { get; set; }
		
		/// <summary>
		/// Sets the voice Sfx muting state across the game
		/// </summary>
		bool IsDialogueMuted { get; set; }

		/// <summary>
		/// Requests check to see if any music is currently playing
		/// </summary>
		bool IsMusicPlaying { get; }
		
		/// <summary>
		/// Requests check to see if any ambience is currently playing
		/// </summary>
		bool IsAmbiencePlaying { get; }

		/// <summary>
		/// Loads audio mixer and initializes the mixer groups
		/// </summary>
		UniTask LoadAudioMixers(IEnumerable audioMixers);

		/// <summary>
		/// Load a set of audio clips into memory, and into the loaded clips collection
		/// </summary>
		/// <param name="clips">Enumerable collection of audio clips and their associated IDs</param>
		UniTask LoadAudioClips(IEnumerable clips);

		/// <summary>
		/// Unload a set of audio clips from memory, and remove al references from loaded clips collection
		/// </summary>
		/// <param name="clips">Enumerable collection of audio clips and their associated IDs</param>
		void UnloadAudioClips(IEnumerable clips);

		/// <summary>
		/// Transitions the belonging audio mixer, to a snapshot over a transition
		/// </summary>
		void TransitionAudioMixer(string snapshotName, float transitionDuration);

		/// <summary>
		/// Tries to return the <see cref="AudioClip"/> mapped to the given <paramref name="id"/>.
		/// Returns true if the audio service currently has the <paramref name="clip"/> for the given <paramref name="id"/>.
		/// </summary>
		bool TryGetClipPlaybackData(T id, out AudioClipPlaybackData clip);

		/// <summary>
		/// Removes follow target from the current <see cref="AudioListenerMonoComponent"/> 
		/// </summary>
		void DetachAudioListener();

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 3D surround in the given <paramref name="worldPosition"/>.
		/// Returns the audio mono component that is playing the sound.
		/// </summary>
		AudioSourceMonoComponent PlayClip3D(T id, Vector3 worldPosition, string mixerGroupOverride = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns the audio mono component that is playing the sound.
		/// </summary>
		AudioSourceMonoComponent PlayClip2D(T id, string mixerGroupOverride = null);

		/// <summary>
		/// Inserts the given <paramref name="id"/> sound clip into the 2D sound queue, where it will be played at the
		/// soonest available opportunity (with the appropriate delay).
		/// </summary>
		void PlayClipQueued2D(T id, string mixerGroupOverride = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> music and transitions with a fade based on fade in and out durations
		/// </summary>
		void PlayMusic(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		               bool continueFromCurrentTime = false);

		/// <summary>
		/// Plays the given <paramref name="id"/> ambient loop and transitions with a fade based on fade in and out durations
		/// </summary>
		void PlayAmbience(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
						  bool continueFromCurrentTime = false);
		
		/// <summary>
		/// Plays the given <paramref name="id"/> SFX clip on music audio mixer, and switches to a music track after
		/// </summary>
		void PlaySequentialMusicTransition(T transitionClip, T musicClip);

		/// <summary>
		/// Stops all currently playing music
		/// </summary>
		void StopMusic(float fadeOutDuration = 0f);
		
		/// <summary>
		/// Stops all currently playing ambience
		/// </summary>
		void StopAmbience(float fadeOutDuration = 0f);
		
		/// <summary>
		/// Stops all currently playing SFX
		/// </summary>
		void StopAllSfx();
		
		/// <summary>
		/// Stops all current sounds in sound queue, and empties the sound queue
		/// </summary>
		void WipeSoundQueue();

		/// <summary>
		/// Requests the current playback time of the currently playing music track, in seconds
		/// </summary>
		float GetCurrentMusicPlaybackTime();
	}

	/// <inheritdoc />
	/// <remarks>
	/// Used only on internal creation data and should not be exposed to the views
	/// </remarks>
	public interface IAudioFxInternalService<T> : IAudioFxService<T> where T : struct, Enum
	{
		/// <summary>
		/// Plays a given <paramref name="id"/> sound clip with initialized playback values
		/// </summary>
		AudioSourceMonoComponent PlayClipInternal(Vector3 worldPosition, AudioSourceInitData? sourceInitData);
		
		/// <summary>
		/// Plays a given <paramref name="id"/> sound clip with initialized playback values
		/// </summary>
		void PlayMusicInternal(float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                       bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null);
		
		/// <summary>
		/// Plays a given <paramref name="id"/> sound clip with initialized playback values
		/// </summary>
		void PlayAmbienceInternal(float fadeInDuration = 0f, float fadeOutDuration = 0f,
								  bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null);
		
		/// <summary>
		/// Inserts the given <paramref name="id"/> sound clip into the 2D sound queue, where it will be played at the
		/// soonest available opportunity (with the appropriate delay), with the initialized playback values
		/// </summary>
		void PlayClipQueued2DInternal(AudioSourceInitData? sourceInitData);
		
		/// <summary>
		/// Add the given <paramref name="id"/> <paramref name="clip"/> to the service
		/// </summary>
		void AddAudioClips(T id, AudioClipPlaybackData clips);

		/// <summary>
		/// Removes the given <paramref name="id"/>'s <see cref="AudioClip"/> from the service
		/// </summary>
		void RemoveAudioClip(T id);

		/// <summary>
		/// Requests the default audio init properties, for a given spatial blend and volume multiplier
		/// </summary>
		AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData, string mixerGroupId);

		/// <summary>
		/// Requests a stored audio mixer group by ID
		/// </summary>
		AudioMixerGroup GetAudioMixerGroup(string id);
		
		/// <summary>
		/// Requests to check if a mixer group should be muted
		/// </summary>
		bool GetMuteStatus(string id);
	}

	

	/// <inheritdoc />
	public class AudioFxService<T> : IAudioFxInternalService<T> where T : struct, Enum
	{
		private readonly int _soundQueueBreakMs;
		private readonly GameObject _audioPoolParent;
		private readonly IDictionary<T, AudioClipPlaybackData> _audioClips = new Dictionary<T, AudioClipPlaybackData>();
		private readonly IObjectPool<AudioSourceMonoComponent> _sfxPlayerPool;
		
		protected AudioMixer _audioMixer;
		protected Dictionary<string, AudioMixerGroup> _mixerGroups;
		protected Dictionary<string, AudioMixerSnapshot> _mixerSnapshots;
		protected string _mixerMasterGroupId;
		protected string _mixerSfx2dGroupId;
		protected string _mixerSfx3dGroupId;
		protected string _mixerMusicGroupId;
		protected string _mixerDialogueGroupId;
		protected string _mixerAmbientGroupId;
		
		protected AudioSourceMonoComponent _activeMusicSource;
		protected AudioSourceMonoComponent _transitionMusicSource;
		protected AudioSourceMonoComponent _activeAmbienceSource;
		protected AudioSourceMonoComponent _transitionAmbienceSource;
		protected Queue<AudioSourceMonoComponent> _soundQueue;

		private bool _bgmMuted;
		private bool _sfxMuted;
		private bool _dialogueMuted;
		private bool _ambienceMuted;
		
		/// <inheritdoc />
		public AudioListenerMonoComponent AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _bgmMuted;
			set
			{
				_bgmMuted = value;
				_activeMusicSource.Source.mute = value;
				_transitionMusicSource.Source.mute = value;
			}
		}
		
		/// <inheritdoc />
		public bool IsAmbienceMuted
		{
			get => _ambienceMuted;
			set
			{
				_ambienceMuted = value;
				_activeAmbienceSource.Source.mute = value;
				_transitionAmbienceSource.Source.mute = value;
			}
		}

		/// <inheritdoc />
		public bool IsSfxMuted
		{
			get => _sfxMuted;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfxMuted = value;

				foreach (var asmc in audio)
				{
					// Default competitive game design, SFX toggle handles all sounds effects, AND ambient sfx.
					asmc.Source.mute = (value && asmc.MixerGroupID == _mixerSfx2dGroupId) ||
					                   (value && asmc.MixerGroupID == _mixerSfx3dGroupId) ||
					                   (value && asmc.MixerGroupID == _mixerAmbientGroupId);
				}
			}
		}
		
		/// <inheritdoc />
		public bool IsDialogueMuted
		{
			get => _dialogueMuted;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_dialogueMuted = value;

				foreach (var asmc in audio)
				{
					if (asmc.MixerGroupID == _mixerDialogueGroupId)
					{
						asmc.Source.mute = value;
					}
				}
			}
		}

		/// <inheritdoc />
		public bool IsMusicPlaying => _activeMusicSource.Source.isPlaying || _transitionMusicSource.Source.isPlaying;
		public bool IsAmbiencePlaying => _activeAmbienceSource.Source.isPlaying || _transitionAmbienceSource.Source.isPlaying;
		
		public AudioFxService(float spatial3DThreshold, int soundQueueBreakMs)
		{
			_mixerSnapshots = new Dictionary<string, AudioMixerSnapshot>();
			_mixerGroups = new Dictionary<string, AudioMixerGroup>();
			_soundQueue = new Queue<AudioSourceMonoComponent>();
			_audioPoolParent = new GameObject("Audio Container");
			
			_soundQueueBreakMs = soundQueueBreakMs;
			
			var audioPlayer = new GameObject("Audio Source").AddComponent<AudioSourceMonoComponent>();
			audioPlayer.Source = audioPlayer.gameObject.AddComponent<AudioSource>();
			audioPlayer.transform.SetParent(_audioPoolParent.transform);
			audioPlayer.Source.playOnAwake = false;
			audioPlayer.gameObject.SetActive(false);

			_activeMusicSource = new GameObject("Music Source 1").AddComponent<AudioSourceMonoComponent>();
			_activeMusicSource.transform.SetParent(_audioPoolParent.transform);
			_activeMusicSource.Source = _activeMusicSource.gameObject.AddComponent<AudioSource>();
			_activeMusicSource.Source.playOnAwake = false;

			_transitionMusicSource = new GameObject("Music Source 2").AddComponent<AudioSourceMonoComponent>();
			_transitionMusicSource.transform.SetParent(_audioPoolParent.transform);
			_transitionMusicSource.Source = _transitionMusicSource.gameObject.AddComponent<AudioSource>();
			_transitionMusicSource.Source.playOnAwake = false;
			
			_activeAmbienceSource = new GameObject("Ambience Source 1").AddComponent<AudioSourceMonoComponent>();
			_activeAmbienceSource.transform.SetParent(_audioPoolParent.transform);
			_activeAmbienceSource.Source = _activeMusicSource.gameObject.AddComponent<AudioSource>();
			_activeAmbienceSource.Source.playOnAwake = false;

			_transitionAmbienceSource = new GameObject("Ambience Source 2").AddComponent<AudioSourceMonoComponent>();
			_transitionAmbienceSource.transform.SetParent(_audioPoolParent.transform);
			_transitionAmbienceSource.Source = _transitionMusicSource.gameObject.AddComponent<AudioSource>();
			_transitionAmbienceSource.Source.playOnAwake = false;

			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListenerMonoComponent>();
			AudioListener.transform.SetParent(_audioPoolParent.transform);
			AudioListener.Listener = AudioListener.gameObject.AddComponent<AudioListener>();
			AudioListener.Listener.enabled = true; // Probably not needed
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			var pool = new GameObjectPool<AudioSourceMonoComponent>(50, audioPlayer);

			pool.DespawnToSampleParent = true;
			_sfxPlayerPool = pool;
		}

		/// <inheritdoc />
		public virtual UniTask LoadAudioMixers(IEnumerable audioMixers)
		{
			return UniTask.CompletedTask;
		}

		/// <inheritdoc />
		public virtual UniTask LoadAudioClips(IEnumerable clips)
		{
			return UniTask.CompletedTask;
		}

		/// <inheritdoc />
		public virtual void UnloadAudioClips(IEnumerable clips)
		{
		}

		/// <inheritdoc />
		public void TransitionAudioMixer(string snapshotKey, float transitionDuration)
		{
			if (!_mixerSnapshots.TryGetValue(snapshotKey, out AudioMixerSnapshot snapshot))
			{
				return;
			}

			snapshot.TransitionTo(transitionDuration);
		}

		/// <inheritdoc />
		public virtual bool TryGetClipPlaybackData(T id, out AudioClipPlaybackData clipData)
		{
			if (!_audioClips.ContainsKey(id))
			{
				clipData = default;
				return false;
			}

			clipData = _audioClips[id];
			return true;
		}

		/// <inheritdoc />
		public void DetachAudioListener()
		{
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);
		}

		/// <inheritdoc />
		public virtual AudioSourceMonoComponent PlayClip3D(T id, Vector3 worldPosition, string mixerGroupOverride = null)
		{
			return null;
		}
		
		/// <inheritdoc />
		public virtual AudioSourceMonoComponent PlayClip2D(T id, string mixerGroupOverride = null)
		{
			return null;
		}
		
		/// <inheritdoc />
		public virtual void PlayClipQueued2D(T id, string mixerGroupOverride = null)
		{
		}
		
		/// <inheritdoc />
		public virtual void PlayMusic(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                              bool continueFromCurrentTime = false)
		{
		}

		public virtual void PlayAmbience(T id, float fadeInDuration = 0, float fadeOutDuration = 0, bool continueFromCurrentTime = false)
		{
		}

		/// <inheritdoc />
		public virtual void PlaySequentialMusicTransition(T transitionClip, T musicClip)
		{
		}

		/// <inheritdoc />
		public AudioSourceMonoComponent PlayClipInternal(Vector3 worldPosition, AudioSourceInitData? sourceInitData)
		{
			if (sourceInitData == null)
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, worldPosition, sourceInitData);
			return source;
		}

		/// <inheritdoc />
		public void PlayClipQueued2DInternal(AudioSourceInitData? sourceInitData)
		{
			if (sourceInitData == null)
			{
				return;
			}
			
			var source = _sfxPlayerPool.Spawn();
			
			// Will play immediately if there is no queued sounds, otherwise the queue will wait for current SFX to finish
			source.Play(_sfxPlayerPool, Vector3.zero, sourceInitData, _soundQueue.Count > 0);
			source.SoundPlayedCallback += ContinueSoundQueue;
			_soundQueue.Enqueue(source);
		}

		private async void ContinueSoundQueue(AudioSourceMonoComponent audioSource)
		{
			if (audioSource != _soundQueue.Peek())
			{
				return;
			}

			_soundQueue.Dequeue();

			await UniTask.Delay(_soundQueueBreakMs);

			if (_soundQueue.Count > 0)
			{
				_soundQueue.Peek().StartPreparedPlayback();
			}
		}
		
		/// <inheritdoc />
		public void PlayMusicInternal(float fadeInDuration = 0, float fadeOutDuration = 0, bool continueFromCurrentTime = false,
		                              AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}

			if (_activeMusicSource.Source.isPlaying)
			{
				_activeMusicSource.FadeVolume(_activeMusicSource.Source.volume, 0, fadeOutDuration);
				_transitionMusicSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration, SwapMusicSources);
				_transitionMusicSource.Play(null, Vector3.zero, sourceInitData);
			}
			else
			{
				_activeMusicSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration);
				_activeMusicSource.Play(null, Vector3.zero, sourceInitData);
			}
		}
		
		public void PlayAmbienceInternal(float fadeInDuration = 0, float fadeOutDuration = 0,
										 bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}

			if (_activeAmbienceSource.Source.isPlaying)
			{
				_activeAmbienceSource.FadeVolume(_activeAmbienceSource.Source.volume, 0, fadeOutDuration);
				_transitionAmbienceSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration, SwapAmbienceSources);
				_transitionAmbienceSource.Play(null, Vector3.zero, sourceInitData);
			}
			else
			{
				_activeAmbienceSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration);
				_activeAmbienceSource.Play(null, Vector3.zero, sourceInitData);
			}
		}

		private void SwapMusicSources(AudioSourceMonoComponent audioSource)
		{
			(_activeMusicSource, _transitionMusicSource) = (_transitionMusicSource, _activeMusicSource);
			_transitionMusicSource.Source.Stop();
		}
		
		private void SwapAmbienceSources(AudioSourceMonoComponent audioSource)
		{
			(_activeAmbienceSource, _transitionAmbienceSource) = (_transitionAmbienceSource, _activeAmbienceSource);
			_transitionAmbienceSource.Source.Stop();
		}

		/// <inheritdoc />
		public void StopMusic(float fadeOutDuration = 0f)
		{
			if (!_activeMusicSource.Source.isPlaying && !_transitionMusicSource.Source.isPlaying)
			{
				return;
			}

			if (fadeOutDuration <= 0)
			{
				_activeMusicSource.Source.Stop();
				_transitionMusicSource.Source.Stop();
			}
			else
			{
				_activeMusicSource.FadeVolume(_activeMusicSource.Source.volume, 0, fadeOutDuration);
				_transitionMusicSource.FadeVolume(_transitionMusicSource.Source.volume, 0, fadeOutDuration);
			}
		}

		public void StopAmbience(float fadeOutDuration = 0)
		{
			if (!_activeAmbienceSource.Source.isPlaying && !_transitionAmbienceSource.Source.isPlaying)
			{
				return;
			}

			if (fadeOutDuration <= 0)
			{
				_activeAmbienceSource.Source.Stop();
				_transitionAmbienceSource.Source.Stop();
			}
			else
			{
				_activeAmbienceSource.FadeVolume(_activeAmbienceSource.Source.volume, 0, fadeOutDuration);
				_transitionAmbienceSource.FadeVolume(_transitionAmbienceSource.Source.volume, 0, fadeOutDuration);
			}
		}

		/// <inheritdoc />
		public void StopAllSfx()
		{
			foreach (var source in _sfxPlayerPool.SpawnedReadOnly.ToList())
			{
				source.StopAndDespawn();
			}
			
			_soundQueue.Clear();
		}

		/// <inheritdoc />
		public void WipeSoundQueue()
		{
			foreach (var source in _soundQueue)
			{
				source.StopAndDespawn();
			}

			_soundQueue.Clear();
		}

		/// <inheritdoc />
		public virtual AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData, string mixerGroupId)
		{
			return default;
		}

		/// <inheritdoc />
		public AudioMixerGroup GetAudioMixerGroup(string id)
		{
			return _mixerGroups[id];
		}

		public bool GetMuteStatus(string id)
		{
			if (id == _mixerMusicGroupId)
			{
				return IsBgmMuted;
			}
			if (id == _mixerSfx2dGroupId || id == _mixerSfx3dGroupId || id == _mixerAmbientGroupId)
			{
				return IsSfxMuted;
			}
			if (id == _mixerDialogueGroupId)
			{
				return IsDialogueMuted;
			}

			return false;
		}

		/// <inheritdoc />
		public float GetCurrentMusicPlaybackTime()
		{
			return _activeMusicSource.Source.time;
		}
		
		/// <inheritdoc />
		public float GetCurrentAmbiencePlaybackTime()
		{
			return _activeAmbienceSource.Source.time;
		}

		/// <inheritdoc />
		public void AddAudioClips(T id, AudioClipPlaybackData playbackData)
		{
			_audioClips.Add(id, playbackData);
		}

		/// <inheritdoc />
		public void RemoveAudioClip(T id)
		{
			_audioClips.Remove(id);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_audioClips.Clear();

			if (_audioPoolParent != null)
			{
				UnityEngine.Object.Destroy(_audioPoolParent.gameObject);
			}
		}
	}
}