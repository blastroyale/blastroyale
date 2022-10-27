using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Disconnected from Adventure Screen UI by:
	/// - Reconnect to the Adventure
	/// - Leave the Adventure to the Main menu
	/// </summary>
	[LoadSynchronously]
	public class LowConnectionPresenter : UiToolkitPresenterData<LowConnectionPresenter.StateData>
	{
		public struct StateData
		{
		}

		private VisualElement _lowConnectionIcon;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.NetworkService.HasLag.Observe(OnLag);
		}

		private void OnDestroy()
		{
			_services?.NetworkService.HasLag.StopObserving(OnLag);
		}

		protected override void QueryElements(VisualElement root)
		{
			_lowConnectionIcon = root.Q("LowConnectionIcon").Required();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetLowConnectionActive(false);
		}
		
		private void OnLag(bool previous, bool hasLag)
		{
			SetLowConnectionActive(hasLag);
		}
		
		private void SetLowConnectionActive(bool active)
		{
			_lowConnectionIcon.EnableInClassList("hidden", !active);
		}
	}
}