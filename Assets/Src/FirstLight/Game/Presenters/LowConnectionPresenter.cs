using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.UiService;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
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
	public class LowConnectionPresenter : UiToolkitPresenter
	{
		private VisualElement[] _loading;
		private VisualElement _lowConnectionIcon;
		private VisualElement _overlay;
		private IGameServices _services;
		private FP _startFrame;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.NetworkService.HasLag.Observe(OnLag);
			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResync);
			QuantumCallback.Subscribe<CallbackGameDestroyed>(this, OnGameDestroyed);
		}

		public bool IsSimulationOverlayOpen()
		{
			return !_overlay.ClassListContains("hidden");
		}

		private void OnUpdateView(CallbackUpdateView cb)
		{
			if (cb.Game.Frames.Verified.Number > _startFrame)
			{
				_startFrame = 0;
				QuantumCallback.UnsubscribeListener<CallbackUpdateView>(this);
				SetLoadingGame(false);
			}
		}

		private void OnGameDestroyed(CallbackGameDestroyed cb)
		{
			_startFrame = 0;
			SetLoadingGame(false);
			QuantumCallback.UnsubscribeListener<CallbackUpdateView>(this);
		}
		
		private void OnGameResync(CallbackGameResynced cb)
		{
			SetLoadingGame(true);
			_startFrame = cb.Game.Frames.Verified.Number;
			QuantumCallback.Subscribe<CallbackUpdateView>(this,OnUpdateView);
		}
		
		private void OnDestroy()
		{
			QuantumCallback.UnsubscribeListener(this);
			_services?.NetworkService.HasLag.StopObserving(OnLag);
		}

		protected override void QueryElements(VisualElement root)
		{
			_overlay = root;
			_lowConnectionIcon = root.Q("LowConnectionBg").Required();
			_loading = new[]
			{
				root.Q("LoadingSpinner").Required(),
				root.Q("LoaderText").Required()
			};
			SetLoadingGame(false);
			SetLowConnectionActive(false);
		}

		private void OnLag(bool previous, bool hasLag)
		{
			SetLowConnectionActive(hasLag);
		}
		
		private void SetLowConnectionActive(bool active)
		{
			_lowConnectionIcon.EnableInClassList("hidden", !active);
			_overlay.EnableInClassList("hidden",
				_lowConnectionIcon.ClassListContains("hidden") &&
				_loading[0].ClassListContains("hidden"));
		}

		private void SetLoadingGame(bool loading)
		{
			var controls = _services.GameUiService.GetUi<MatchControlsHudPresenter>();
			if (loading)
			{
				if (!controls.IsDestroyed())
				{
					controls.Hidden = true;
				}
				
			}
			else
			{
				if (!controls.IsDestroyed())
				{
					controls.Hidden = false;
				}
			}

			foreach (var l in _loading)
			{
				l.EnableInClassList("hidden", !loading);
			}
			_overlay.EnableInClassList("hidden",
				_lowConnectionIcon.ClassListContains("hidden") &&
				_loading[0].ClassListContains("hidden"));
		}
	}
}