using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using I2.Loc;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Class to implement specific handlers for specific actions. Liveops can setup specific actions to
	/// happen when players are in a given segment. The possible actions are defined in this class.
	/// </summary>
	public class SegmentActionHandler
	{
		private IGameServices _services;
		private Dictionary<LiveopsAction, Action<LiveopsSegmentActionConfig>> _handlers;
		
		public SegmentActionHandler(IGameServices services)
		{
			_services = services;
			_handlers = new()
			{
				{ LiveopsAction.ButtonDialog, HandleDialogAction }
			};
		}

		private void HandleDialogAction(LiveopsSegmentActionConfig actionConfig)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = () =>
				{
					_services.GenericDialogService.CloseDialog();
					_services.CommandService.ExecuteCommand(new LiveopsActionCommand()
					{
						ActionIdentifier = actionConfig.ActionIdentifier
					});
				}
			};
			_services.GenericDialogService.OpenButtonDialog(
				GetLocalizedParameter(actionConfig.ActionParameter[0]),
				GetLocalizedParameter(actionConfig.ActionParameter[1]), 
				false, 
				confirmButton);
		}
		
		public void TriggerAction(LiveopsSegmentActionConfig actionConfig)
		{
			if (_handlers.TryGetValue(actionConfig.Action, out var triggerFunction))
			{
				triggerFunction(actionConfig);
				_services.AnalyticsService.UiCalls.ScreenView($"Segment Action {actionConfig.ActionIdentifier}");
			}
		}

		private string GetLocalizedParameter(string parameter)
		{
			// TODO: Add localization support for liveoperable stuff
			return parameter;
		}
	}
}