using System.Collections;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Services;
using UnityEngine;

namespace Backend.Game
{
	/// <summary>
    /// Just need this.
    /// TODO: Abstract this so we don't need to implement this in server.
    /// </summary>
    public class ServerAudio : IAudioFxService<AudioId>
    {
    	public void Dispose()
    	{
    	}
    
    	public UniTask LoadAudioMixers(IEnumerable audioMixers)
    	{
    		return default;
    	}
    
    	public UniTask LoadAudioClips(IEnumerable clips) => UniTask.CompletedTask;
    
    	public void UnloadAudioClips(IEnumerable clips)
    	{
    	}
    
    	public void TransitionAudioMixer(string snapshotName, float transitionDuration)
    	{
    	}
    
    	public bool TryGetClipPlaybackData(AudioId id, out AudioClipPlaybackData clip)
    	{
    		clip = default;
    		return true;
    	}
    
    	public void DetachAudioListener()
    	{
    	}
    
    	public AudioSourceMonoComponent PlayClip3D(AudioId id, Vector3 worldPosition, string mixerGroupOverride = null)
    	{
    		return default;
    	}
    
    	public AudioSourceMonoComponent PlayClip2D(AudioId id, string mixerGroupOverride = null)
    	{
    		return default;
    	}

        public void PlayClipQueued2D(AudioId id, string mixerGroupOverride = null)
        {
        }

        public void PlayMusic(AudioId id, float fadeInDuration = 0, float fadeOutDuration = 0, bool continueFromCurrentTime = false)
    	{
    	}

		public void PlayAmbience(AudioId id, float fadeInDuration = 0, float fadeOutDuration = 0, bool continueFromCurrentTime = false)
		{
			
		}

		public void PlaySequentialMusicTransition(AudioId transitionClip, AudioId musicClip)
    	{
    	}
    
    	public void StopMusic(float fadeOutDuration = 0)
    	{
    	}

		public void StopAmbience(float fadeOutDuration = 0)
		{
			
		}

		public void StopAllSfx()
    	{
        }

        public void WipeSoundQueue()
        {
        }

        public float GetCurrentMusicPlaybackTime()
    	{
    		return 0f;
    	}
    
    	public AudioListenerMonoComponent AudioListener { get; }
    
    	public bool IsBgmMuted { get; set; }
		public bool IsAmbienceMuted { get; set; }
		public bool IsSfxMuted { get; set; }
        public bool IsDialogueMuted { get; set; }
        public bool IsMusicPlaying { get; }
		public bool IsAmbiencePlaying { get; }
	}
}

