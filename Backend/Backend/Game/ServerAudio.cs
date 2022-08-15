using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Services;
using UnityEngine;

namespace Backend.Game;

/// <summary>
/// Just need this.
/// TODO: Abstract this so we don't need to implement this in server.
/// </summary>
public class ServerAudio : IAudioFxService<AudioId>
{
	public void Dispose()
	{
	}

	public Task LoadAudioMixers(IEnumerable audioMixers)
	{
		return default;
	}

	public Task LoadAudioClips(IEnumerable clips) => null;

	public void UnloadAudioClips(IEnumerable clips)
	{
	}

	public void TransitionAudioMixer(string snapshotName, float transitionDuration)
	{
		throw new System.NotImplementedException();
	}

	public bool TryGetClipPlaybackData(AudioId id, out AudioClipPlaybackData clip)
	{
		clip = default;
		return true;
	}

	public void DetachAudioListener()
	{
	}

	public AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition, AudioSourceInitData? sourceInitData = null,
	                                           Action<AudioSourceMonoComponent> soundPlayedCallback = null, string mixerGroupOverride = null)
	{
		return null;
	}

	public AudioSourceMonoComponent PlayClip2D(AudioId id, AudioSourceInitData? sourceInitData = null,
	                                           Action<AudioSourceMonoComponent> soundPlayedCallback = null, string mixerGroupOverride = null)
	{
		return null;
	}

	public void PlayClipQueued2D(AudioId id, string mixerGroupId, AudioSourceInitData? sourceInitData = null)
	{
	}

	public void PlayMusic(AudioId id, float fadeInDuration = 0, float fadeOutDuration = 0,
	                      bool continueFromCurrentTime = false,
	                      AudioSourceInitData? sourceInitData = null)
	{
	}

	public void StopMusic(float fadeOutDuration = 0)
	{
	}

	public float GetCurrentMusicPlaybackTime()
	{
		return 0f;
	}

	public AudioListenerMonoComponent AudioListener { get; }

	public bool IsBgmMuted
	{
		get => true;
		set  { }
	}

	public bool IsSfxMuted { get; set; }
	public bool IsMusicPlaying { get; }

	public bool Is2dSfxMuted
	{
		get => true;
		set { }
	}

	public bool Is3dSfxMuted
	{
		get => true;
		set { }
	}
}