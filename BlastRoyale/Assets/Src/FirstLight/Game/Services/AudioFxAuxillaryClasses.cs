using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FirstLight.Services
{
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

		public Action<AudioSourceMonoComponent> FadeVolumeCallback;
		public Action<AudioSourceMonoComponent> SoundPlayedCallback;
		
		public string MixerGroupID { get; private set; }
		
		private IObjectPool<AudioSourceMonoComponent> _pool;
		private bool _canDespawn;
		private Coroutine _playSoundCoroutine;
		private Coroutine _fadeVolumeCoroutine;
		private Transform _followTarget;
		private Vector3 _followOffset;
		private float _pitchModPerLoop;

		private void Update()
		{
			if (_followTarget != null)
			{
				transform.position = _followTarget.position + _followOffset;
			}
		}

		/// <summary>
		/// Initialize the audio source of the object with relevant properties
		/// </summary>
		/// /// <remarks>Note: if initialized with Loop as true, the audio source must be despawned manually.</remarks>
		public void Play(IObjectPool<AudioSourceMonoComponent> pool,
		                 Vector3? worldPos, AudioSourceInitData? sourceInitData = null, bool prepareOnly = false)
		{
			if (sourceInitData == null)
			{
				return;
			}
			
			SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			_pool = pool;
			
			Source.outputAudioMixerGroup = sourceInitData.Value.MixerGroupAndId.Item1;
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
			_pitchModPerLoop = sourceInitData.Value.PitchModPerLoop;
			MixerGroupID = sourceInitData.Value.MixerGroupAndId.Item2;
			
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
			
			SetFollowTarget(null, Vector3.zero, Quaternion.identity);

			if (_playSoundCoroutine != null)
			{
				StopCoroutine(_playSoundCoroutine);
			}

			if (_fadeVolumeCoroutine != null)
			{
				StopCoroutine(_fadeVolumeCoroutine);
			}
			
			SoundPlayedCallback = null;
			FadeVolumeCallback = null;

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

			FadeVolumeCallback = callbackFadeFinished;
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

			FadeVolumeCallback?.Invoke(this);
			FadeVolumeCallback = null;
		}

		private IEnumerator PlaySoundCoroutine()
		{
			Source.Play();

			do
			{
				yield return new WaitForSeconds(Source.clip.length);

				Source.pitch += _pitchModPerLoop;
			} while (!_canDespawn);

			SoundPlayedCallback?.Invoke(this);
			StopAndDespawn();
		}
	}

	/// <summary>
	/// This class contains initialization properties for AudioObject instances
	/// </summary>
	public struct AudioSourceInitData
	{
		public Tuple<AudioMixerGroup,string> MixerGroupAndId;
		public AudioClip Clip;
		public float StartTime;
		public float SpatialBlend;
		public float Volume;
		public float Pitch;
		public bool Mute;
		public bool Loop;
		public bool RandomStartTime;
		public float PitchModPerLoop;

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
		public bool Loop;
		public bool RandomStartTime;
		public float MinVol;
		public float MaxVol;
		public float MinPitch;
		public float MaxPitch;
		public float PitchModPerLoop;
		
		public float PlaybackVolume => UnityEngine.Random.Range(MinVol, MaxVol);
		public float PlaybackPitch => UnityEngine.Random.Range(MinPitch, MaxPitch);
		public AudioClip PlaybackClip => AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];
		public float StartTime => RandomStartTime ? UnityEngine.Random.Range(0, PlaybackClip.length) : 0;
	}
}