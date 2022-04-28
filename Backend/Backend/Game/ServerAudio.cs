
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

	public bool TryGetClip(AudioId id, out AudioClip clip)
	{
		clip = null;
		return false;
	}

	public void DetachAudioListener()
	{
	
	}

	public void PlayClip3D(AudioId id, Vector3 worldPosition, float delay)
	{
		
	}
	
	public void PlayClip3D(AudioId id, Vector3 worldPosition)
	{
		
	}

	public void PlayClip2D(AudioId id, float delay = 0)
	{
		
	}

	public void PlayMusic(AudioId id, float delay = 0)
	{
		
	}

	public void StopMusic()
	{
		
	}

	public AudioListener AudioListener => null;

	public bool IsBgmMuted
	{
		get => true;
		set  { }
	}

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