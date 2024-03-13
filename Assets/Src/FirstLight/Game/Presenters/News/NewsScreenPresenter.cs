using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
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

		private const string USS_CATEGORY_BUTTON = "category-button";
		private const string USS_CATEGORY_BUTTON_HIGHLIGHT = "category-button--highlight";
		private const string USS_CATEGORY_INDICATOR = "category-button__indicator";
		
		private VisualElement _categoriesContainer;
		private VisualElement _container;
		private VisualElement _loading;
		private List<TitleNews> _allNews;
		private Dictionary<string, Button> _categories;
		private VisualElement _indicator;
		private string _selectedCategory = "Game";
		
		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_container = root.Q("NewsContainer").Required();
			_loading = root.Q("Loading").Required();
			_categoriesContainer = root.Q("Categories");
			_indicator = new VisualElement();
			_indicator.AddToClassList(USS_CATEGORY_INDICATOR);
			root.Q<ImageButton>("back").Required().clicked += Data.OnBack;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			_loading.SetDisplay(true);
			Fillnews().ContinueWith(OnFilledNews).Forget();
		}

		private void OnFilledNews()
		{
			var cat = _categories.First();
			OnClickCategory(cat.Key);
			_loading.SetDisplay(false);
		}

		private void DrawNews()
		{
			_container.Clear();
			var latest = _allNews.First();
			foreach (var newsItem in _allNews)
			{
				if (newsItem.Data.Category != _selectedCategory) continue;
				var view = new NewsItemElement();
				view.SetData(newsItem);
				view.SetHot(newsItem == latest);
				_container.Add(view);
				if (newsItem == latest)
				{
					view.AnimatePing(1.1f, 100);
				}
			}
		}
		
		private void OnClickCategory(string category)
		{
			if (_categories.TryGetValue(_selectedCategory, out var oldBtn))
			{
				oldBtn.RemoveFromClassList(USS_CATEGORY_BUTTON_HIGHLIGHT);
			}
			if (_categories.TryGetValue(category, out var newBtn))
			{
				newBtn.AddToClassList(USS_CATEGORY_BUTTON_HIGHLIGHT);
				newBtn.Add(_indicator);
			}
			_selectedCategory = category;
			FLog.Verbose("Clicked to view category "+category);
			DrawNews();
		}

		private async UniTask Fillnews()
		{
			_allNews = new List<TitleNews>();
			_categories = new Dictionary<string, Button>();
			var news = await MainInstaller.ResolveServices().NewsService.GetNews();
			if (!IsOpen) return;
			if (!isActiveAndEnabled) return;
			foreach (var newsItem in news)
			{
				var newsData = new TitleNews(newsItem);
				_allNews.Add(newsData);
				if (!_categories.ContainsKey(newsData.Data.Category))
				{
					var button = new Button();
					button.AddToClassList(USS_CATEGORY_BUTTON);
					button.text = newsData.Data.Category;
					button.clicked += () => OnClickCategory(newsData.Data.Category);
					_categories[newsData.Data.Category] = button;
					_categoriesContainer.Add(button);
				}
			}
			FLog.Verbose("Fetched all news");
		}
	}
}