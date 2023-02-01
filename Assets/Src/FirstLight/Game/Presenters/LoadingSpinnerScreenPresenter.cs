using System;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing generic loading spinner screen
	/// </summary>	
	[LoadSynchronously]
	public class LoadingSpinnerScreenPresenter : UiToolkitPresenter
	{
		protected override void QueryElements(VisualElement root)
		{
		}
	}
}