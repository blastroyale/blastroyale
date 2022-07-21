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
		/// Request main game's <see cref="AudioListener"/>
		/// </summary>
		AudioListener AudioListener { get; }
		
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
		/// Detaches the current main <see cref="AudioListener"/> from it's current parent
		/// </summary>
		void DetachAudioListener();

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 3D surround in the given <paramref name="worldPosition"/>.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayClip3D(T id, Vector3 worldPosition, AudioPlayerInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayClip2D(T id, AudioPlayerInitData? sourceInitData = null);

		/// <summary>
		/// Plays the given <paramref name="id"/> music forever and replaces any old music currently playing.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayMusic(T id, AudioPlayerInitData? sourceInitData = null);
		
		/// <summary>
		/// Stops the music
		/// </summary>
		void StopMusic();

		/// <summary>
		/// Requests the default audio init properties, for a given spatial blend and volume multiplier
		/// </summary>
		AudioPlayerInitData GetDefaultAudioInitProps(float spatialBlend);
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
	/// A simple class wrapper for the <see cref="AudioSource"/> objects
	/// </summary>
	public class AudioPlayerMonoComponent : MonoBehaviour
	{
		public AudioSource AudioSource;
			
		private IObjectPool<AudioPlayerMonoComponent> _pool;

		/// <summary>
		/// Initialize the audio source of the object with relevant properties
		/// </summary>
		public void Init(AudioClip clip, float volumeMultiplier, Vector3? worldPos, AudioPlayerInitData? sourceInitData)
		{
			if (sourceInitData == null)
			{
				return;
			}
			
			AudioSource.clip = clip;
			AudioSource.volume = sourceInitData.Value.Volume * volumeMultiplier;
			AudioSource.spatialBlend = sourceInitData.Value.SpatialBlend;
			AudioSource.pitch = sourceInitData.Value.Pitch;
			AudioSource.time = sourceInitData.Value.StartTime;
			AudioSource.mute = sourceInitData.Value.Mute;
			AudioSource.loop = sourceInitData.Value.Loop;
			AudioSource.rolloffMode = sourceInitData.Value.RolloffMode;
			AudioSource.minDistance = sourceInitData.Value.MinDistance;
			AudioSource.maxDistance = sourceInitData.Value.MaxDistance;
			
			if (worldPos.HasValue)
			{
				transform.position = worldPos.Value;
			}
		}
		
		/// <summary>
		/// Marks this AudioFx to despawn back to the given <paramref name="pool"/> after the sound is completed
		/// </summary>
		public void StartTimeDespawner(IObjectPool<AudioPlayerMonoComponent> pool)
		{
			_pool = pool;
				
			StartCoroutine(Despawner());
		}

		private IEnumerator Despawner()
		{
			yield return new WaitForSeconds(AudioSource.clip.length);

			_pool.Despawn(this);
		}
	}

	/// <summary>
	/// This class contains initialization properties for AudioObject instances
	/// </summary>
	public struct AudioPlayerInitData
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
		private const float SPACIAL_3D_THRESHOLD = 0.2f;
		
		private readonly IDictionary<T, AudioClip> _audioClips = new Dictionary<T, AudioClip>();
		private readonly IObjectPool<AudioPlayerMonoComponent> _sfxPlayerPool;
		private readonly AudioPlayerMonoComponent _musicPlayer;
		protected readonly float _sfx2dVolumeMultiplier;
		protected readonly float _sfx3dVolumeMultiplier;
		protected readonly float _bgmVolumeMultiplier;
		
		private bool _sfx2dEnabled;
		private bool _sfx3dEnabled;

		/// <inheritdoc />
		public AudioListener AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _musicPlayer.AudioSource.mute;
			set => _musicPlayer.AudioSource.mute = value;
		}
		
		public bool Is2dSfxMuted
		{
			get => _sfx2dEnabled;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfx2dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].AudioSource.spatialBlend < SPACIAL_3D_THRESHOLD)
					{
						audio[i].AudioSource.mute = value;
					}
				}
			}
			
		}
		
		public bool Is3dSfxMuted 
		{
			get => _sfx3dEnabled;
			set
			{
				var audio = _sfxPlayerPool.SpawnedReadOnly;

				_sfx3dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].AudioSource.spatialBlend >= SPACIAL_3D_THRESHOLD)
					{
						audio[i].AudioSource.mute = value;
					}
				}
			}
		}

		public AudioFxService(float sfx2dVolumeMultiplier, float sfx3dVolumeMultiplier, float bgmVolumeMultiplier)
		{
			var container = new GameObject("Audio Container");
			var audioObject = new GameObject("Audio Source").AddComponent<AudioPlayerMonoComponent>();
			
			audioObject.AudioSource = audioObject.gameObject.AddComponent<AudioSource>();
			audioObject.AudioSource.playOnAwake = false;
			audioObject.gameObject.SetActive(false);
			audioObject.transform.SetParent(container.transform);
			
			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListener>();
			AudioListener.transform.SetParent(container.transform);
			AudioListener.enabled = false;
			
			_musicPlayer = new GameObject("Music Source").AddComponent<AudioPlayerMonoComponent>();
			_musicPlayer.AudioSource = _musicPlayer.gameObject.AddComponent<AudioSource>();
			_musicPlayer.AudioSource.playOnAwake = false;
			_musicPlayer.transform.SetParent(container.transform);
			
			_sfx2dVolumeMultiplier = sfx2dVolumeMultiplier;
			_sfx3dVolumeMultiplier = sfx3dVolumeMultiplier;
			_bgmVolumeMultiplier = bgmVolumeMultiplier;
			
			var pool = new GameObjectPool<AudioPlayerMonoComponent>(10, audioObject);

			pool.DespawnToSampleParent = true;
			_sfxPlayerPool = pool;
		}
		
		public virtual bool TryGetClip(T id, out AudioClip clip)
		{
			return _audioClips.TryGetValue(id, out clip);
		}
		
		public void DetachAudioListener()
		{
			AudioListener.transform.SetParent(_musicPlayer.transform.parent);
		}
		
		public void PlayClip3D(T id, Vector3 worldPosition, AudioPlayerInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}

			var source = _sfxPlayerPool.Spawn();
			source.Init(clip, _sfx3dVolumeMultiplier,worldPosition, sourceInitData);
			source.AudioSource.Play();
			source.StartTimeDespawner(_sfxPlayerPool);
		}
		
		public void PlayClip2D(T id, AudioPlayerInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}
			
			var source = _sfxPlayerPool.Spawn();
			source.Init(clip, _sfx2dVolumeMultiplier, Vector3.zero, sourceInitData);
			source.AudioSource.Play();
			source.StartTimeDespawner(_sfxPlayerPool);
		}
		
		public void PlayMusic(T id, AudioPlayerInitData? sourceInitData = null)
		{
			if (!TryGetClip(id, out var clip) || sourceInitData == null)
			{
				return;
			}
			
			Debug.LogError(sourceInitData.Value.Pitch);
			_musicPlayer.Init(clip, _bgmVolumeMultiplier, Vector3.zero, sourceInitData);
			_musicPlayer.AudioSource.Play();
		}
		
		public void StopMusic()
		{
			_musicPlayer.AudioSource.Stop();
		}

		public AudioPlayerInitData GetDefaultAudioInitProps(float spatialBlend)
		{
			return new AudioPlayerInitData()
			{
				SpatialBlend = spatialBlend,
				Pitch = 1f,
				Volume = 1f,
				Loop = false,
				Mute = false,
				StartTime = 0,
				RolloffMode = AudioRolloffMode.Linear,
				MinDistance = 0f,
				MaxDistance = 50f
			};
		}
		
		public void Add(T id, AudioClip clip)
		{
			_audioClips.Add(id, clip);
		}
		
		public void Remove(T id)
		{
			_audioClips.Remove(id);
		}
		
		public Dictionary<T, AudioClip> Clear()
		{
			var dic = new Dictionary<T, AudioClip>(_audioClips);
			
			_audioClips.Clear();

			return dic;
		}
		
		public void Dispose()
		{
			_audioClips.Clear();

			if (_musicPlayer != null && _musicPlayer.transform.parent.gameObject != null)
			{
				UnityEngine.Object.Destroy(_musicPlayer.transform.parent.gameObject);
			}
		}
	}
}