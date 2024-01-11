using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.News
{
	public class NewsScreenPresenter : UiToolkitPresenterData<NewsScreenPresenter.NewsScreenData>
	{
		public struct NewsScreenData
		{
			public Action OnBack;
		}

		private VisualElement _container;
		private VisualElement _loading;

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_container = root.Q("NewsContainer").Required();
			_loading = root.Q("Loading").Required();
			root.Q<ImageButton>("back").Required().clicked += Data.OnBack;
			root.Q<ImageButton>("home").Required().clicked += Data.OnBack;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			Fillnews().Forget();
		}

		private async UniTaskVoid Fillnews()
		{
			var news = await MainInstaller.ResolveServices().NewsService.GetNews();
			if (!IsOpen) return;
			if (!isActiveAndEnabled) return;
			foreach (var newsItem in news)
			{
				var view = new NewsItemElement();
				view.SetData(new TitleNews(newsItem));
				_container.Add(view);
			}
			_loading.SetDisplay(false);
		}
	}
}