using System;
using System.Collections.Generic;
using System.Linq;
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
		IReadOnlyList<string> UserSegments { get; }
	}
	
	public class LiveopsService : ILiveopsService
	{
		private IPlayfabService _playfab;
		private IGameServices _services;
		private List<string> _segments;
		private SegmentActionHandler _actionHandler;
		private ILiveopsDataProvider _liveopsData;
		private IConfigsProvider _configs;

		public LiveopsService(IPlayfabService playfabService, IConfigsProvider configs, IGameServices services, ILiveopsDataProvider liveopsData)
		{
			_actionHandler = new SegmentActionHandler(services);
			_playfab = playfabService;
			_liveopsData = liveopsData;
			_services = services;
			_configs = configs;
			_services.MessageBrokerService.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
		}
		
		public IReadOnlyList<string> UserSegments => _segments;
		
		private void OnMainMenuOpened(MainMenuOpenedMessage msg)
		{
			_playfab.GetPlayerSegments(OnGetPlayerSegments);
		}

		private void OnGetPlayerSegments(List<GetSegmentResult> segments)
		{
			_segments = segments.Select(s => s.Name).ToList();
			TriggerSegmentEvents(typeof(MainMenuOpenedMessage));
		}

		private void TriggerSegmentEvents(Type triggerEventType)
		{
			var actions = _configs.GetConfigsList<LiveopsSegmentActionConfig>();
			foreach (var action in actions)
			{
				if (_liveopsData.HasTriggeredSegmentationAction(action.ActionIdentifier) || !_segments.Contains(action.PlayerSegment))
				{
					continue;
				}
				_actionHandler.TriggerAction(action);
			}
		}
	}
}