using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{

	public enum NewsActionButtonType
	{
		Hyperlink
	}
	
	[Serializable]
	public class NewsActionButtonData
	{
		public NewsActionButtonType ButtonType;
		public string ButtonText;
		public string Param;
		public string Color;
	}
	
	
	/// <summary>
	/// Handles a news (journal) item
	/// </summary>
	public class NewsActionButton : Button
	{
		public const string USS_ACTION_BUTTON = "news-action-button";
		
		private NewsActionButtonData _data;
		
		public NewsActionButton SetData(NewsActionButtonData data)
		{
			this.text = data.ButtonText;
			_data = data;
			AddToClassList(USS_ACTION_BUTTON);
			if (!string.IsNullOrEmpty(_data.Color) && ColorUtility.TryParseHtmlString(_data.Color, out var color))
			{
				style.backgroundColor = color;
			}
			if(data.ButtonType == NewsActionButtonType.Hyperlink) SetupLinkButton();
			return this;
		}

		private void SetupLinkButton()
		{
			this.clicked += () => Application.OpenURL(_data.Param);
		}
	}
}