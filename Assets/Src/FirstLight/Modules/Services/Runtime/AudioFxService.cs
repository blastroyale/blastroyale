using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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
		/// Load a set of audio clips into memory, and into the loaded clips collection
		/// </summary>
		/// <param name="clips">Enumerable collection of audio clips and their associated IDs</param>
		Task LoadAudioClips(IEnumerable clips);

		/// <summary>
		/// Load a single audio clip intoo memory, and into the loaded clips collection
		/// </summary>
		Task LoadAudioClip(T id);

		/// <summary>
		/// Unload a set of audio clips from memory, and remove al references from loaded clips collection
		/// </summary>
		/// <param name="clips">Enumerable collection of audio clips and their associated IDs</param>
		void UnloadAudioClips(IEnumerable clips);

		/// <summary>
		/// Unload a single audio clip from memory, and remove reference from loaded clips collection
		/// </summary>
		void UnloadAudioClip(T id);

		/// <summary>
		/// Tries to return the <see cref="AudioClip"/> mapped to the given <paramref name="id"/>.
		/// Returns true if the audio service currently has the <paramref name="clip"/> for the given <paramref name="id"/>.
		/// </summary>
		bool TryGetClip(T id, out AudioClip clip);

		/// <summary>
		/// Removes follow target from the current <see cref="AudioListenerMonoComponent"/> 
		/// </summary>
		void DetachAudioListener();

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 3D surround in the given <paramref name="worldPosition"/>.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		AudioSourceMonoComponent PlayClip3D(T id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		AudioSourceMonoComponent PlayClip2D(T id, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> music forever and replaces any old music currently playing.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayMusic(T id, float transitionDuration = 0f, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Stops the music
		/// </summary>
		void StopMusic();

		/// <summary>
		/// Requests the default audio init properties, for a given spatial blend and volume multiplier
		/// </summary>
		AudioSourceInitData GetDefaultAudioInitProps(float spatialBlend);
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
		void AddAudioClip(T id, AudioClip clip);

		/// <summary>
		/// Removes the given <paramref name="id"/>'s <see cref="AudioClip"/> from the service
		/// </summary>
		void RemoveAudioClip(T id);

		/// <summary>
		/// Requests the currently loaded audio clips
		/// </summary>
		List<T> GetLoadedAudioClips();
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
		private float _currentVolumeMultiplier;
		private Coroutine _playSoundCoroutine;
		private Transform _followTarget;
		private Vector3 _followOffset;

		/// <summary>
		/// Initialize the audio source of the object with relevant properties
		/// </summary>
		/// /// <remarks>Note: if initialized with Loop as true, the audio source must be despawned manually.</remarks>
		public void Play(IObjectPool<AudioSourceMonoComponent> pool, AudioClip clip, float volumeMultiplier,
		                 Vector3? worldPos, AudioSourceInitData? sourceInitData = null)
		{
			if (sourceInitData == null)
			{
				return;
			}

			_pool = pool;
			_currentVolumeMultiplier = volumeMultiplier;

			Source.clip = clip;
			Source.volume = sourceInitData.Value.Volume * volumeMultiplier;
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
		
		private void Update()
		{
			if (_followTarget != null)
			{
				transform.position = _followTarget.position + _followOffset;
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

		/// <summary>
		/// Starts a coroutine that fades the volume of the audio from X to Y
		/// </summary>
		public void FadeVolume(float fromVolume, float toVolume, float fadeDuration,
		                       Action<AudioSourceMonoComponent> callbackFadeFinished = null)
		{
			StartCoroutine(FadeVolumeCoroutine(fromVolume, toVolume, fadeDuration, callbackFadeFinished));
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
				_playSoundCoroutine = null;
			}

			_pool?.Despawn(this);
		}

		private IEnumerator FadeVolumeCoroutine(float fromVolume, float toVolume, float fadeDuration,
		                                        Action<AudioSourceMonoComponent> callbackFadeFinished = null)
		{
			float currentTimeProgress = 0;

			while (currentTimeProgress < fadeDuration)
			{
				yield return null;

				currentTimeProgress += Time.deltaTime;

				var fadePercent = currentTimeProgress / fadeDuration;
				Source.volume = Mathf.Lerp(fromVolume * _currentVolumeMultiplier, toVolume * _currentVolumeMultiplier,
				                           fadePercent);
			}

			callbackFadeFinished?.Invoke(this);
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

	/// <inheritdoc />
	public class AudioFxService<T> : IAudioFxInternalService<T> where T : struct, Enum
	{
		private const float SPATIAL_3D_THRESHOLD = 0.1f;

		private GameObject _audioPoolParent;
		private readonly IDictionary<T, AudioClip> _audioClips = new Dictionary<T, AudioClip>();
		private readonly IObjectPool<AudioSourceMonoComponent> _sfxPlayerPool;
		private readonly float _sfx2dVolumeMultiplier;
		private readonly float _sfx3dVolumeMultiplier;
		private readonly float _bgmVolumeMultiplier;
		private AudioSourceMonoComponent _activeMusicSource;
		private AudioSourceMonoComponent _transitionMusicSource;
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

		public AudioFxService(float sfx2dVolumeMultiplier, float sfx3dVolumeMultiplier, float bgmVolumeMultiplier)
		{
			_audioPoolParent = new GameObject("Audio Container");
			var audioPlayer = new GameObject("Audio Source").AddComponent<AudioSourceMonoComponent>();

			audioPlayer.Source = audioPlayer.gameObject.AddComponent<AudioSource>();
			audioPlayer.transform.SetParent(_audioPoolParent.transform);
			audioPlayer.Source.playOnAwake = false;
			audioPlayer.gameObject.SetActive(false);

			_activeMusicSource = new GameObject("Music Source").AddComponent<AudioSourceMonoComponent>();
			_activeMusicSource.transform.SetParent(_audioPoolParent.transform);
			_activeMusicSource.Source = _activeMusicSource.gameObject.AddComponent<AudioSource>();
			_activeMusicSource.Source.playOnAwake = false;

			_transitionMusicSource = new GameObject("Music Transition Source").AddComponent<AudioSourceMonoComponent>();
			_transitionMusicSource.transform.SetParent(_audioPoolParent.transform);
			_transitionMusicSource.Source = _transitionMusicSource.gameObject.AddComponent<AudioSource>();
			_transitionMusicSource.Source.playOnAwake = false;

			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListenerMonoComponent>();
			AudioListener.transform.SetParent(_audioPoolParent.transform);
			AudioListener.Listener = AudioListener.gameObject.AddComponent<AudioListener>();
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			_sfx2dVolumeMultiplier = sfx2dVolumeMultiplier;
			_sfx3dVolumeMultiplier = sfx3dVolumeMultiplier;
			_bgmVolumeMultiplier = bgmVolumeMultiplier;

			var pool = new GameObjectPool<AudioSourceMonoComponent>(10, audioPlayer);

			pool.DespawnToSampleParent = true;
			_sfxPlayerPool = pool;
		}

		public virtual Task LoadAudioClips(IEnumerable clips)
		{
			return default;
		}

		public virtual Task LoadAudioClip(T id)
		{
			return default;
		}

		public virtual void UnloadAudioClips(IEnumerable clips)
		{
		}

		public virtual void UnloadAudioClip(T id)
		{
		}

		/// <inheritdoc />
		public virtual bool TryGetClip(T id, out AudioClip clip)
		{
			if (!_audioClips.TryGetValue(id, out clip))
			{
				throw new
					MissingMemberException($"The {nameof(AudioFxService<T>)} does not have an audio clip with ID " +
					                       $"'{nameof(id)}' loaded in memory for playback. ");
			}

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
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, clip, _sfx3dVolumeMultiplier, worldPosition, sourceInitData);
			return source;
		}

		/// <inheritdoc />
		public virtual AudioSourceMonoComponent PlayClip2D(T id, AudioSourceInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return null;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, clip, _sfx2dVolumeMultiplier, Vector3.zero, sourceInitData);
			return source;
		}

		/// <inheritdoc />
		public virtual void PlayMusic(T id, float transitionDuration = 0f, AudioSourceInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}

			if (_activeMusicSource.Source.isPlaying)
			{
				_activeMusicSource.FadeVolume(_activeMusicSource.Source.volume, 0, transitionDuration,
				                              CallbackAudioFadeFinished);
				_transitionMusicSource.Play(null, clip, _bgmVolumeMultiplier, Vector3.zero, sourceInitData);
				_transitionMusicSource.FadeVolume(0, sourceInitData.Value.Volume, transitionDuration,
				                                  CallbackAudioFadeFinished);
			}
			else
			{
				_activeMusicSource.Play(null, clip, _bgmVolumeMultiplier, Vector3.zero, sourceInitData);
			}
		}

		private void CallbackAudioFadeFinished(AudioSourceMonoComponent audioSource)
		{
			if (audioSource == _transitionMusicSource)
			{
				(_activeMusicSource, _transitionMusicSource) = (_transitionMusicSource, _activeMusicSource);
				_transitionMusicSource.StopAndDespawn();
				// TODO - UNLOAD MUSIC THAT WAS TRANSITIONED FROM
			}
		}

		/// <inheritdoc />
		public void StopMusic()
		{
			_activeMusicSource.Source.Stop();
		}

		/// <inheritdoc />
		public virtual AudioSourceInitData GetDefaultAudioInitProps(float spatialBlend)
		{
			return default;
		}

		/// <inheritdoc />
		public void AddAudioClip(T id, AudioClip clip)
		{
			_audioClips.Add(id, clip);
		}

		/// <inheritdoc />
		public void RemoveAudioClip(T id)
		{
			_audioClips.Remove(id);
		}

		/// <inheritdoc />
		public List<T> GetLoadedAudioClips()
		{
			return _audioClips.Keys.ToList();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_audioClips.Clear();

			if (_activeMusicSource != null && _activeMusicSource.transform.parent.gameObject != null)
			{
				UnityEngine.Object.Destroy(_activeMusicSource.transform.parent.gameObject);
			}
		}
	}
}