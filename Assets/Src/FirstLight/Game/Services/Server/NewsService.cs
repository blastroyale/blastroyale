using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
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
		
		public PlayfabNewsService()
		{
			_newsLocalData = new DataService();
			_data = _newsLocalData.LoadData<NewsData>();
		}

		public async UniTask<List<TitleNewsItem>> GetNews()
		{
			var response = await AsyncPlayfabAPI.GetNews(new GetTitleNewsRequest()
			{
				Count = 10,
			});
			var latest = response.News.First();
			_data.LastNewsId = latest.NewsId;
			_newsLocalData.SaveData<NewsData>();
			return response.News;
		}
		
		public async UniTask<bool> HasNotSeenNews()
		{
			var response = await AsyncPlayfabAPI.GetNews(new GetTitleNewsRequest()
			{
				Count = 1,
			});
			if (response.News.Count == 0) return false;
			var latest = response.News.First();
			return _data.LastNewsId != latest.NewsId;
		}
	}
}