using System.IO;
using Src.FirstLight.Modules.Services.Runtime;
using UnityEditor;
using UnityEngine;

namespace Src.FirstLight.Modules.Services.Editor
{
	[CustomEditor(typeof(ImageCaptureService))]
	[CanEditMultipleObjects]
	public class ImageCaptureEditor : UnityEditor.Editor 
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			if (GUILayout.Button("Open Export Folder"))
			{
				var exportDir = Path.GetDirectoryName( Application.dataPath ) + "/Assets/Captures";
				EditorUtility.RevealInFinder(exportDir);
			}
			if (GUILayout.Button("Run Capture"))
			{
				var imageCaptureService = target as ImageCaptureService;
				imageCaptureService.Run();
			}
		}
	}
}