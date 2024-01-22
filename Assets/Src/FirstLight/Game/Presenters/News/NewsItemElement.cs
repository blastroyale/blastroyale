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
	}

	public class TitleNews
	{
		public string Date;
		public string Title;
		public NewsItemData Data;

		public TitleNews() { }

		public TitleNews(TitleNewsItem playfabItem)
		{
			Date = playfabItem.Timestamp.ToString(CultureInfo.CurrentCulture);
			Title = playfabItem.Title;
			Data = JsonConvert.DeserializeObject<NewsItemData>(playfabItem.Body);
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
		
		public string Title { get; set; }
		public string Text { get; set; }
		public string ImageUrl { get; set; }
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
		}

		public void SetData(TitleNews newsItem)
		{
			_date.text = newsItem.Date;
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

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var ge = (NewsItemElement) ve;
				ge.SetData(new TitleNews
				{
					Data = new NewsItemData()
					{
						Image = _imageUrl.GetValueFromBag(bag, cc),
						Text = _text.GetValueFromBag(bag, cc)
					},
					Title = _title.GetValueFromBag(bag, cc),
					Date = _date.GetValueFromBag(bag, cc)
				});
			}
		}
	}
}