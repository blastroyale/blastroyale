using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using PlayFab.ClientModels;


namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles game news that are driven by server
	/// </summary>
	public interface INewsService
	{
		/// <summary>
		/// Gets the latest news
		/// </summary>
		UniTask<List<TitleNewsItem>> GetNews();

		/// <summary>
		/// Checks if there's any news the player never seen
		/// </summary>
		UniTask<bool> HasNotSeenNews();
	}
	
	public class PlayfabNewsService : INewsService
	{
		private DataService _newsLocalData;
		private NewsData _data;
		private List<TitleNewsItem> _news;
		private UniTask _fetchingTask;
		private DateTime _lastFetch;
		
		public PlayfabNewsService(IMessageBrokerService service)
		{
			_newsLocalData = new DataService();
			_data = _newsLocalData.LoadData<NewsData>();
			service.Subscribe<SuccessAuthentication>(OnAuth);
			service.Subscribe<MainMenuOpenedMessage>(OnMenuOpen);
		}

		private void OnMenuOpen(MainMenuOpenedMessage msg)
		{
			if (_fetchingTask.Status == UniTaskStatus.Pending) return;

			if (_fetchingTask.Status == UniTaskStatus.Succeeded)
			{
				var timeSinceLast = (DateTime.UtcNow - _lastFetch).TotalMinutes;
				if (timeSinceLast <= 1) return; // max 1 per minute
			}
	
			_fetchingTask = FetchNews();
		}
		
		private void OnAuth(SuccessAuthentication msg)
		{
			_fetchingTask = FetchNews();
		}
		
		private async UniTask<List<TitleNewsItem>> FetchNews()
		{
			FLog.Info("Fetching All Game News");
			var response = await AsyncPlayfabAPI.GetNews(new GetTitleNewsRequest()
			{
				Count = 30,
			});
			FLog.Verbose("News",$"News received: {ModelSerializer.Serialize(response.News).Value}");
			_news = response.News;
			_lastFetch = DateTime.UtcNow;
			return _news;
		}

		public async UniTask<List<TitleNewsItem>> GetNews()
		{ ;
			await WaitPendingFetch();
			var latest = _news.First();
			_data.LastNewsId = latest.NewsId;
			_newsLocalData.SaveData<NewsData>();
			return _news;
		}
		
		public async UniTask<bool> HasNotSeenNews()
		{
			FLog.Info("News", "Checking if has unseen news");
			await WaitPendingFetch();
			if (_news == null)
			{
				return false;
			}
			var latest = _news.First();
			return _data.LastNewsId != latest.NewsId;
		}
		
		private bool FinishedPendingFetch() => _fetchingTask.Status != UniTaskStatus.Pending;
		private async UniTask WaitPendingFetch() => await UniTask.WaitUntil(FinishedPendingFetch);

	}
}