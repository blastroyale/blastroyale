using System;
using System.ComponentModel;
using System.IO;
using UnityEngine;

public partial class SROptions
{
#if UNITY_EDITOR
	[UnityEditor.MenuItem("FLG/Take Screenshot #s")]
#endif
	[Category("Marketing")]
	public static void TakeScreenshot()
	{
		var directory = Directory.GetCurrentDirectory() + "/Screenshots";
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		
		ScreenCapture.CaptureScreenshot(directory + "/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".png");
		Debug.Log("New Screenhot Done - " + directory);
	}
}