using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This service allows to manage multiple <see cref="T"/> of the defined <typeparamref name="T"/> enum type.
	/// </summary>
	public interface IAudioFxService<in T> : IDisposable where T : struct, Enum
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
		void PlayClip3D(T id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayClip2D(T id, AudioSourceInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> music forever and replaces any old music currently playing.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayMusic(T id, AudioSourceInitData? sourceInitData = null);

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
		void Add(T id, AudioClip clip);

		/// <summary>
		/// Removes the given <paramref name="id"/>'s <see cref="AudioClip"/> from the service
		/// </summary>
		void Remove(T id);

		/// <summary>
		/// Clears the container of clips currently held by this service and returns a copy of clips cleared.
		/// </summary>
		Dictionary<T, AudioClip> Clear();
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
		private Coroutine _coroutine;

		/// <summary>
		/// Initialize the audio source of the object with relevant properties
		/// </summary>
		/// /// <remarks>Note: if initialized with Loop as true, the audio source must be despawned manually.</remarks>
		public void Play(IObjectPool<AudioSourceMonoComponent> pool, AudioClip clip, float volumeMultiplier,
		                 Vector3? worldPos, AudioSourceInitData? sourceInitData)
		{
			if (sourceInitData == null)
			{
				return;
			}

			_pool = pool;

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

			_coroutine = StartCoroutine(PlaySoundSequence());
		}

		/// <summary>
		/// Flags this audio source to despawn at the end of the current clip playback
		/// </summary>
		public void FlagForDespawn()
		{
			_canDespawn = true;
		}

		/// <summary>
		/// Despawns this audio source immediately
		/// </summary>
		public void Despawn()
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			_pool.Despawn(this);
		}

		private IEnumerator PlaySoundSequence()
		{
			Source.Play();

			do
			{
				yield return new WaitForSeconds(Source.clip.length);
			} while (!_canDespawn);

			_pool.Despawn(this);
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

		private readonly IDictionary<T, AudioClip> _audioClips = new Dictionary<T, AudioClip>();
		private readonly IObjectPool<AudioSourceMonoComponent> _sfxPlayerPool;
		private readonly AudioSourceMonoComponent _musicSource;

		protected readonly float _sfx2dVolumeMultiplier;
		protected readonly float _sfx3dVolumeMultiplier;
		protected readonly float _bgmVolumeMultiplier;

		private bool _sfx2dEnabled;
		private bool _sfx3dEnabled;

		/// <inheritdoc />
		public AudioListenerMonoComponent AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _musicSource.Source.mute;
			set => _musicSource.Source.mute = value;
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
			var container = new GameObject("Audio Container");
			var audioPlayer = new GameObject("Audio Source").AddComponent<AudioSourceMonoComponent>();

			audioPlayer.Source = audioPlayer.gameObject.AddComponent<AudioSource>();
			audioPlayer.transform.SetParent(container.transform);
			audioPlayer.Source.playOnAwake = false;
			audioPlayer.gameObject.SetActive(false);

			_musicSource = new GameObject("Music Source").AddComponent<AudioSourceMonoComponent>();
			_musicSource.transform.SetParent(container.transform);
			_musicSource.Source = _musicSource.gameObject.AddComponent<AudioSource>();
			_musicSource.Source.playOnAwake = false;

			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListenerMonoComponent>();
			AudioListener.transform.SetParent(container.transform);
			AudioListener.Listener = AudioListener.gameObject.AddComponent<AudioListener>();
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			_sfx2dVolumeMultiplier = sfx2dVolumeMultiplier;
			_sfx3dVolumeMultiplier = sfx3dVolumeMultiplier;
			_bgmVolumeMultiplier = bgmVolumeMultiplier;

			var pool = new GameObjectPool<AudioSourceMonoComponent>(10, audioPlayer);

			pool.DespawnToSampleParent = true;
			_sfxPlayerPool = pool;
		}

		/// <inheritdoc />
		public virtual bool TryGetClip(T id, out AudioClip clip)
		{
			return _audioClips.TryGetValue(id, out clip);
		}

		/// <inheritdoc />
		public void DetachAudioListener()
		{
			AudioListener.SetFollowTarget(null, Vector3.zero, Quaternion.identity);
		}

		/// <inheritdoc />
		public void PlayClip3D(T id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, clip, _sfx3dVolumeMultiplier, worldPosition, sourceInitData);
		}

		/// <inheritdoc />
		public void PlayClip2D(T id, AudioSourceInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Play(_sfxPlayerPool, clip, _sfx2dVolumeMultiplier, Vector3.zero, sourceInitData);
		}

		/// <inheritdoc />
		public void PlayMusic(T id, AudioSourceInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}

			_musicSource.Play(_sfxPlayerPool, clip, _bgmVolumeMultiplier, Vector3.zero, sourceInitData);
		}

		/// <inheritdoc />
		public void StopMusic()
		{
			_musicSource.Source.Stop();
		}

		/// <inheritdoc />
		public virtual AudioSourceInitData GetDefaultAudioInitProps(float spatialBlend)
		{
			return default;
		}

		/// <inheritdoc />
		public void Add(T id, AudioClip clip)
		{
			_audioClips.Add(id, clip);
		}

		/// <inheritdoc />
		public void Remove(T id)
		{
			_audioClips.Remove(id);
		}

		/// <inheritdoc />
		public Dictionary<T, AudioClip> Clear()
		{
			var dic = new Dictionary<T, AudioClip>(_audioClips);

			_audioClips.Clear();

			return dic;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_audioClips.Clear();

			if (_musicSource != null && _musicSource.transform.parent.gameObject != null)
			{
				UnityEngine.Object.Destroy(_musicSource.transform.parent.gameObject);
			}
		}
	}
}