#if !DISABLE_SRDEBUGGER
using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using SRDebugger;
using SRDebugger.Internal;
using SRDebugger.Services;
using UnityEngine;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace FirstLight.Game.Cheats.SROptions
{
	public class FLGBugReporter : IBugReporterHandler
	{
		public bool IsUsable => Settings.Instance.EnableBugReporter && !string.IsNullOrWhiteSpace(Settings.Instance.ApiKey);

		public void Submit(BugReport report, Action<BugReportSubmitResult> onComplete, IProgress<float> progress)
		{
			// Update send to send PlayfabID
			report.Email += $"({PlayFabSettings.staticPlayer.PlayFabId})";

			// Send device in description to be easy spotable
			report.UserDescription += "\n";
			report.UserDescription += "\nEnv: " + MainInstaller.ResolveServices().GameBackendService.CurrentEnvironmentData.EnvironmentID.ToString();
			report.UserDescription += "\nOs: " + SystemInfo.operatingSystem;
			report.UserDescription += "\nModel: " + SystemInfo.deviceModel;

			// Send state
			var dataProvider = MainInstaller.ResolveServices().DataService;
			MainInstaller.ResolveServices().GameBackendService.FetchServerState(state =>
			{
				var console = new List<ConsoleEntry>(report.ConsoleLog);
				console.Add(new ConsoleEntry
				{
					LogType = LogType.Log,
					Message = MainInstaller.Resolve<IGameStateMachine>().GetCurrentStateDebug(),
					Count = 1,
					StackTrace = ""
				});

				foreach (var type in dataProvider.GetKeys())
				{
					string serverValue = string.Empty;
					if (type.FullName != null) state.TryGetValue(type.FullName, out serverValue);

					string clientValue = ModelSerializer.Serialize(dataProvider.GetData((type))).Value;

					console.Add(new ConsoleEntry
					{
						LogType = LogType.Log,
						Message = "Model state " + type.Name,
						StackTrace = $"Client {type.Name}:\n {clientValue}\n\nServer {type.Name}:\n{serverValue}\n",
						Count = 1,
					});
				}

				report.ConsoleLog = console;
				BugReportApi.Submit(report, Settings.Instance.ApiKey, onComplete, progress);
			}, (err) => FLog.Error(err.ErrorMessage));
		}
	}
}
#endif