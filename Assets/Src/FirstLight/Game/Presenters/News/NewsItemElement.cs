using System;
using System.Globalization;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	[Serializable]
	public class NewsItemData
	{
		public string Image;
		public string Text;
		public string Category;
		public NewsActionButtonData[] Buttons;
	}

	public class TitleNews
	{
		public DateTime Date;
		public string DateFormatted;
		public string Title;
		public NewsItemData Data;

		public TitleNews() { }

		public TitleNews(TitleNewsItem playfabItem)
		{
			Date = playfabItem.Timestamp;
			DateFormatted = Date.ToShortDateString();
			Title = playfabItem.Title;
			Data = JsonConvert.DeserializeObject<NewsItemData>(playfabItem.Body);
			Data.Category ??= "Game";
		}
	}
	
	/// <summary>
	/// Handles a news (journal) item
	/// </summary>
	public class NewsItemElement : VisualElement
	{
		private static VisualTreeAsset _itemResource;
		
		private Label _title;
		private Label _date;
		private Label _text;
		private VisualElement _image;
		private VisualElement _actions;
		private VisualElement _hot;
		private VisualElement _hotShine;
		
		public string Title { get; set; }
		public string Text { get; set; }
		public string ImageUrl { get; set; }
		public string Category { get; set; }
		public string Date { get; set; }
		
		public NewsItemElement()
		{
			if (_itemResource == null)
			{
				_itemResource = Resources.Load<VisualTreeAsset>("NewsItem");
			}
			_itemResource.CloneTree(this);
			
			_title = this.Q<Label>("Title").Required();
			_date = this.Q<Label>("Date").Required();
			_text = this.Q<Label>("Text").Required();
			_image = this.Q<VisualElement>("ContentImage").Required();
			_actions = this.Q("Controls").Required();
			_hot = this.Q("Hot").Required();
			_hotShine = this.Q("HotShine").Required();
		}

		public void SetHot(bool hot)
		{
			_hot.SetDisplay(hot);
			_hotShine.SetDisplay(hot);
			if (hot)
			{
				_hotShine.AddRotatingEffect(1, 1);
				_hotShine.AnimatePing();
				_hot.AnimatePing();
			}
		}

		public void SetData(TitleNews newsItem)
		{
			_date.text = newsItem.DateFormatted;
			_title.text = newsItem.Title;
			var data = newsItem.Data;
			_text.text = data.Text;
			if (data.Image != null)
			{
				MainInstaller.ResolveServices().RemoteTextureService.SetTexture(_image, data.Image);
			}
			else
			{
				_image.SetDisplay(false);
			}

			if (newsItem.Data.Buttons == null || newsItem.Data.Buttons.Length == 0)
			{
				_actions.SetDisplay(false);
				return;
			}
			
			foreach (var btnData in newsItem.Data.Buttons)
			{
				_actions.Add(new NewsActionButton().SetData(btnData));				
			}
		}
		
		public new class UxmlFactory : UxmlFactory<NewsItemElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private readonly UxmlStringAttributeDescription _title = new ()
			{
				name = "title",
				defaultValue = "Test Title",
			};
			
			private readonly UxmlStringAttributeDescription _text = new ()
			{
				name = "text",
				defaultValue = "test text",
			};
			
			private readonly UxmlStringAttributeDescription _date = new ()
			{
				name = "date",
				defaultValue = "01/01/1981",
			};
			
			private readonly UxmlStringAttributeDescription _imageUrl = new ()
			{
				name = "image-url",
				defaultValue = "",
			};
			
			private readonly UxmlStringAttributeDescription _category = new ()
			{
				name = "category",
				defaultValue = "Game",
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var ge = (NewsItemElement) ve;
				ge.SetData(new TitleNews
				{
					Data = new NewsItemData()
					{
						Image = _imageUrl.GetValueFromBag(bag, cc),
						Text = _text.GetValueFromBag(bag, cc),
						Category = _category.GetValueFromBag(bag, cc)
					},
					Title = _title.GetValueFromBag(bag, cc),
					DateFormatted = _date.GetValueFromBag(bag, cc)
				});
			}
		}
	}
}