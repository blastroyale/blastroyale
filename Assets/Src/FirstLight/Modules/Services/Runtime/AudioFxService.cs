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
		void PlayClip3D(T id, AudioObject initProps, Vector3 worldPosition, float delay = 0f);

		/// <summary>
		/// Plays the given <paramref name="id"/> sound clip in 2D mono sound.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayClip2D(T id, AudioObject initProps, float delay = 0f);

		/// <summary>
		/// Plays the given <paramref name="id"/> music forever and replaces any old music currently playing.
		/// Returns true if successfully has the audio to play.
		/// </summary>
		void PlayMusic(T id, float delay = 0f);
		
		/// <summary>
		/// Stops the music
		/// </summary>
		void StopMusic();
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
	public class AudioObject : MonoBehaviour
	{
		public AudioSource AudioSource;
			
		private IObjectPool<AudioObject> _pool;

		/// <summary>
		/// Marks this AudioFx to despawn back to the given <paramref name="pool"/> after the sound is completed
		/// </summary>
		public void StartTimeDespawner(IObjectPool<AudioObject> pool)
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
	/// Class that contains initialization properties for AudioObject instances
	/// </summary>
	public struct AudioIniitProps
	{
		public float Volume;
		public float Pitch;
	}
		
	/// <inheritdoc />
	public class AudioFxService<T> : IAudioFxInternalService<T> where T : struct, Enum
	{
		private const float Sound3dSpacialThreshold = 0.2f;
		
		private readonly IDictionary<T, AudioClip> _audioClips = new Dictionary<T, AudioClip>();
		private readonly IObjectPool<AudioObject> _pool;
		private readonly AudioSource _musicSource;
		private readonly float _sfx2dVolume;
		private readonly float _sfx3dVolume;
		
		private bool _sfx2dEnabled;
		private bool _sfx3dEnabled;

		/// <inheritdoc />
		public AudioListener AudioListener { get; }

		/// <inheritdoc />
		public bool IsBgmMuted
		{
			get => _musicSource.mute;
			set => _musicSource.mute = value;
		}

		/// <inheritdoc />
		public bool Is2dSfxMuted
		{
			get => _sfx2dEnabled;
			set
			{
				var audio = _pool.SpawnedReadOnly;

				_sfx2dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].AudioSource.spatialBlend < Sound3dSpacialThreshold)
					{
						audio[i].AudioSource.mute = value;
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
				var audio = _pool.SpawnedReadOnly;

				_sfx3dEnabled = value;

				for (var i = 0; i < audio.Count; i++)
				{
					if (audio[i].AudioSource.spatialBlend >= Sound3dSpacialThreshold)
					{
						audio[i].AudioSource.mute = value;
					}
				}
			}
		}

		public AudioFxService(float sfx2dVolume, float sfx3dVolume, float bgmVolume)
		{
			var container = new GameObject("Audio Container");
			var audioObject = new GameObject("Audio Source").AddComponent<AudioObject>();

			audioObject.AudioSource = audioObject.gameObject.AddComponent<AudioSource>();
			audioObject.AudioSource.loop = false;
			audioObject.AudioSource.playOnAwake = false;
			AudioListener = new GameObject("Audio Listener").AddComponent<AudioListener>();
			_musicSource = new GameObject("Music Source").AddComponent<AudioSource>();
			AudioListener.enabled = false;
			_musicSource.playOnAwake = false;
			_musicSource.loop = true;
			_musicSource.volume = bgmVolume;
			_sfx2dVolume = sfx2dVolume;
			_sfx3dVolume = sfx3dVolume;
			
			audioObject.gameObject.SetActive(false);
			audioObject.transform.SetParent(container.transform);
			AudioListener.transform.SetParent(container.transform);
			_musicSource.transform.SetParent(container.transform);

			var pool = new GameObjectPool<AudioObject>(10, audioObject);

			pool.DespawnToSampleParent = true;
			_pool = pool;
		}

		/// <inheritdoc />
		public virtual bool TryGetClip(T id, out AudioClip clip)
		{
			return _audioClips.TryGetValue(id, out clip);
		}

		/// <inheritdoc />
		public void DetachAudioListener()
		{
			AudioListener.transform.SetParent(_musicSource.transform.parent);
		}

		/// <inheritdoc />
		public void PlayClip3D(T id, AudioObject initProps, Vector3 worldPosition, float delay = 0f)
		{
			if (!TryGetClip(id, out var clip))
			{
				return;
			}
			
			var source = _pool.Spawn();

			source.AudioSource.time = delay;
			source.AudioSource.clip = clip;
			source.AudioSource.spatialBlend = 1f;
			source.AudioSource.loop = false;
			source.transform.position = worldPosition;
			source.AudioSource.volume = _sfx3dVolume;
			source.AudioSource.mute = Is3dSfxMuted;

			source.AudioSource.Play();
			source.StartTimeDespawner(_pool);
		}

		/// <inheritdoc />
		public void PlayClip2D(T id, AudioObject initProps, float delay = 0f)
		{
			if (!TryGetClip(id, out var clip))
			{
				return;
			}
			
			var source = _pool.Spawn();

			source.AudioSource.time = delay;
			source.AudioSource.clip = clip;
			source.AudioSource.loop = false;
			source.AudioSource.spatialBlend = 0f;
			source.AudioSource.volume = _sfx2dVolume;
			source.AudioSource.mute = Is2dSfxMuted;

			source.AudioSource.Play();
			source.StartTimeDespawner(_pool);
		}

		/// <inheritdoc />
		public void PlayMusic(T id, float delay = 0f)
		{
			if (!TryGetClip(id, out var clip))
			{
				return;
			}
			
			_musicSource.time = delay;
			_musicSource.clip = clip;
			_musicSource.Play();
		}

		/// <inheritdoc />
		public void StopMusic()
		{
			_musicSource.Stop();
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