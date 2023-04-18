using System;
using System.ComponentModel;
using System.Linq;
using FirstLight.Game.Cheats.SROptions;
using SRDebugger;
using SRDebugger.Internal;
using SRDebugger.Services;
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
        SRServiceManager.GetService<SRDebugger.Internal.InternalOptionsRegistry>().AddOptionContainer(Current);
		SRDebug.Instance.SetBugReporterHandler(new FLGBugReporter());
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

	public void SendQuietBugReport(string desc)
	{
		var s = SRServiceManager.GetService<IBugReportService>();

		var r = new BugReport
		{
			Email = "quiet report",
			UserDescription = desc,
			ConsoleLog = Service.Console.AllEntries.ToList(),
			SystemInformation = SRServiceManager.GetService<ISystemInformationService>().CreateReport(),
			ScreenshotData = null
		};


		s.SendBugReport(r, (succeed, message) =>{} ,new Progress<float>());
	}

    private event PropertyChangedEventHandler InterfacePropertyChangedEventHandler;

    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add { InterfacePropertyChangedEventHandler += value; }
        remove { InterfacePropertyChangedEventHandler -= value; }
    }
}
