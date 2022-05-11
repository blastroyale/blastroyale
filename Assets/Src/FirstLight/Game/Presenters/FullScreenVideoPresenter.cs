using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the full screen video playback.
	/// </summary>
	public class FullScreenVideoPresenter : UiPresenterData<FullScreenVideoPresenter.VideoData>
	{
		public struct VideoData
		{
			public Action OnVideoCompleted;
			public string VideoAddress;
		}
		
		[SerializeField, Required] private Button _skipButton;
		[SerializeField, Required] private VideoPlayer _videoPlayer;

		private IGameServices _gameServices;
		private Coroutine _coroutine;
		private Coroutine _skipCoroutine;

		private void Awake()
		{
			_skipButton.onClick.AddListener(Skip);
		}

		protected override void OnInitialized()
		{
			_gameServices = MainInstaller.Resolve<IGameServices>();
		}

		protected override async void OnOpened()
		{
			var clip = await _gameServices.AssetResolverService.LoadAssetAsync<VideoClip>(Data.VideoAddress);
			
			if (this.IsDestroyed())
			{
				return;
			}

			_skipButton.enabled = Debug.isDebugBuild;
			_skipCoroutine = StartCoroutine(TimeUpdateCoroutine());
			_coroutine = StartCoroutine(PlayVideoCoroutine(clip));
		}

		private void Skip()
		{
			if (_skipCoroutine != null)
			{
				StopCoroutine(_skipCoroutine);
				_skipCoroutine = null;
			}
			
			if(_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			
			_videoPlayer.Stop();
			_gameServices.AssetResolverService.UnloadAsset(_videoPlayer.clip);
			Data.OnVideoCompleted();
		}
		
		private IEnumerator TimeUpdateCoroutine()
		{
			yield return new WaitForSeconds(10);

			_skipButton.enabled = true;
		}

		
		private IEnumerator PlayVideoCoroutine(VideoClip clip)
		{
			_videoPlayer.clip = clip;
			
			_videoPlayer.Play();
			
			yield return new WaitForSeconds((float) clip.length);
			
			_gameServices.AssetResolverService.UnloadAsset(_videoPlayer.clip);
			Data.OnVideoCompleted();
			
			_coroutine = null;
		}
	}
}
	