using System.IO;
using Src.FirstLight.Modules.Services.Runtime.Tools;
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
			
			if (GUILayout.Button("Open Import Folder"))
			{
				var imageCaptureService = target as ImageCaptureService;
		
				EditorUtility.RevealInFinder(imageCaptureService.ImportAbsoluteFolderPath);
			}
			if (GUILayout.Button("Open Export Folder"))
			{
				var exportDir = Path.GetDirectoryName( Application.dataPath ) + "/Assets/Captures";
				EditorUtility.RevealInFinder(exportDir);
			}
			if (GUILayout.Button("Export Metadata Collection"))
			{
				var imageCaptureService = target as ImageCaptureService;
				imageCaptureService.ExportMetadataCollection();
			}
			if (GUILayout.Button("Export Metadata File"))
			{
				var imageCaptureService = target as ImageCaptureService;
				imageCaptureService.ExportMetadataJson();
			}
			if (GUILayout.Button("Snapshot"))
			{
				var imageCaptureService = target as ImageCaptureService;
				imageCaptureService.SnapShot();
			}
			if (GUILayout.Button("Centre Marker Children"))
			{
				var imageCaptureService = target as ImageCaptureService;
				imageCaptureService.CentreMarkerChildren();
			}
		}
	}
}