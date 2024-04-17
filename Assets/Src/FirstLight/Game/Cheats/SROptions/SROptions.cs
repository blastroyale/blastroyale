using System;
using System.ComponentModel;
using System.Linq;
using FirstLight.Game.Utils;
using SRF.Service;
using UnityEngine;
using UnityEngine.Scripting;

public delegate void SROptionsPropertyChanged(object sender, string propertyName);

#if !DISABLE_SRDEBUGGER
[Preserve]
#endif
public partial class SROptions : INotifyPropertyChanged
{
	private static SROptions _current;

	public static SROptions Current
	{
		get { return _current; }
	}

#if !DISABLE_SRDEBUGGER
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void OnStartup()
	{
		_current = new SROptions(); // Need to reset options here so if we enter play-mode without a domain reload there will be the default set of options.
		AddCurrencyCheats();
		SRServiceManager.GetService<SRDebugger.Internal.InternalOptionsRegistry>().AddOptionContainer(Current);
		SRDebug.Instance.SetBugReporterHandler(new FirstLight.Game.Cheats.SROptions.FLGBugReporter());

		SRDebug.Instance.AddSystemInfo(SRDebugger.InfoEntry.Create("Client Build Commit", () => VersionUtils.Commit), "Version");
		SRDebug.Instance.AddSystemInfo(SRDebugger.InfoEntry.Create("Server Build Commit", () => VersionUtils.ServerBuildCommit), "Version");
		SRDebug.Instance.AddSystemInfo(SRDebugger.InfoEntry.Create("Server Build Number", () => VersionUtils.ServerBuildNumber), "Version");
		SRDebug.Instance.AddSystemInfo(SRDebugger.InfoEntry.Create("Client Build Number", () => VersionUtils.BuildNumber), "Version");
	}


#endif

	public event SROptionsPropertyChanged PropertyChanged;

#if UNITY_EDITOR
	[JetBrains.Annotations.NotifyPropertyChangedInvocator]
#endif
	public void OnPropertyChanged(string propertyName)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, propertyName);
		}

		if (InterfacePropertyChangedEventHandler != null)
		{
			InterfacePropertyChangedEventHandler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

#if !DISABLE_SRDEBUGGER
	public void SendQuietBugReport(string desc)
	{
		var s = SRServiceManager.GetService<SRDebugger.Services.IBugReportService>();

		var r = new SRDebugger.BugReport
		{
			Email = "quiet report",
			UserDescription = desc,
			ConsoleLog = SRDebugger.Internal.Service.Console.AllEntries.ToList(),
			SystemInformation = SRServiceManager.GetService<SRDebugger.Services.ISystemInformationService>().CreateReport(),
			ScreenshotData = null
		};


		s.SendBugReport(r, (succeed, message) => { }, new Progress<float>());
	}
#endif

	private event PropertyChangedEventHandler InterfacePropertyChangedEventHandler;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add { InterfacePropertyChangedEventHandler += value; }
		remove { InterfacePropertyChangedEventHandler -= value; }
	}
}