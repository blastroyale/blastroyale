using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		/// Sets the 2D Sfx muting state across the game
		/// </summary>
		bool IsSfxMuted { get; set; }

		/// <summary>
		/// Requests check to see if any music is currently playing
		/// </summary>
		bool IsMusicPlaying { get; }

		/// <summary>
		/// Loads audio mixer and initializes the mixer groups
		/// </summary>
		Task LoadAudioMixers(IEnumerable audioMixers);

		/// <summary>
		/// Load a set of audio clips into memory, and into the loaded clips collection
		/// </summary>
		/// <param name="clips">Enumerable collection of audio clips and their associated IDs</param>
		Task LoadAudioClips(IEnumerable clips);

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
		AudioSourceMonoComponent PlayClip3D(T id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns the audio mono component that is playing the sound.
		/// </summary>
		AudioSourceMonoComponent PlayClip2D(T id, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Inserts the given <paramref name="id"/> sound clip into the 2D sound queue, where it will be played at the
		/// soonest available opportunity (with the appropriate delay).
		/// </summary>
		void PlayClipQueued2D(T id, string mixerGroupId, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> music and transitions with a fade based on <paramref name="transitionDuration"/>
		/// </summary>
		void PlayMusic(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		               bool continueFromCurrentTime = false, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Stops the music
		/// </summary>
		void StopMusic(float fadeOutDuration = 0f);

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
		AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData);

		/// <summary>
		/// Requests an audio mixer group by ID
		/// </summary>
		AudioMixerGroup GetAudioMixerGroup(string id);
	}

	/// <summary>
	/// Simple wrapper for <see cref="AudioListener"/> objects.
	/// Listener can be set to follow a target, and keep it's last rotation (needed for 3D sound)
	/// </summary>
	public class AudioListenerMonoComponent : MonoBehaviour
	{
		public AudioListener Listener;
		private Transform _followTarget;
		private Vector3 _followOffset;

		private void Update()
		{
			if (_followTarget != null)
			{
				transform.position = _followTarget.position + _followOffset;
			}
			else
			{
				transform.position = Vector3.zero;
			}
		}

		/// <summary>
		/// Sets the follow target for this audio listener
		/// If null, the listener will zero out it's position every frame instead
		/// </summary>
		public void SetFollowTarget(Transform newTarget, Vector3 followOffset, Quaternion rotation)
		{
			_followTarget = newTarget;
			_followOffset = followOffset;
			transform.rotation = rotation;
		}
	}

	/// <summary>
	/// A simple class wrapper for the <see cref="Source"/> objects
	/// </summary>
	public class AudioSourceMonoComponent : MonoBehaviour
	{
		public AudioSource Source;

		private IObjectPool<AudioSourceMonoComponent> _pool;
		private bool _canDespawn;
		private Coroutine _playSoundCoroutine;
		private Coroutine _fadeVolumeCoroutine;
		private Transform _followTarget;
		private Vector3 _followOffset;
		private Action<AudioSourceMonoComponent> _fadeVolumeCallback;
		private Action<AudioSourceMonoComponent> _soundPlayedCallback;

		private void Update()
		{
			if (_followTarget != null)
			{
				transform.position = _followTarget.position + _followOffset;
			}
		}

		private void OnDestroy()
		{
			_fadeVolumeCallback?.Invoke(this);
			_fadeVolumeCallback = null;
		}

		/// <summary>
		/// Initialize the audio source of the object with relevant properties
		/// </summary>
		/// /// <remarks>Note: if initialized with Loop as true, the audio source must be despawned manually.</remarks>
		public void Play(IObjectPool<AudioSourceMonoComponent> pool,
		                 Vector3? worldPos, AudioSourceInitData? sourceInitData = null,
		                 Action<AudioSourceMonoComponent> soundPlayedCallback = null, bool prepareOnly = false)
		{
			if (sourceInitData == null)
			{
				return;
			}

			_pool = pool;
			_soundPlayedCallback = soundPlayedCallback;
			
			Source.outputAudioMixerGroup = sourceInitData.Value.MixerGroup;
			Source.clip = sourceInitData.Value.Clip;
			Source.volume = sourceInitData.Value.Volume;
			Source.spatialBlend = sourceInitData.Value.SpatialBlend;
			Source.pitch = sourceInitData.Value.Pitch;
			Source.time = sourceInitData.Value.StartTime;
			Source.mute = sourceInitData.Value.Mute;
			Source.loop = sourceInitData.Value.Loop;
			Source.rolloffMode = sourceInitData.Value.RolloffMode;
			Source.minDistance = sourceInitData.Value.MinDistance;
			Source.maxDistance = sourceInitData.Value.MaxDistance;

			if (worldPos.HasValue)
			{
				transform.position = worldPos.Value;
			}

			_canDespawn = !sourceInitData.Value.Loop;

			if (!prepareOnly)
			{
				StartPreparedPlayback();
			}
		}

		/// <summary>
		/// Starts playback with currently initialized audio source values
		/// </summary>
		public void StartPreparedPlayback()
		{
			_playSoundCoroutine = StartCoroutine(PlaySoundCoroutine());
		}

		/// <summary>
		/// Sets the follow target for this audio listener
		/// If null, the listener will zero out it's position every frame instead
		/// </summary>
		public void SetFollowTarget(Transform newTarget, Vector3 followOffset, Quaternion rotation)
		{
			_followTarget = newTarget;
			_followOffset = followOffset;
			transform.rotation = rotation;
		}

		/// <summary>
		/// Flags this audio source to despawn at the end of the current clip playback
		/// </summary>
		public void FlagForDespawn()
		{
			_canDespawn = true;
		}

		/// <summary>
		/// Instantly stops the playing sound, and despawns component back to pool if possible
		/// </summary>
		public void StopAndDespawn()
		{
			Source.Stop();

			if (_playSoundCoroutine != null)
			{
				StopCoroutine(_playSoundCoroutine);
			}

			if (_fadeVolumeCoroutine != null)
			{
				StopCoroutine(_fadeVolumeCoroutine);
			}

			_pool?.Despawn(this);
		}

		/// <summary>
		/// Starts a coroutine that fades the volume of the audio from X to Y
		/// </summary>
		public void FadeVolume(float fromVolume, float toVolume, float fadeDuration,
		                       Action<AudioSourceMonoComponent> callbackFadeFinished = null)
		{
			if (_fadeVolumeCoroutine != null)
			{
				StopCoroutine(_fadeVolumeCoroutine);
			}

			_fadeVolumeCallback = callbackFadeFinished;
			_fadeVolumeCoroutine = StartCoroutine(FadeVolumeCoroutine(fromVolume, toVolume, fadeDuration));
		}

		private IEnumerator FadeVolumeCoroutine(float fromVolume, float toVolume, float fadeDuration)
		{
			var currentTimeProgress = 0f;

			while (currentTimeProgress < fadeDuration)
			{
				yield return null;

				currentTimeProgress += Time.deltaTime;

				var fadePercent = currentTimeProgress / fadeDuration;
				Source.volume = Mathf.Lerp(fromVolume, toVolume,
				                           fadePercent);
			}

			if (toVolume <= 0)
			{
				Source.Stop();
			}

			_fadeVolumeCallback?.Invoke(this);
			_fadeVolumeCallback = null;
		}

		private IEnumerator PlaySoundCoroutine()
		{
			Source.Play();

			do
			{
				yield return new WaitForSeconds(Source.clip.length);
			} while (!_canDespawn);

			_soundPlayedCallback?.Invoke(this);
			StopAndDespawn();
		}
	}

	/// <summary>
	/// This class contains initialization properties for AudioObject instances
	/// </summary>
	public struct AudioSourceInitData
	{
		public AudioMixerGroup MixerGroup;
		public AudioClip Clip;
		public float StartTime;
		public float SpatialBlend;
		public float Volume;
		public float Pitch;
		public bool Mute;
		public bool Loop;

		public AudioRolloffMode RolloffMode;
		public float MinDistance;
		public float MaxDistance;
	}

	/// <summary>
	/// This class stores audio clips, and contains crucial playback data for them
	/// </summary>
	public struct AudioClipPlaybackData
	{
		public List<AudioClip> AudioClips;
		public float MinVol;
		public float MaxVol;
		public float MinPitch;
		public float MaxPitch;

		public float PlaybackVolume => UnityEngine.Random.Range(MinVol, MaxVol);
		public float PlaybackPitch => UnityEngine.Random.Range(MinPitch, MaxPitch);
		public AudioClip PlaybackClip => AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];
	}

	/// <inheritdoc />
	public class AudioFxService<T> : IAudioFxInternalService<T> where T : struct, Enum
	{
		protected float _spatial3dThreshold;
		protected int _soundQueueBreakMs;
		
		protected readonly IDictionary<T, AudioClipPlaybackData> _audioClips = new Dictionary<T, AudioClipPlaybackData>();

		private readonly GameObject _audioPoolParent;
		private readonly IObjectPool<AudioSourceMonoComponent> _sfxPlayerPool;

		// Music sources
		private AudioSourceMonoComponent _activeMusicSource;
		private AudioSourceMonoComponent _transitionMusicSource;
		private Queue<AudioSourceMonoComponent> _soundQueue;

		// Audio mixer elements
		protected AudioMixer _audioMixer;
		protected Dictionary<string, AudioMixerGroup> _mixerGroups;
		protected Dictionary<string, AudioMixerSnapshot> _mixerSnapshots;

		private bool _sfxEnabled;
		private IAudioFxInternalService<T> _audioFxInternalServiceImplementation;

		/// <inheritdoc />
		public AudioListenerMonoComponent AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _activeMusicSource.Source.mute && _transitionMusicSource.Source.mute;
			set
			{
				_activeMusicSource.Source.mute = value;
				_transitionMusicSource.Source.mute = value;
			}
		}

		/// <inheritdoc />
		public bool IsSfxMuted
		{
			get => _sfxEnabled;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfxEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].Source.spatialBlend < _spatial3dThreshold)
					{
						audio[i].Source.mute = value;
					}
				}
			}
		}

		/// <inheritdoc />
		public bool IsMusicPlaying => _activeMusicSource.Source.isPlaying || _transitionMusicSource.Source.isPlaying;

		public AudioFxService(float spatial3DThreshold, int soundQueueBreakMs)
		{
			_mixerSnapshots = new Dictionary<string, AudioMixerSnapshot>();
			_mixerGroups = new Dictionary<string, AudioMixerGroup>();
			_soundQueue = new Queue<AudioSourceMonoComponent>();
			_audioPoolParent = new GameObject("Audio Container");

			_spatial3dThreshold = spatial3DThreshold;
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

			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListenerMonoComponent>();
			AudioListener.transform.SetParent(_audioPoolParent.transform);
			AudioListener.Listener = AudioListener.gameObject.AddComponent<AudioListener>();
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			var pool = new GameObjectPool<AudioSourceMonoComponent>(10, audioPlayer);

			pool.DespawnToSampleParent = true;
			_sfxPlayerPool = pool;
		}

		/// <inheritdoc />
		public virtual Task LoadAudioMixers(IEnumerable audioMixers)
		{
			return default;
		}

		/// <inheritdoc />
		public virtual Task LoadAudioClips(IEnumerable clips)
		{
			return default;
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
		public virtual AudioSourceMonoComponent PlayClip3D(T id, Vector3 worldPosition,
		                                                   AudioSourceInitData? sourceInitData = null)
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
		public virtual AudioSourceMonoComponent PlayClip2D(T id, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, Vector3.zero, sourceInitData);
			return source;
		}

		public virtual void PlayClipQueued2D(T id, string mixerGroupId, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}
			
			var source = _sfxPlayerPool.Spawn();
			
			// Will play immediately if there is no queued sounds, otherwise the queue will wait for current SFX to finish
			source.Play(_sfxPlayerPool, Vector3.zero, sourceInitData, ContinueSoundQueue, _soundQueue.Count > 0);
			_soundQueue.Enqueue(source);
		}
		
		private async void ContinueSoundQueue(AudioSourceMonoComponent audioSource)
		{
			if (audioSource != _soundQueue.Peek())
			{
				return;
			}

			_soundQueue.Dequeue();

			await Task.Delay(_soundQueueBreakMs);

			if (_soundQueue.Count > 0)
			{
				_soundQueue.Peek().StartPreparedPlayback();
			}
		}

		/// <inheritdoc />
		public virtual void PlayMusic(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                              bool continueFromCurrentTime = false,
		                              AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}

			if (_activeMusicSource.Source.isPlaying)
			{
				_activeMusicSource.FadeVolume(_activeMusicSource.Source.volume, 0, fadeOutDuration);
				_transitionMusicSource.Play(null, Vector3.zero, sourceInitData);
				_transitionMusicSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration, SwapMusicSources);
			}
			else
			{
				_activeMusicSource.Play(null, Vector3.zero, sourceInitData);
				_activeMusicSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration);
			}
		}

		private void SwapMusicSources(AudioSourceMonoComponent audioSource)
		{
			(_activeMusicSource, _transitionMusicSource) = (_transitionMusicSource, _activeMusicSource);
			_transitionMusicSource.Source.Stop();
		}

		/// <inheritdoc />
		public void StopMusic(float fadeOutDuration = 0f)
		{
			if (!_activeMusicSource.Source.isPlaying)
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

		/// <inheritdoc />
		public virtual AudioSourceInitData GetAudioInitProps(float spatialBlend, AudioClipPlaybackData playbackData)
		{
			return default;
		}

		/// <inheritdoc />
		public AudioMixerGroup GetAudioMixerGroup(string id)
		{
			var mixerGroup = _mixerGroups.TryGetValue(id, out AudioMixerGroup group);
			return group;
		}

		/// <inheritdoc />
		public float GetCurrentMusicPlaybackTime()
		{
			return _activeMusicSource.Source.time;
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