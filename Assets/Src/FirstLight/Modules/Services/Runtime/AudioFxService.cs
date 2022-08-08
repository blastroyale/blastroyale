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
		bool Is2dSfxMuted { get; set; }

		/// <summary>
		/// Sets the 3D Sfx muting state across the game
		/// </summary>
		bool Is3dSfxMuted { get; set; }

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
		public void Play(IObjectPool<AudioSourceMonoComponent> pool, AudioMixerGroup mixerGroup,
		                 Vector3? worldPos, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}

			_pool = pool;
			
			Source.outputAudioMixerGroup = mixerGroup;
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

			StopAndDespawn();
		}
	}

	/// <summary>
	/// This class contains initialization properties for AudioObject instances
	/// </summary>
	public struct AudioSourceInitData
	{
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
		private const float SPATIAL_3D_THRESHOLD = 0.1f;

		protected readonly IDictionary<T, AudioClipPlaybackData> _audioClips =
			new Dictionary<T, AudioClipPlaybackData>();

		private readonly GameObject _audioPoolParent;
		private readonly IObjectPool<AudioSourceMonoComponent> _sfxPlayerPool;
		private AudioSourceMonoComponent _activeMusicSource;
		private AudioSourceMonoComponent _transitionMusicSource;
		protected AudioMixer _audioMixer;
		protected AudioMixerGroup _2dMixerGroup;
		protected AudioMixerGroup _3dMixerGroup;
		protected AudioMixerGroup _bgmMixerGroup;
		protected AudioMixerGroup _ancrMixerGroup;
		protected AudioMixerGroup _ambMixerGroup;
		private bool _sfx2dEnabled;
		private bool _sfx3dEnabled;

		/// <inheritdoc />
		public AudioListenerMonoComponent AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _activeMusicSource.Source.mute;
			set => _activeMusicSource.Source.mute = value;
		}

		/// <inheritdoc />
		public bool Is2dSfxMuted
		{
			get => _sfx2dEnabled;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfx2dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].Source.spatialBlend < SPATIAL_3D_THRESHOLD)
					{
						audio[i].Source.mute = value;
					}
				}
			}
		}

		/// <inheritdoc />
		public bool Is3dSfxMuted
		{
			get => _sfx3dEnabled;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfx3dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].Source.spatialBlend >= SPATIAL_3D_THRESHOLD)
					{
						audio[i].Source.mute = value;
					}
				}
			}
		}

		public AudioFxService()
		{
			_audioPoolParent = new GameObject("Audio Container");
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
			if (sourceInitData == null || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, _3dMixerGroup, worldPosition, sourceInitData);
			return source;
		}

		/// <inheritdoc />
		public virtual AudioSourceMonoComponent PlayClip2D(T id, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null || !TryGetClipPlaybackData(id, out var clipData))
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, _2dMixerGroup, Vector3.zero, sourceInitData);
			return source;
		}

		/// <inheritdoc />
		public virtual void PlayMusic(T id, float fadeInDuration = 0f, float fadeOutDuration = 0f,
		                              bool continueFromCurrentTime = false,
		                              AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null || !TryGetClipPlaybackData(id, out var clipData))
			{
				return;
			}

			if (_activeMusicSource.Source.isPlaying)
			{
				_activeMusicSource.FadeVolume(_activeMusicSource.Source.volume, 0, fadeOutDuration);
				_transitionMusicSource.Play(null, _bgmMixerGroup, Vector3.zero, sourceInitData);
				_transitionMusicSource.FadeVolume(0, sourceInitData.Value.Volume, fadeInDuration, SwapMusicSources);
			}
			else
			{
				_activeMusicSource.Play(null, _bgmMixerGroup, Vector3.zero, sourceInitData);
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
		public virtual AudioSourceInitData GetAudioInitProps(float spatialBlend,
		                                                            AudioClipPlaybackData playbackData)
		{
			return default;
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