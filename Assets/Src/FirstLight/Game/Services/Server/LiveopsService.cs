using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service responsible for hooking game events, reading liveops configs and matching those configs to events to
	/// detect if there's any in-game action to be taken for those events.
	/// Specific actions can be declared in the <see cref="SegmentActionHandler"/> class
	/// </summary>
	public interface ILiveopsService
	{
		/// <summary>
		/// Obtains a list of feature flag overrides based on the user segments
		/// </summary>
		Dictionary<string, string> GetUserSegmentedFeatureFlags();

		/// <summary>
		///  Fetch the user segments from third party providers
		/// </summary>
		void FetchSegments(Action<List<string>> onFetched = null);

		/// <summary>
		/// Checks if a given user is in a given segment
		/// </summary>
		bool IsInSegment(string segmentName);
	}
	
	public class LiveopsService : ILiveopsService
	{
		private IGameBackendService _gameBackend;
		private IGameServices _services;
		private List<string> _segments;
		private SegmentActionHandler _actionHandler;
		private ILiveopsDataProvider _liveopsData;
		private IConfigsProvider _configs;

		public LiveopsService(IGameBackendService gameBackendService, IConfigsProvider configs, IGameServices services, ILiveopsDataProvider liveopsData)
		{
			_actionHandler = new SegmentActionHandler(services);
			_gameBackend = gameBackendService;
			_liveopsData = liveopsData;
			_services = services;
			_configs = configs;
			_services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
		}

		public Dictionary<string, string> GetUserSegmentedFeatureFlags()
		{
			var result = new Dictionary<string, string>();
			var featureFlags = _configs.GetConfigsList<LiveopsFeatureFlagConfig>();
			foreach (var featureFlag in featureFlags)
			{
				if (IsInSegment(featureFlag.PlayerSegment))
				{
					result[featureFlag.FeatureFlag] = featureFlag.Enabled.ToString();
				}
			}
			return result;
		}

		// TODO - ADD ERROR CALLBACK?
		public void FetchSegments(Action<List<string>> onFetched=null)
		{
			_gameBackend.GetPlayerSegments(r =>
			{
				_segments = r.Select(s => s.Name.ToLower()).ToList();
				onFetched?.Invoke(_segments);
			}, null);
		}

		public bool IsInSegment(string segmentName)
		{
			if (_segments == null)
			{
				FLog.Error("Reading segments before fetching them, something went bad");
				return false;
			}
			return _segments.Contains(segmentName.ToLower());
		}

		private void OnMainMenuOpened(MainMenuOpenedMessage msg)
		{
			TriggerSegmentEvents(msg.GetType());
		}

		private void TriggerSegmentEvents(Type triggerEventType)
		{
			var actions = _configs.GetConfigsList<LiveopsSegmentActionConfig>();
			foreach (var action in actions)
			{
				if (_liveopsData.HasTriggeredSegmentationAction(action.ActionIdentifier) || !IsInSegment(action.PlayerSegment))
				{
					continue;
				}
				_actionHandler.TriggerAction(action);
			}
		}
		
	}
}